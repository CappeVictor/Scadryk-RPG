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
$outputPath = Join-Path $projectRoot 'Build/ScadrykDemo.exe'
$logPath = Join-Path $env:TEMP ('Scadryk-Unity-Build-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')

$editorArguments = @(
    '-batchmode',
    '-quit',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-executeMethod',
    'ScadrykBuildMenu.BuildWindowsDemo',
    '-logFile', ('"{0}"' -f $logPath)
)
$unityProcess = Start-Process -FilePath $UnityEditorPath -ArgumentList $editorArguments -Wait -PassThru
$licenseError = Select-String -LiteralPath $logPath -Pattern 'No valid Unity Editor license found|Please activate your license' -ErrorAction SilentlyContinue
if ($licenseError) {
    throw "Unity não tem uma licença ativa. Entre no Unity Hub e ative uma licença Personal ou válida. Consulte: $logPath"
}
$codeIntegrityBlock = Select-String -LiteralPath $logPath -Pattern 'did not meet the Enterprise signing level requirements|violated code integrity policy|0x800711C7' -ErrorAction SilentlyContinue
if ($codeIntegrityBlock) {
    throw "A política Code Integrity/App Control do Windows bloqueou um componente do Unity durante a build. Peça ao administrador para aprovar o componente oficial dentro da política vigente; não desative a proteção. Consulte o log: $logPath (evento Code Integrity 3077)."
}
if ($unityProcess.ExitCode -ne 0) {
    throw "Build do Unity falhou (código $($unityProcess.ExitCode)). Consulte o log: $logPath"
}

if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
    throw "O Unity terminou sem gerar o executável esperado. Consulte o log: $logPath"
}

Write-Host "Build compilada. O jogo não foi iniciado."
Write-Host "Executável: $outputPath"
Write-Host "Log: $logPath"
