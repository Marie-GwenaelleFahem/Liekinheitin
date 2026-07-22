param(
    [Parameter(Mandatory = $true)] [string]$InputPath,
    [Parameter(Mandatory = $true)] [string]$OutputPath,
    [double]$Duration = 3.0,
    [string]$ProjectName = ''
)

$ErrorActionPreference = 'Stop'
$project = Get-Content -LiteralPath $InputPath -Raw -Encoding UTF8 | ConvertFrom-Json
$fadeDuration = [Math]::Max(0, $Duration)

if ($project.PSObject.Properties.Name -contains 'AudioFadeOutDuration') {
    $project.AudioFadeOutDuration = $fadeDuration
}
else {
    $project | Add-Member -NotePropertyName AudioFadeOutDuration -NotePropertyValue $fadeDuration
}

if (-not [string]::IsNullOrWhiteSpace($ProjectName)) {
    $project.Name = $ProjectName
}

$directory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$project | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $OutputPath -Encoding utf8
