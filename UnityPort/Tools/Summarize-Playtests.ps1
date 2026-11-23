[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SessionsPath,

    [string]$AttackClarityPath,
    [string]$SessionId,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$phaseNames = @('Bosque alagado', 'Bacia de sal', 'Costa das badaladas')

if (-not (Test-Path -LiteralPath $SessionsPath -PathType Leaf)) {
    throw "CSV de sessões não encontrado: $SessionsPath"
}

$sessions = @(Import-Csv -LiteralPath $SessionsPath)
if ($sessions.Count -eq 0) {
    throw 'O CSV de sessões não contém linhas de jogo.'
}

$requiredSessionColumns = @(
    'session_id', 'phase', 'room', 'seconds', 'deaths', 'unclear_attacks',
    'crescent', 'salt', 'tide', 'enemies_left', 'completed', 'current_entries',
    'current_seconds', 'input_mode', 'seals_revealed', 'shallow_water_seconds',
    'resonance_activations'
)
$sessionColumns = @($sessions[0].PSObject.Properties.Name)
$missingSessionColumns = @($requiredSessionColumns | Where-Object { $_ -notin $sessionColumns })
if ($missingSessionColumns.Count -gt 0) {
    throw "Este não parece ser o CSV v8. Colunas ausentes: $($missingSessionColumns -join ', ')"
}

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
        input_modes = (@($items.input_mode | Where-Object { $_ } | Select-Object -Unique) -join ', ')
    }
}

Write-Output 'Resumo por sessão'
$sessionSummary | Format-Table -AutoSize | Out-String | Write-Output
Write-Output 'Detalhes por sala'
$details | Sort-Object session_id, phase, room |
    Format-Table session_id, location, seconds, deaths, unclear_attacks, attack_marks,
        crescent, salt, tide, completed, input_mode -AutoSize |
    Out-String | Write-Output

if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $resolvedOutputPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
    $outputDirectory = Split-Path -Parent $resolvedOutputPath
    if ($outputDirectory -and -not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
        New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    }
    $details | Sort-Object session_id, phase, room | Export-Csv -LiteralPath $resolvedOutputPath -NoTypeInformation -Encoding UTF8
    Write-Output "CSV consolidado: $resolvedOutputPath"
}
