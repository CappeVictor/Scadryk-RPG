param(
    [string]$UnityEditorPath
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($UnityEditorPath)) {
    $editorCandidates = @(
        'C:\Outros Arquivos\Unity\Editors\6000.6.9f1\Editor\Unity.exe',
        'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe'
    )
    $UnityEditorPath = $editorCandidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($UnityEditorPath) -or -not (Test-Path -LiteralPath $UnityEditorPath -PathType Leaf)) {
    throw "Unity Editor 6000.6.9f1 não encontrado. Instale essa versão ou informe o caminho com -UnityEditorPath."
}

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$logPath = Join-Path $env:TEMP ('Scadryk-Unity-Compile-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')

& $UnityEditorPath `
    -batchmode `
    -quit `
    -projectPath $projectRoot `
    -logFile $logPath

$unityExitCode = $LASTEXITCODE
$compilerErrors = Select-String -LiteralPath $logPath -Pattern '\berror CS\d+:' -ErrorAction SilentlyContinue
if ($unityExitCode -ne 0 -or $compilerErrors) {
    throw "Compilação do Unity falhou (código $unityExitCode). Consulte o log: $logPath"
}

Write-Host "Scripts e projeto importados pelo Editor sem abrir a cena em Play Mode."
Write-Host "Log: $logPath"
