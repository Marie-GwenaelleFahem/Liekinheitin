param(
    [Parameter(Mandatory = $true)] [string]$InputPath,
    [Parameter(Mandatory = $true)] [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$width = 128
$height = 128

function Get-HandDrawnRippleIds([bool]$strong) {
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    $radii = if ($strong) { @(7, 14, 22, 31, 41, 52) } else { @(10, 21, 34, 48) }
    $thickness = if ($strong) { 3.4 } else { 2.1 }
    $centerX = 64.0
    $centerY = 66.0

    for ($y = 0; $y -lt $height; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            $dx = $x - $centerX
            $dy = $y - $centerY
            $distance = [Math]::Sqrt(($dx * $dx) + ($dy * $dy))
            $angle = [Math]::Atan2($dy, $dx)
            foreach ($radius in $radii) {
                $wobble = ([Math]::Sin(($angle * 3.0) + ($radius * 0.17)) * 1.15) + ([Math]::Sin(($angle * 7.0) - ($radius * 0.09)) * 0.55)
                if ([Math]::Abs($distance - ($radius + $wobble)) -le $thickness) {
                    [void]$ids.Add(($y * $width) + $x)
                    break
                }
            }
        }
    }

    return @($ids | Sort-Object)
}

$project = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json
$violinTrack = $project.Tracks | Where-Object { $_.Name -like '*Violon*' } | Select-Object -First 1
if ($null -eq $violinTrack) {
    throw 'La piste Violon est introuvable dans cette sauvegarde.'
}

$softIds = Get-HandDrawnRippleIds $false
$strongIds = Get-HandDrawnRippleIds $true
for ($index = 0; $index -lt $violinTrack.Clips.Count; $index++) {
    $clip = $violinTrack.Clips[$index]
    $strong = ([double]$clip.Intensity -ge 0.8) -or ([double]$clip.Speed -ge 1.8)
    $clip.Name = if ($strong) { "Violon - ondulations fortes $($index + 1)" } else { "Violon - ondulations douces $($index + 1)" }
    $clip.EffectType = 9
    $clip.Target.Type = 1
    $clip.Target.EntityIds = if ($strong) { @($strongIds) } else { @($softIds) }
    $clip.Target.TrackName = $null
    $clip.Intensity = if ($strong) { 1.0 } else { 0.58 }
    $clip.Speed = if ($strong) { 3.0 } else { 0.62 }
    $clip.MovementEffect = 0
    $clip.MovementOffsetX = 0
    $clip.MovementOffsetY = 0
    $clip.MovementKeyframes = @()
    if ($clip.PSObject.Properties.Name -contains 'RotationDegrees') {
        $clip.RotationDegrees = 0
    }
    else {
        $clip | Add-Member -NotePropertyName RotationDegrees -NotePropertyValue 0
    }
}

$violinTrack.Name = '2 – Violon — ondulations dynamiques'
$project.Name = 'Finland – Voix, Violon et Ondulations'

$directory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
$project | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $OutputPath -Encoding utf8
