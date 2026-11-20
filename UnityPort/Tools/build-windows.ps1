param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$UnityEditorPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "Unity Editor não encontrado: $UnityEditorPath"
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$outputPath = Join-Path $projectRoot 'Build/ScadrykDemo.exe'
$logPath = Join-Path $env:TEMP ('Scadryk-Unity-Build-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')

& $UnityEditorPath `
    -batchmode `
    -quit `
    -projectPath $projectRoot `
    -executeMethod ScadrykBuildMenu.BuildWindowsDemo `
    -logFile $logPath

if ($LASTEXITCODE -ne 0) {
    throw "Build do Unity falhou (código $LASTEXITCODE). Consulte o log: $logPath"
}

if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
    throw "O Unity terminou sem gerar o executável esperado. Consulte o log: $logPath"
}

Write-Host "Build compilada. O jogo não foi iniciado."
Write-Host "Executável: $outputPath"
Write-Host "Log: $logPath"
