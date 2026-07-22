param(
    [string]$SourceName = 'Finland – Ondulations Homme, Violon Cardiaque et Fondu.lshow',
    [string]$OutputName = 'Finland – Ondulations Homme, Violon Cardiaque, Fondu et Anneaux Plein Écran.lshow'
)

$ErrorActionPreference = 'Stop'
$animations = Join-Path $PSScriptRoot '..\Liekinheitin\CreativeTool\Animations'
$source = Join-Path $animations $SourceName
$output = Join-Path $animations $OutputName

if (-not (Test-Path -LiteralPath $source)) {
    throw "Sauvegarde source introuvable : $source"
}

$project = Get-Content -LiteralPath $source -Raw -Encoding UTF8 | ConvertFrom-Json
$judgeTrack = $project.Tracks | Where-Object { $_.Name -like '3*Le Juge*' } | Select-Object -First 1
if ($null -eq $judgeTrack) {
    throw 'Piste « Le Juge » introuvable.'
}

$verdicts = @($judgeTrack.Clips | Where-Object { $_.Name -like 'Le Juge – verdict 1*' } | Sort-Object StartTime)
if ($verdicts.Count -lt 3) {
    throw "Trois impacts étaient attendus, $($verdicts.Count) seulement ont été trouvés."
}

function New-CalmWaterPatch {
    param(
        [int]$CenterX,
        [int]$CenterY,
        [int]$RadiusX,
        [int]$RadiusY,
        [int]$Seed
    )

    $ids = [System.Collections.Generic.List[int]]::new()
    for ($y = [Math]::Max(0, $CenterY - $RadiusY - 2); $y -le [Math]::Min($project.WallHeight - 1, $CenterY + $RadiusY + 2); $y++) {
        for ($x = [Math]::Max(0, $CenterX - $RadiusX - 2); $x -le [Math]::Min($project.WallWidth - 1, $CenterX + $RadiusX + 2); $x++) {
            $angle = [Math]::Atan2($y - $CenterY, $x - $CenterX)
            $shore = 1 + (0.075 * [Math]::Sin(($angle * 5) + $Seed)) + (0.04 * [Math]::Sin(($angle * 9) - ($Seed * 0.7)))
            $dx = ($x - $CenterX) / [double]$RadiusX
            $dy = ($y - $CenterY) / [double]$RadiusY
            if ((($dx * $dx) + ($dy * $dy)) -le $shore) {
                $ids.Add(($y * $project.WallWidth) + $x)
            }
        }
    }
    return @($ids)
}

$placements = @(
    @{ Name = "Le Juge – impact d'eau 1 — haut gauche"; X = 27;  Y = 24;  RX = 25; RY = 10; Seed = 1 },
    @{ Name = "Le Juge – impact d'eau 2 — haut droite"; X = 100; Y = 24;  RX = 25; RY = 10; Seed = 3 },
    @{ Name = "Le Juge – impact d'eau 3 — centre bas";  X = 64;  Y = 104; RX = 29; RY = 11; Seed = 5 },
    @{ Name = "Le Juge – impact d'eau 4 — haut droite organique"; X = 110; Y = 17; RX = 22; RY = 9; Seed = 7 }
)

for ($index = 0; $index -lt 4; $index++) {
    $clip = $verdicts[$index]
    $placement = $placements[$index]
    $clip.Name = $placement.Name.Replace("impact d'eau", "anneau d'impact")
    $clip.Duration = 1.1
    $clip.EffectType = 10      # ClickRipple
    $clip.MovementEffect = 0
    $clip.Speed = 1
    $clip.Intensity = 0.9
    $clip.Color.R = 88
    $clip.Color.G = 174
    $clip.Color.B = 220
    $clip.Color.W = 72
    $clip.Target.Type = 0      # FullWall : l'anneau peut atteindre les quatre coins
    $clip.Target.EntityIds = @()
    $clip | Add-Member -NotePropertyName RippleCenterX -NotePropertyValue ([double]$placement.X) -Force
    $clip | Add-Member -NotePropertyName RippleCenterY -NotePropertyValue ([double]$placement.Y) -Force
}

$project.Name = [System.IO.Path]::GetFileNameWithoutExtension($OutputName)
$json = $project | ConvertTo-Json -Depth 100
[System.IO.File]::WriteAllText($output, $json, [System.Text.UTF8Encoding]::new($false))
Write-Output $output
