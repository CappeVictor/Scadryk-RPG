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

$buildSuccess = Select-String -LiteralPath $logPath -Pattern 'Build Finished, Result: Success\.' -Quiet -ErrorAction SilentlyContinue
if (-not $buildSuccess) {
    throw "O Unity encerrou sem registrar um BuildReport bem-sucedido. A existência de um executável antigo não confirma uma build nova. Consulte o log: $logPath"
}

$dataPath = Join-Path $projectRoot 'Build/ScadrykDemo_Data'
$requiredPayloadFiles = @(
    (Join-Path $dataPath 'Managed/Assembly-CSharp.dll'),
    (Join-Path $dataPath 'boot.config'),
    (Join-Path $dataPath 'globalgamemanagers')
)
$missingPayload = $requiredPayloadFiles | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) }
if ($missingPayload) {
    throw "A build foi confirmada, mas o payload está incompleto: $($missingPayload -join ', '). Consulte o log: $logPath"
}

$buildInfoPath = Join-Path $projectRoot 'Build/SCADRYK_BUILD_INFO.txt'
if (-not (Test-Path -LiteralPath $buildInfoPath -PathType Leaf)) {
    throw "A build foi concluída, mas o Unity não gerou a identificação esperada: $buildInfoPath"
}
$buildInfoLines = Get-Content -LiteralPath $buildInfoPath
$buildInfoCommitLine = $buildInfoLines | Where-Object { $_.StartsWith('Commit: ', [System.StringComparison]::Ordinal) } | Select-Object -First 1
$buildInfoSourceLine = $buildInfoLines | Where-Object { $_.StartsWith('Source tree: ', [System.StringComparison]::Ordinal) } | Select-Object -First 1
$buildInfoChangedLine = $buildInfoLines | Where-Object { $_.StartsWith('Changed paths before build: ', [System.StringComparison]::Ordinal) } | Select-Object -First 1
$buildInfoEditorLine = $buildInfoLines | Where-Object { $_.StartsWith('Unity Editor: ', [System.StringComparison]::Ordinal) } | Select-Object -First 1
$buildInfoUtcLine = $buildInfoLines | Where-Object { $_ -match '^Build UTC: \d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$' } | Select-Object -First 1
if (-not $buildInfoCommitLine -or -not $buildInfoSourceLine -or -not $buildInfoChangedLine -or
    -not $buildInfoEditorLine -or -not $buildInfoUtcLine -or
    $buildInfoChangedLine -notmatch '^Changed paths before build: (unknown|\d+)$' -or
    $buildInfoSourceLine -notin @('Source tree: clean', 'Source tree: modified', 'Source tree: unavailable')) {
    throw "A identificação da build está incompleta: $buildInfoPath"
}
$changedPathCountText = ($buildInfoChangedLine -split ':', 2)[1].Trim()
if (($buildInfoSourceLine -eq 'Source tree: clean' -and $changedPathCountText -ne '0') -or
    ($buildInfoSourceLine -eq 'Source tree: modified' -and $changedPathCountText -notmatch '^\d*[1-9]\d*$')) {
    throw "O estado da árvore-fonte não corresponde à contagem registrada: $buildInfoPath"
}

$repositoryRoot = Split-Path -Parent $projectRoot
$gitCommand = Get-Command git -ErrorAction SilentlyContinue
if ($gitCommand) {
    $candidateCommit = & $gitCommand.Source -C $repositoryRoot rev-parse --short=12 HEAD 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($candidateCommit) -and
        $buildInfoCommitLine -ne ('Commit: ' + $candidateCommit.Trim())) {
        throw "A build informa '$buildInfoCommitLine', mas o código-fonte atual está em '$($candidateCommit.Trim())'."
    }
}

Write-Host "Build compilada. O jogo não foi iniciado."
Write-Host "Executável: $outputPath"
Write-Host "Identificação: $buildInfoPath"
Write-Host "Log: $logPath"
