param(
    [Parameter(Mandatory = $true)] [string]$InputPath,
    [Parameter(Mandatory = $true)] [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$width = 128
$height = 128

function Get-PixelId([int]$x, [int]$y) {
    if ($x -lt 0 -or $x -ge $width -or $y -lt 0 -or $y -ge $height) { return $null }
    return ($y * $width) + $x
}

function Get-LineIds(
    [double]$x1,
    [double]$y1,
    [double]$x2,
    [double]$y2,
    [double]$thickness = 1
) {
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    $steps = [Math]::Max([Math]::Abs($x2 - $x1), [Math]::Abs($y2 - $y1)) * 2
    for ($step = 0; $step -le $steps; $step++) {
        $progress = if ($steps -eq 0) { 0 } else { $step / $steps }
        $cx = $x1 + (($x2 - $x1) * $progress)
        $cy = $y1 + (($y2 - $y1) * $progress)
        $radius = [Math]::Max(0, [Math]::Floor($thickness / 2))
        for ($oy = -$radius; $oy -le $radius; $oy++) {
            for ($ox = -$radius; $ox -le $radius; $ox++) {
                $id = Get-PixelId ([Math]::Round($cx + $ox)) ([Math]::Round($cy + $oy))
                if ($null -ne $id) { [void]$ids.Add($id) }
            }
        }
    }
    return @($ids | Sort-Object)
}

function Get-EcgIds([int]$offsetY = 0, [double]$thickness = 1) {
    $points = @(
        @(4, 64), @(19, 64), @(24, 61), @(29, 67), @(34, 64),
        @(47, 64), @(52, 53), @(56, 87), @(61, 35), @(67, 72), @(73, 64),
        @(88, 64), @(94, 59), @(100, 68), @(106, 64), @(123, 64)
    )
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    for ($index = 0; $index -lt $points.Count - 1; $index++) {
        $segment = Get-LineIds $points[$index][0] ($points[$index][1] + $offsetY) $points[$index + 1][0] ($points[$index + 1][1] + $offsetY) $thickness
        foreach ($id in $segment) { [void]$ids.Add($id) }
    }
    return @($ids | Sort-Object)
}

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

function Copy-Color($color) {
    return [ordered]@{ R = $color.R; G = $color.G; B = $color.B; W = $color.W }
}

function New-EcgClip(
    [string]$name,
    $sourceClip,
    [int[]]$ids,
    [int]$effectType,
    [double]$intensity,
    [double]$speed,
    [int]$whiteBoost = 0
) {
    $color = Copy-Color $sourceClip.Color
    $color.W = [Math]::Min(255, [int]$color.W + $whiteBoost)
    return [ordered]@{
        Name = $name
        StartTime = [double]$sourceClip.StartTime
        Duration = [double]$sourceClip.Duration
        EffectType = $effectType
        IsAudio = $false
        IsHidden = $false
        IsMedia = $false
        MediaOverlayId = $null
        Target = [ordered]@{ Type = 1; EntityIds = @($ids); TrackName = $null }
        Color = $color
        Intensity = $intensity
        Speed = $speed
        MovementEffect = 0
        MovementOffsetX = 0
        MovementOffsetY = 0
        IsMotionDraft = $false
        MovementKeyframes = @()
        RotationDegrees = 0
    }
}

$project = Get-Content -LiteralPath $InputPath -Raw -Encoding UTF8 | ConvertFrom-Json
$maleTrack = $project.Tracks | Where-Object { $_.Name -like '*homme*' } | Select-Object -First 1
$violinTrack = $project.Tracks | Where-Object { $_.Name -like '*Violon*' } | Select-Object -First 1
if ($null -eq $maleTrack -or $null -eq $violinTrack) {
    throw "Les pistes de la voix de l’homme et du violon sont requises."
}

$softRippleIds = Get-HandDrawnRippleIds $false
$strongRippleIds = Get-HandDrawnRippleIds $true
for ($index = 0; $index -lt $maleTrack.Clips.Count; $index++) {
    $clip = $maleTrack.Clips[$index]
    $strong = ([double]$clip.Intensity -ge 0.8) -or ([double]$clip.Speed -ge 1.6)
    $clip.Name = if ($strong) { "Voix homme - ondulations fortes $($index + 1)" } else { "Voix homme - ondulations douces $($index + 1)" }
    $clip.EffectType = 9
    $clip.Target.Type = 1
    $clip.Target.EntityIds = if ($strong) { @($strongRippleIds) } else { @($softRippleIds) }
    $clip.Target.TrackName = $null
    $clip.Intensity = if ($strong) { 0.95 } else { 0.58 }
    $clip.Speed = if ($strong) { 2.35 } else { 0.68 }
    $clip.MovementEffect = 0
    $clip.MovementOffsetX = 0
    $clip.MovementOffsetY = 0
    $clip.MovementKeyframes = @()
    if ($clip.PSObject.Properties.Name -contains 'RotationDegrees') { $clip.RotationDegrees = 0 }
    else { $clip | Add-Member -NotePropertyName RotationDegrees -NotePropertyValue 0 }
}
$maleTrack.Name = "1 – Voix de l’homme — ondulations émotionnelles"

$ecgHaloIds = Get-EcgIds 0 5
$ecgCoreIds = Get-EcgIds 0 2
$newViolinClips = [System.Collections.Generic.List[object]]::new()
for ($index = 0; $index -lt $violinTrack.Clips.Count; $index++) {
    $sourceClip = $violinTrack.Clips[$index]
    $strong = ([double]$sourceClip.Intensity -ge 0.8) -or ([double]$sourceClip.Speed -ge 1.8)
    $strengthLabel = if ($strong) { 'fort' } else { 'doux' }
    $haloIntensity = if ($strong) { 0.34 } else { 0.16 }
    $coreIntensity = if ($strong) { 1.0 } else { 0.62 }
    $haloSpeed = if ($strong) { 6.0 } else { 2.2 }
    $coreSpeed = if ($strong) { 7.2 } else { 2.8 }
    $newViolinClips.Add((New-EcgClip "Violon - halo cardiaque $strengthLabel $($index + 1)" $sourceClip $ecgHaloIds 2 $haloIntensity $haloSpeed 18))
    $newViolinClips.Add((New-EcgClip "Violon - bip cardiaque $strengthLabel $($index + 1)" $sourceClip $ecgCoreIds 2 $coreIntensity $coreSpeed 80))
}
$violinTrack.Clips = @($newViolinClips)
$violinTrack.Name = '2 – Violon — pulsations cardiaques bip bip'

$project.Name = 'Finland – Ondulations Homme et Violon Cardiaque'
$directory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
$project | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $OutputPath -Encoding utf8
