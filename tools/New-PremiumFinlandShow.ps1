param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$width = 128
$height = 128

function Get-PixelId([int]$x, [int]$y) {
    if ($x -lt 0 -or $x -ge $width -or $y -lt 0 -or $y -ge $height) {
        return $null
    }

    return ($y * $width) + $x
}

function Get-RingIds(
    [double]$centerX,
    [double]$centerY,
    [double]$radius,
    [double]$thickness,
    [double]$startAngle = 0,
    [double]$endAngle = 360
) {
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    $inner = [Math]::Max(0.0, $radius - ($thickness / 2))
    $outer = $radius + ($thickness / 2)

    for ($y = [Math]::Max(0, [Math]::Floor($centerY - $outer)); $y -le [Math]::Min($height - 1, [Math]::Ceiling($centerY + $outer)); $y++) {
        for ($x = [Math]::Max(0, [Math]::Floor($centerX - $outer)); $x -le [Math]::Min($width - 1, [Math]::Ceiling($centerX + $outer)); $x++) {
            $dx = $x - $centerX
            $dy = $y - $centerY
            $distance = [Math]::Sqrt(($dx * $dx) + ($dy * $dy))
            if ($distance -lt $inner -or $distance -gt $outer) { continue }

            $angle = [Math]::Atan2($dy, $dx) * 180 / [Math]::PI
            if ($angle -lt 0) { $angle += 360 }
            if ($startAngle -le $endAngle) {
                if ($angle -lt $startAngle -or $angle -gt $endAngle) { continue }
            }
            elseif ($angle -gt $endAngle -and $angle -lt $startAngle) {
                continue
            }

            [void]$ids.Add((Get-PixelId $x $y))
        }
    }

    return @($ids | Sort-Object)
}

