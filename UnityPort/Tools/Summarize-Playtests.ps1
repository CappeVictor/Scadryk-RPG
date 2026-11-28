[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SessionsPath,

    [string]$AttackClarityPath,
    [string]$RatingsPath,
    [string]$SessionId,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$phaseNames = @('Bosque alagado', 'Bacia de sal', 'Costa das badaladas')
$allowedExperience = @('iniciante', 'intermediária', 'experiente')
$minimumRatingsPerExperience = 2

if (-not (Test-Path -LiteralPath $SessionsPath -PathType Leaf)) {
    throw "CSV de sessões não encontrado: $SessionsPath"
}

$sessions = @(Import-Csv -LiteralPath $SessionsPath)
if ($sessions.Count -eq 0) {
    throw 'O CSV de sessões não contém linhas de jogo.'
}
$sessionsWithoutId = @($sessions | Where-Object { [string]::IsNullOrWhiteSpace($_.session_id) })
if ($sessionsWithoutId.Count -gt 0) {
    throw "O CSV de sessões contém $($sessionsWithoutId.Count) linha(s) sem session_id; não é possível separar tentativas com segurança."
}
$knownSessionIds = @($sessions | ForEach-Object { $_.session_id } | Select-Object -Unique)

$requiredSessionColumns = @(
    'session_id', 'phase', 'room', 'seconds', 'deaths', 'unclear_attacks',
    'crescent', 'salt', 'tide', 'enemies_left', 'completed', 'current_entries',
    'current_seconds', 'input_mode', 'seals_revealed', 'shallow_water_seconds',
    'resonance_activations'
)
$sessionColumns = @($sessions[0].PSObject.Properties.Name)
$missingSessionColumns = @($requiredSessionColumns | Where-Object { $_ -notin $sessionColumns })
if ($missingSessionColumns.Count -gt 0) {
    throw "CSV de sessões incompatível. Colunas ausentes: $($missingSessionColumns -join ', ')"
}
$hasSaltCrosswindMetric = 'salt_crosswind_seconds' -in $sessionColumns

if (-not [string]::IsNullOrWhiteSpace($SessionId)) {
    $sessions = @($sessions | Where-Object { $_.session_id -eq $SessionId })
    if ($sessions.Count -eq 0) { throw "Nenhuma sessão corresponde ao ID '$SessionId'." }
}

$attackEvents = @()
if (-not [string]::IsNullOrWhiteSpace($AttackClarityPath)) {
    if (-not (Test-Path -LiteralPath $AttackClarityPath -PathType Leaf)) {
        throw "CSV de clareza de ataques não encontrado: $AttackClarityPath"
    }
    $attackEvents = @(Import-Csv -LiteralPath $AttackClarityPath)
    if ($attackEvents.Count -gt 0) {
        $requiredAttackColumns = @('session_id', 'phase', 'room', 'attack_patterns')
        $attackColumns = @($attackEvents[0].PSObject.Properties.Name)
        $missingAttackColumns = @($requiredAttackColumns | Where-Object { $_ -notin $attackColumns })
        if ($missingAttackColumns.Count -gt 0) {
            throw "CSV de clareza inválido. Colunas ausentes: $($missingAttackColumns -join ', ')"
        }
    }
}

$ratingColumns = @(
    'objectives', 'optional_secret', 'attack_readability', 'controls', 'power_use',
    'combo_readability', 'environmental_mechanics', 'audio_mix', 'memory_readability',
    'memory_audio', 'secret_audio', 'portal_readability', 'death_fairness', 'accessibility_options',
    'reduced_flashes', 'reduced_shake', 'high_contrast_warnings', 'text_scaling', 'visual_warnings_without_audio'
)
$accessibilityRatingColumns = @(
    'accessibility_options', 'reduced_flashes', 'reduced_shake', 'high_contrast_warnings',
    'text_scaling', 'visual_warnings_without_audio'
)
$ratingDetails = @()
if (-not [string]::IsNullOrWhiteSpace($RatingsPath)) {
    if (-not (Test-Path -LiteralPath $RatingsPath -PathType Leaf)) {
        throw "CSV de avaliações não encontrado: $RatingsPath"
    }
    $requiredRatingColumns = @('session_id', 'experience') + $ratingColumns
    $ratingHeaderLine = Get-Content -LiteralPath $RatingsPath -TotalCount 1
    $ratingHeader = $ratingHeaderLine.TrimStart([char]0xFEFF).Split(',')
    $missingRatingColumns = @($requiredRatingColumns | Where-Object { $_ -notin $ratingHeader })
    if ($missingRatingColumns.Count -gt 0) {
        throw "Cabeçalho de avaliações inválido. Colunas ausentes: $($missingRatingColumns -join ', ')"
    }

    $allRatingRows = @(Import-Csv -LiteralPath $RatingsPath)
    $orphanRatings = @($allRatingRows | Where-Object { $_.session_id -notin $knownSessionIds })
    foreach ($orphan in $orphanRatings) {
        $orphanId = if ([string]::IsNullOrWhiteSpace($orphan.session_id)) { '(vazio)' } else { $orphan.session_id }
        Write-Warning "A ficha '$orphanId' não corresponde a nenhuma sessão do CSV; ela não foi incluída."
    }
    $sessionIds = @($sessions | ForEach-Object { $_.session_id } | Select-Object -Unique)
    $ratingRows = @($allRatingRows | Where-Object { $_.session_id -in $sessionIds })
    foreach ($duplicate in ($ratingRows | Group-Object session_id | Where-Object { $_.Count -gt 1 })) {
        throw "Mais de uma ficha de avaliação foi informada para a sessão '$($duplicate.Name)'."
    }

    $ratingDetails = @(foreach ($row in $ratingRows) {
        if ($row.experience -notin $allowedExperience) {
            throw "Nível inválido na sessão '$($row.session_id)'. Use iniciante, intermediária ou experiente."
        }
        $detail = [ordered]@{ session_id = $row.session_id; experience = $row.experience }
        foreach ($column in $ratingColumns) {
            $value = 0
            if ([string]::IsNullOrWhiteSpace($row.$column)) { $detail[$column] = $null; continue }
            if (-not [int]::TryParse($row.$column, [ref]$value) -or $value -lt 1 -or $value -gt 5) {
                throw "A nota '$column' da sessão '$($row.session_id)' precisa ser de 1 a 5 ou ficar vazia."
            }
            $detail[$column] = $value
        }
        [pscustomobject]$detail
    })
}

$details = foreach ($row in $sessions) {
    $phase = 0
    $room = 0
    if (-not [int]::TryParse($row.phase, [ref]$phase) -or $phase -lt 1 -or $phase -gt $phaseNames.Count) {
        throw "Fase inválida na sessão '$($row.session_id)': '$($row.phase)'."
    }
    if (-not [int]::TryParse($row.room, [ref]$room) -or $room -lt 1 -or $room -gt 3) {
        throw "Sala inválida na sessão '$($row.session_id)': '$($row.room)'."
    }

    $roomEvents = @($attackEvents | Where-Object {
        $_.session_id -eq $row.session_id -and $_.phase -eq $row.phase -and $_.room -eq $row.room
    })
    $patterns = @($roomEvents | ForEach-Object { $_.attack_patterns } | Where-Object { $_ } | Select-Object -Unique)
    [pscustomobject]@{
        session_id = $row.session_id
        phase = $phase
        room = $room
        location = "$($phaseNames[$phase - 1]) $room"
        seconds = [int]$row.seconds
        deaths = [int]$row.deaths
        unclear_attacks = [int]$row.unclear_attacks
        attack_marks = $roomEvents.Count
        attack_patterns = $patterns -join ' ; '
        crescent = [int]$row.crescent
        salt = [int]$row.salt
        tide = [int]$row.tide
        input_mode = $row.input_mode
        completed = [System.Convert]::ToBoolean($row.completed)
        enemies_left = [int]$row.enemies_left
        current_entries = [int]$row.current_entries
        current_seconds = [int]$row.current_seconds
        salt_crosswind_seconds = if ($hasSaltCrosswindMetric -and -not [string]::IsNullOrWhiteSpace($row.salt_crosswind_seconds)) { [int]$row.salt_crosswind_seconds } else { 0 }
        shallow_water_seconds = [int]$row.shallow_water_seconds
        resonance_activations = [int]$row.resonance_activations
        seals_revealed = [int]$row.seals_revealed
    }
}

$sessionSummary = foreach ($group in ($details | Group-Object session_id)) {
    $items = @($group.Group)
    [pscustomobject]@{
        session_id = $group.Name
        rooms_recorded = $items.Count
        rooms_completed = @($items | Where-Object { $_.completed }).Count
        active_minutes = [math]::Round((($items | Measure-Object seconds -Sum).Sum / 60.0), 1)
        deaths = ($items | Measure-Object deaths -Sum).Sum
        unclear_attacks = ($items | Measure-Object unclear_attacks -Sum).Sum
        attack_marks = ($items | Measure-Object attack_marks -Sum).Sum
        crescent = ($items | Measure-Object crescent -Sum).Sum
        salt = ($items | Measure-Object salt -Sum).Sum
        tide = ($items | Measure-Object tide -Sum).Sum
        salt_crosswind_seconds = ($items | Measure-Object salt_crosswind_seconds -Sum).Sum
        input_modes = (@($items.input_mode | Where-Object { $_ } | Select-Object -Unique) -join ', ')
    }
}

Write-Output 'Resumo por sessão'
$sessionSummary | Format-Table -AutoSize | Out-String | Write-Output
Write-Output 'Detalhes por sala'
$details | Sort-Object session_id, phase, room |
    Format-Table session_id, location, seconds, deaths, unclear_attacks, attack_marks,
        crescent, salt, tide, current_seconds, salt_crosswind_seconds, shallow_water_seconds, completed, input_mode -AutoSize |
    Out-String | Write-Output

if (-not [string]::IsNullOrWhiteSpace($RatingsPath)) {
    $ratingCoverage = foreach ($experience in $allowedExperience) {
        $items = @($ratingDetails | Where-Object { $_.experience -eq $experience })
        [pscustomobject]@{
            experience = $experience
            participants = $items.Count
            minimum = $minimumRatingsPerExperience
            missing = [math]::Max(0, $minimumRatingsPerExperience - $items.Count)
            target_met = $items.Count -ge $minimumRatingsPerExperience
        }
    }
    Write-Output "Cobertura das avaliações (meta: $minimumRatingsPerExperience participantes por experiência)"
    $ratingCoverage | Format-Table -AutoSize | Out-String | Write-Output
    foreach ($row in $ratingCoverage | Where-Object { -not $_.target_met }) {
        Write-Warning "Experiência '$($row.experience)': faltam $($row.missing) avaliação(ões); não compare médias deste grupo como amostra concluída."
    }

    if ($ratingDetails.Count -gt 0) {
        $ratingSummary = foreach ($experience in $allowedExperience) {
            $items = @($ratingDetails | Where-Object { $_.experience -eq $experience })
            if ($items.Count -eq 0) { continue }
            $summary = [ordered]@{ experience = $experience; participants = $items.Count }
            foreach ($column in $ratingColumns) {
                $values = @($items | ForEach-Object { $_.$column } | Where-Object { $null -ne $_ })
                $summary[$column] = if ($values.Count -gt 0) { [math]::Round((($values | Measure-Object -Average).Average), 2) } else { $null }
            }
            [pscustomobject]$summary
        }
        Write-Output 'Médias das avaliações por experiência (escala de 1 a 5; interpretar junto da cobertura)'
        $ratingSummary | Format-Table -AutoSize | Out-String | Write-Output
        Write-Output 'Médias de acessibilidade por experiência (campos não usados ficam vazios)'
        $ratingSummary | Select-Object -Property (@('experience', 'participants') + $accessibilityRatingColumns) |
            Format-Table -AutoSize -Wrap | Out-String -Width 180 | Write-Output
    }
}

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }
    $details | Sort-Object session_id, phase, room | Export-Csv -LiteralPath $resolvedOutputPath -NoTypeInformation -Encoding UTF8
    Write-Output "CSV consolidado: $resolvedOutputPath"
}
