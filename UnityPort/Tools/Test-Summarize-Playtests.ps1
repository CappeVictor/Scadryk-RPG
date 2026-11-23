[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$summaryScript = Join-Path $PSScriptRoot 'Summarize-Playtests.ps1'
$fixtureRoot = Join-Path ([IO.Path]::GetTempPath()) ('Scadryk-playtest-tests-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fixtureRoot | Out-Null

function Assert-Condition([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
}

function Invoke-Summary([string]$RatingsFile) {
    $warnings = @()
    $captured = (& $summaryScript -SessionsPath (Join-Path $fixtureRoot 'sessions.csv') `
        -RatingsPath (Join-Path $fixtureRoot $RatingsFile) -WarningVariable warnings 2>&1 | Out-String)
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) { throw "O resumo falhou: $captured" }
    return [pscustomobject]@{ Output = $captured; Warnings = @($warnings) }
}

try {
    $sessionHeader = 'session_id,phase,room,seconds,deaths,unclear_attacks,crescent,salt,tide,enemies_left,completed,current_entries,current_seconds,input_mode,seals_revealed,shallow_water_seconds,resonance_activations,salt_crosswind_seconds'
    $sessionRows = @(
        'beginner,1,1,60,0,0,0,0,0,0,true,0,0,keyboard,0,0,0,7',
        'intermediate1,1,1,60,0,0,0,0,0,0,true,0,0,keyboard,0,0,0,0',
        'intermediate2,1,2,60,0,0,0,0,0,0,true,0,0,keyboard,0,0,0,4'
    )
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'sessions.csv'), ($sessionHeader + "`n" + ($sessionRows -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))

    $ratingHeader = 'session_id,experience,objectives,optional_secret,attack_readability,controls,power_use,combo_readability,environmental_mechanics,audio_mix,memory_readability,memory_audio,secret_audio,portal_readability,death_fairness'
    $ratingRows = @(
        'beginner,iniciante,4,4,4,4,4,4,4,4,4,4,4,4,4',
        'intermediate1,intermediária,4,4,4,4,4,4,4,4,4,4,4,4,4',
        'intermediate2,intermediária,4,4,4,4,4,4,4,4,4,4,4,4,4'
    )
    $ratingsPath = Join-Path $fixtureRoot 'ratings.csv'
    [IO.File]::WriteAllText($ratingsPath, ($ratingHeader + "`n" + ($ratingRows -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'empty-ratings.csv'), ($ratingHeader + "`n"), [Text.UTF8Encoding]::new($false))

    $partial = Invoke-Summary 'ratings.csv'
    Assert-Condition ($partial.Output.Contains('Cobertura das avaliações')) 'A tabela de cobertura não foi impressa.'
    Assert-Condition (($partial.Warnings.Count -eq 2) -and (($partial.Warnings -join "`n") -match "iniciante.*faltam 1") -and (($partial.Warnings -join "`n") -match "experiente.*faltam 2")) 'As lacunas das amostras parciais não foram avisadas corretamente.'
    $v9SummaryPath = Join-Path $fixtureRoot 'v9-summary.csv'
    & $summaryScript -SessionsPath (Join-Path $fixtureRoot 'sessions.csv') -OutputPath $v9SummaryPath | Out-Null
    $v9Rows = @(Import-Csv -LiteralPath $v9SummaryPath)
    Assert-Condition (($v9Rows | Where-Object session_id -eq 'beginner').salt_crosswind_seconds -eq '7') 'O resumo v9 perdeu a exposição à corrente de sal.'

    $legacyHeader = ($sessionHeader -split ',') | Where-Object { $_ -ne 'salt_crosswind_seconds' }
    $legacyRows = @($sessionRows | ForEach-Object { $_.Substring(0, $_.LastIndexOf(',')) })
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'legacy-v8.csv'), (($legacyHeader -join ',') + "`n" + ($legacyRows -join "`n") + "`n"), [Text.UTF8Encoding]::new($false))
    $legacySummaryPath = Join-Path $fixtureRoot 'legacy-summary.csv'
    & $summaryScript -SessionsPath (Join-Path $fixtureRoot 'legacy-v8.csv') -OutputPath $legacySummaryPath | Out-Null
    $legacyRowsOut = @(Import-Csv -LiteralPath $legacySummaryPath)
    Assert-Condition (($legacyRowsOut | Where-Object session_id -eq 'beginner').salt_crosswind_seconds -eq '0') 'CSV v8 legado não recebeu zero seguro para a nova métrica.'

    $empty = Invoke-Summary 'empty-ratings.csv'
    Assert-Condition ($empty.Output.Contains('Cobertura das avaliações') -and $empty.Warnings.Count -eq 3) 'O CSV sem respostas não mostrou os três grupos pendentes.'

    [IO.File]::WriteAllText($ratingsPath, ($ratingHeader + "`n" + ($ratingRows[0] + "`n" + $ratingRows[0]) + "`n"), [Text.UTF8Encoding]::new($false))
    $duplicateRejected = $false
    try { & $summaryScript -SessionsPath (Join-Path $fixtureRoot 'sessions.csv') -RatingsPath $ratingsPath *> $null }
    catch { $duplicateRejected = $_.Exception.Message -match 'Mais de uma ficha' }
    Assert-Condition $duplicateRejected 'Fichas duplicadas para a mesma sessão não foram rejeitadas.'

    $invalidRow = 'beginner,iniciante,9,4,4,4,4,4,4,4,4,4,4,4,4'
    [IO.File]::WriteAllText($ratingsPath, ($ratingHeader + "`n" + $invalidRow + "`n"), [Text.UTF8Encoding]::new($false))
    $invalidRatingRejected = $false
    try { & $summaryScript -SessionsPath (Join-Path $fixtureRoot 'sessions.csv') -RatingsPath $ratingsPath *> $null }
    catch { $invalidRatingRejected = $_.Exception.Message -match 'precisa ser de 1 a 5' }
    Assert-Condition $invalidRatingRejected 'A nota fora da escala de 1 a 5 não foi rejeitada.'

    Write-Output 'PASS: amostra parcial, arquivo sem respostas, ficha duplicada e nota inválida.'
}
finally {
    foreach ($name in @('sessions.csv', 'ratings.csv', 'empty-ratings.csv', 'v9-summary.csv', 'legacy-v8.csv', 'legacy-summary.csv')) {
        $path = Join-Path $fixtureRoot $name
        if (Test-Path -LiteralPath $path -PathType Leaf) { Remove-Item -LiteralPath $path }
    }
    if (Test-Path -LiteralPath $fixtureRoot -PathType Container) { Remove-Item -LiteralPath $fixtureRoot }
}