function Get-LineIds([double]$x1, [double]$y1, [double]$x2, [double]$y2, [double]$thickness = 1, [bool]$dashed = $false) {
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    $steps = [Math]::Max([Math]::Abs($x2 - $x1), [Math]::Abs($y2 - $y1)) * 2
    for ($step = 0; $step -le $steps; $step++) {
        if ($dashed -and (([Math]::Floor($step / 6) % 2) -eq 1)) { continue }
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

function Get-SpectrumIds([double[]]$levels) {
    $ids = [System.Collections.Generic.HashSet[int]]::new()
    $barCount = $levels.Count
    $barWidth = 3
    $barGap = 2
    $startX = 10
    $bottomY = 121

    for ($bar = 0; $bar -lt $barCount; $bar++) {
        $barHeight = 2 + [Math]::Round(50 * [Math]::Min(1.0, [Math]::Max(0.0, $levels[$bar])))
        $barHeight = [Math]::Min(52, [Math]::Max(2, $barHeight))
        $xStart = $startX + ($bar * ($barWidth + $barGap))
        $topY = $bottomY - $barHeight + 1

        for ($y = $topY; $y -le $bottomY; $y++) {
            for ($x = $xStart; $x -lt ($xStart + $barWidth); $x++) {
                $id = Get-PixelId $x $y
                if ($null -ne $id) { [void]$ids.Add($id) }
            }
        }
    }

    return @($ids | Sort-Object)
}

function New-Color([int]$red, [int]$green, [int]$blue, [int]$white) {
    return [ordered]@{ R = $red; G = $green; B = $blue; W = $white }
}

function New-VisualClip(
    [string]$name,
    [double]$start,
    [double]$duration,
    [int]$effect,
    [int[]]$ids,
    $color,
    [double]$intensity,
    [double]$speed = 1
) {
    return [ordered]@{
        Name = $name
        StartTime = [Math]::Max(0.0, $start)
        Duration = [Math]::Max(0.08, $duration)
        EffectType = $effect
        IsAudio = $false
        IsHidden = $false
        IsMedia = $false
        MediaOverlayId = $null
        Target = [ordered]@{ Type = 1; EntityIds = @($ids); TrackName = $null }
        Color = $color
        Intensity = [Math]::Min(1.0, [Math]::Max(0.0, $intensity))
        Speed = [Math]::Max(0.1, $speed)
        MovementEffect = 6
        MovementOffsetX = 0
        MovementOffsetY = 0
        IsMotionDraft = $false
        MovementKeyframes = @()
    }
}

$project = Get-Content -LiteralPath $InputPath -Raw | ConvertFrom-Json
$audioTrack = $project.Tracks | Where-Object { $_.Clips | Where-Object IsAudio } | Select-Object -First 1
$maleSource = $project.Tracks | Where-Object { $_.Name -like '*homme*' } | Select-Object -First 1
$womanSource = $project.Tracks | Where-Object { $_.Name -like '*Juge*' } | Select-Object -First 1
$violinSource = $project.Tracks | Where-Object { $_.Name -like '*Violon*' } | Select-Object -First 1

if ($null -eq $audioTrack -or $null -eq $maleSource -or $null -eq $womanSource -or $null -eq $violinSource) {
    throw 'La sauvegarde ne contient pas les pistes de référence attendues.'
}

$womanClips = [System.Collections.Generic.List[object]]::new()
$warmPalette = @(
    (New-Color 255 72 32 28),
    (New-Color 255 126 36 38),
    (New-Color 255 54 92 24),
    (New-Color 255 178 76 46)
)
$womanEvents = @($womanSource.Clips | Sort-Object StartTime)
for ($eventIndex = 0; $eventIndex -lt $womanEvents.Count; $eventIndex++) {
    $event = $womanEvents[$eventIndex]
    for ($ringIndex = 0; $ringIndex -lt 4; $ringIndex++) {
        $radius = 8 + ($ringIndex * 9)
        $delay = $ringIndex * 0.11
        $duration = [Math]::Max(0.75, [double]$event.Duration + 0.55 - $delay)
        $ids = Get-RingIds 88 45 $radius (4.8 - ($ringIndex * 0.65))
        $clipName = "Voix femme - onde chaude $($eventIndex + 1).$($ringIndex + 1)"
        $clipStart = [double]$event.StartTime + $delay
        $clipIntensity = 0.92 - ($ringIndex * 0.14)
        $clipSpeed = 1.15 + ($ringIndex * 0.16)
        $clipColor = $warmPalette[$ringIndex]
        $clip = New-VisualClip $clipName $clipStart $duration 1 $ids $clipColor $clipIntensity $clipSpeed
        $womanClips.Add($clip)
    }
}

$maleClips = [System.Collections.Generic.List[object]]::new()
$maleEvents = @($maleSource.Clips | Sort-Object StartTime)
for ($eventIndex = 0; $eventIndex -lt $maleEvents.Count; $eventIndex++) {
    $event = $maleEvents[$eventIndex]
    $start = [double]$event.StartTime
    $duration = [Math]::Max(0.65, [double]$event.Duration)
    $core = New-VisualClip "Voix homme - coeur contenu $($eventIndex + 1)" $start $duration 1 (Get-RingIds 35 79 9 5 28 332) (New-Color 58 82 255 40) 0.9 1.25
    $tensionStart = $start + 0.08
    $tensionDuration = [Math]::Max(0.55, $duration - 0.08)
    $tension = New-VisualClip "Voix homme - amour en tension $($eventIndex + 1)" $tensionStart $tensionDuration 2 (Get-RingIds 35 79 18 3.5 32 328) (New-Color 108 55 255 24) 0.65 1.8
    $reachStart = $start + 0.16
    $reachDuration = [Math]::Max(0.45, $duration - 0.16)
    $reach = New-VisualClip "Voix homme - elan interrompu $($eventIndex + 1)" $reachStart $reachDuration 1 (Get-LineIds 49 72 73 54 3 $true) (New-Color 224 72 152 30) 0.72 1.4
    $maleClips.Add($core)
    $maleClips.Add($tension)
    $maleClips.Add($reach)
}

$violinClips = [System.Collections.Generic.List[object]]::new()
$violinEvents = @($violinSource.Clips | Sort-Object StartTime)
for ($eventIndex = 0; $eventIndex -lt $violinEvents.Count; $eventIndex++) {
    $event = $violinEvents[$eventIndex]
    $start = [double]$event.StartTime
    $duration = [Math]::Max(0.32, [double]$event.Duration)
    $halo = New-VisualClip "Violon - halo cardiaque $($eventIndex + 1)" $start $duration 2 (Get-EcgIds 0 5) (New-Color 255 50 118 20) 0.22 5.4
    $pulse = New-VisualClip "Violon - pulsation vive $($eventIndex + 1)" $start $duration 2 (Get-EcgIds 0 2) (New-Color 255 188 92 110) 1 6.4
    $resonanceStart = $start + 0.06
    $resonanceDuration = [Math]::Max(0.24, $duration - 0.06)
    $resonance = New-VisualClip "Violon - resonance rapide $($eventIndex + 1)" $resonanceStart $resonanceDuration 1 (Get-EcgIds 2 1) (New-Color 255 92 184 55) 0.5 7.2
    $violinClips.Add($halo)
    $violinClips.Add($pulse)
    $violinClips.Add($resonance)
}

$spectrumIds = [System.Collections.Generic.HashSet[int]]::new()
for ($bar = 0; $bar -lt 22; $bar++) {
    $xStart = 10 + ($bar * 5)
    for ($y = 68; $y -le 121; $y++) {
        for ($x = $xStart; $x -lt ($xStart + 3); $x++) {
            [void]$spectrumIds.Add((Get-PixelId $x $y))
        }
    }
}
$spectrumClip = New-VisualClip 'Spectre DJ – rendu temps réel' 0 ([double]$project.Duration) 8 @($spectrumIds | Sort-Object) (New-Color 255 104 180 72) 0.82 2.8
$spectrumClip.MovementEffect = 0
$spectrumClips = @($spectrumClip)

$project.Name = 'Finland – Ondes, voix et pulsations'
$project.Tracks = @(
    $audioTrack,
    [ordered]@{ Name = 'Spectre DJ – Bandes dynamiques'; Clips = @($spectrumClips) },
    [ordered]@{ Name = 'Voix de la femme – Ondes chaudes'; Clips = @($womanClips) },
    [ordered]@{ Name = "Voix de l’homme – Amour inaccessible"; Clips = @($maleClips) },
    [ordered]@{ Name = 'Violon seul – Pulsations cardiaques'; Clips = @($violinClips) }
)
$project.MediaOverlays = @()

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$project | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $OutputPath -Encoding utf8
