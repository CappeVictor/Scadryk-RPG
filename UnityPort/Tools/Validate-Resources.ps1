$ErrorActionPreference = 'Stop'

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artRoot = Join-Path $projectRoot 'Assets/Resources/ScadrykArt'
$sourceRoot = Join-Path $projectRoot 'Assets/Scripts'
if (-not (Test-Path -LiteralPath $artRoot -PathType Container)) {
    throw "Unity Resources folder was not found: $artRoot"
}

$resourceNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$directLoaderPattern = 'Load(?:EffectSprite|SingleSprite|SheetRow)\s*\(\s*"([A-Za-z0-9_]+)"'
$directResourcePattern = 'Resources\.Load<[^>]+>\s*\(\s*"ScadrykArt/([A-Za-z0-9_]+)"'
$sheetChoicePattern = '^\s*string\s+sheet\s*=.*'
$quotedNamePattern = '"([A-Za-z0-9_]+)"'

foreach ($sourceFile in Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -Recurse -File) {
    foreach ($line in Get-Content -LiteralPath $sourceFile.FullName) {
        foreach ($match in [regex]::Matches($line, $directLoaderPattern)) {
            [void]$resourceNames.Add($match.Groups[1].Value)
        }
        foreach ($match in [regex]::Matches($line, $directResourcePattern)) {
            [void]$resourceNames.Add($match.Groups[1].Value)
        }
        if ($line -match $sheetChoicePattern) {
            foreach ($match in [regex]::Matches($line, $quotedNamePattern)) {
                [void]$resourceNames.Add($match.Groups[1].Value)
            }
        }
    }
}

if ($resourceNames.Count -eq 0) {
    throw 'No Resources.Load asset names were found in Unity scripts.'
}

$missing = @(
    foreach ($name in ($resourceNames | Sort-Object)) {
        $assetPath = Join-Path $artRoot ($name + '.png')
        if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) { $assetPath }
    }
)
if ($missing.Count -gt 0) {
    throw "Missing Unity Resources assets:`n$($missing -join "`n")"
}

$invalidTextureImports = @(
    foreach ($asset in Get-ChildItem -LiteralPath $artRoot -Filter '*.png' -File) {
        $metaPath = $asset.FullName + '.meta'
        if (-not (Test-Path -LiteralPath $metaPath -PathType Leaf) -or
            -not (Select-String -LiteralPath $metaPath -Pattern '^  isReadable: 0\s*$' -Quiet)) {
            $metaPath
        }
    }
)
if ($invalidTextureImports.Count -gt 0) {
    throw "Every PNG in Resources/ScadrykArt needs metadata with isReadable: 0:`n$($invalidTextureImports -join "`n")"
}

$artCount = @(Get-ChildItem -LiteralPath $artRoot -Filter '*.png' -File).Count
Write-Host "Validated $($resourceNames.Count) script resource references and all $artCount GPU-only art texture imports."
