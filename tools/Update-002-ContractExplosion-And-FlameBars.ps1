param(
    [string]$ProjectPath = "$PSScriptRoot\..\Liekinheitin\CreativeTool\Animations\002.lshow"
)

$ErrorActionPreference = 'Stop'
$project = Get-Content -LiteralPath $ProjectPath -Raw -Encoding UTF8 | ConvertFrom-Json
$project.Name = '002'

$voiceTrack = $project.Tracks | Where-Object { $_.Name -like '1*Voix*' } | Select-Object -First 1
if ($null -eq $voiceTrack) { throw 'Piste de voix homme introuvable.' }

$strong3 = $voiceTrack.Clips | Where-Object Name -eq 'Voix homme - ondulations fortes 3' | Select-Object -First 1
$strong10 = $voiceTrack.Clips | Where-Object Name -eq 'Voix homme - ondulations fortes 10' | Select-Object -First 1
$soft4 = $voiceTrack.Clips | Where-Object Name -eq 'Voix homme - ondulations douces 4' | Select-Object -First 1
if ($null -in @($strong3, $strong10, $soft4)) { throw 'Clips de voix attendus introuvables.' }

$strong3.IsHidden = $true
$strong10.Duration = [Math]::Max(0.001, 5.7 - [double]$strong10.StartTime)
$soft4End = [double]$soft4.StartTime + [double]$soft4.Duration
$soft4.StartTime = 9.5
$soft4.Duration = [Math]::Max(0.001, $soft4End - 9.5)

$voiceTrack.Clips += [pscustomobject]@{
    Name = 'Voix homme - contraction bleue et explosion'
    StartTime = 5.7
    Duration = 3.8
    EffectType = 14
    IsAudio = $false
    IsHidden = $false
    IsMedia = $false
    MediaOverlayId = $null
    Target = [pscustomobject]@{ Type = 0; EntityIds = @(); TrackName = $null }
    Color = [pscustomobject]@{ R = 24; G = 112; B = 255; W = 18 }
    Intensity = 0.94
    Speed = 1.0
    RippleCenterX = $null
    RippleCenterY = $null
    MovementEffect = 0
    MovementOffsetX = 0
    MovementOffsetY = 0
    RotationDegrees = 0
    IsMotionDraft = $false
    MovementKeyframes = @()
}

$lineTrack = $project.Tracks | Where-Object Name -eq 'Lignes blanches descendantes' | Select-Object -First 1
if ($null -eq $lineTrack -or $lineTrack.Clips.Count -ne 1) { throw 'Piste de rectangles descendants introuvable.' }
$lineTrack.Name = 'Rectangles flamme descendants'
$lineClip = $lineTrack.Clips[0]
$lineClip.Name = 'Rectangles flamme — chute 9,5 à 11 s'
$lineClip.StartTime = 9.5
$lineClip.Duration = 1.5
$lineClip.Color.R = 255
$lineClip.Color.G = 102
$lineClip.Color.B = 8
$lineClip.Color.W = 22
$lineClip.Intensity = 0.96

$json = $project | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path -LiteralPath $ProjectPath), $json, [System.Text.UTF8Encoding]::new($false))
