param([string]$ProjectPath = "$PSScriptRoot\..\Liekinheitin\CreativeTool\Animations\004.lshow")

$ErrorActionPreference = 'Stop'
$project = Get-Content -LiteralPath $ProjectPath -Raw -Encoding UTF8 | ConvertFrom-Json
$project.Name = '004'
$audioEnd = @($project.Tracks | ForEach-Object { $_.Clips } | Where-Object IsAudio | ForEach-Object { [double]$_.StartTime + [double]$_.Duration } | Measure-Object -Maximum).Maximum
if ($audioEnd -le 45) { throw "Fin audio inattendue : $audioEnd" }

function New-CinematicClip($name, $start, $duration, $effect, $r, $g, $b, $w = 0) {
    [pscustomobject]@{
        Name = $name; StartTime = [double]$start; Duration = [double]$duration; EffectType = $effect
        IsAudio = $false; IsHidden = $false; IsMedia = $false; MediaOverlayId = $null
        Target = [pscustomobject]@{ Type = 0; EntityIds = @(); TrackName = $null }
        Color = [pscustomobject]@{ R = $r; G = $g; B = $b; W = $w }
        Intensity = 1.0; Speed = 1.0; RippleCenterX = $null; RippleCenterY = $null
        MovementEffect = 0; MovementOffsetX = 0; MovementOffsetY = 0; RotationDegrees = 0
        IsMotionDraft = $false; MovementKeyframes = @()
    }
}

$cinematicNames = @('Finale — Sucreries', 'Finale — Lignes rouges', 'Finale — Rose', 'Finale — Clair de lune', 'Finale — Oh dear Lord', 'Finale — Cœur croqué', 'Finale — Liquide noir')
$project.Tracks = @($project.Tracks | Where-Object { $_.Name -notin $cinematicNames })
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Sucreries'; IsMuted = $false; Clips = @(New-CinematicClip 'Sucreries — apparition et morsures' 27.7 1.1 15 255 132 58) }
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Lignes rouges'; IsMuted = $false; Clips = @(New-CinematicClip 'Fines diagonales rouges' 34 1 16 180 12 28) }
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Rose'; IsMuted = $false; Clips = @(New-CinematicClip 'Rose rouge — ouverture et explosion' 34 1 17 218 24 52) }
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Clair de lune'; IsMuted = $false; Clips = @(New-CinematicClip 'Deux silhouettes — approche et baiser' 35 7 18 224 219 177 20) }
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Oh dear Lord'; IsMuted = $false; Clips = @(New-CinematicClip 'Oh, dear Lord — rouge sang' 42 1 19 148 0 20) }
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Cœur croqué'; IsMuted = $false; Clips = @(New-CinematicClip 'Cœur rouge — morsure progressive' 43 2 20 206 8 34) }
$project.Tracks += [pscustomobject]@{ Name = 'Finale — Liquide noir'; IsMuted = $false; Clips = @(New-CinematicClip 'Liquide noir — extinction finale' 45 ($audioEnd - 45) 21 0 0 0) }

$project.Duration = [Math]::Max([double]$project.Duration, [double]$audioEnd)
$json = $project | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText((Resolve-Path -LiteralPath $ProjectPath), $json, [System.Text.UTF8Encoding]::new($false))
