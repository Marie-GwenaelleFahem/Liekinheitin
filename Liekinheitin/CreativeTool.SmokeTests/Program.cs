using Liekinheitin.CreativeTool.Models;
using Liekinheitin.CreativeTool.Services;

var project = new ShowProject
{
    Name = "Smoke test",
    Duration = 2,
    WallWidth = 2,
    WallHeight = 2,
    Tracks =
    {
        new Track
        {
            Name = "Lumière",
            Clips =
            {
                new TimelineClip
                {
                    Name = "Rouge",
                    StartTime = 0,
                    Duration = 1,
                    Color = new RgbwColor(255, 0, 0, 0),
                    Target = TargetSelection.FullWall()
                }
            }
        }
    }
};

var engine = new ShowPlaybackEngine();
var litState = engine.ComputeState(0.5, project);
Assert(litState.Entities.Count == 4, "Chaque image doit contenir tous les pixels.");
Assert(litState.Entities.All(entity => entity.Channels[0] == 255), "Le clip rouge doit éclairer tout le mur.");

var emptyState = engine.ComputeState(1.5, project);
Assert(emptyState.Entities.Count == 4, "Une image vide doit encore contenir tous les pixels.");
Assert(emptyState.Entities.All(entity => entity.Channels.All(channel => channel == 0)), "Les pixels hors clip doivent être noirs.");

var blackout = engine.ComputeBlackoutState(project);
Assert(blackout.Entities.Count == 4 && blackout.Entities.All(entity => entity.Channels.All(channel => channel == 0)),
    "Le blackout doit explicitement éteindre tous les pixels.");

var fileService = new ProjectFileService();
var tempPath = Path.Combine(Path.GetTempPath(), $"liekinheitin-{Guid.NewGuid():N}.lshow");
try
{
    fileService.Save(tempPath, project);
    var loaded = fileService.Load(tempPath);
    Assert(loaded.Tracks.Count == 1 && loaded.Tracks[0].Clips.Count == 1, "Le spectacle doit survivre à un aller-retour disque.");
    Assert(loaded.WallWidth == 2 && loaded.WallHeight == 2, "La géométrie doit être conservée.");
}
finally
{
    if (File.Exists(tempPath)) File.Delete(tempPath);
}

foreach (var effectType in Enum.GetValues<EffectType>())
{
    project.Tracks[0].Clips[0].EffectType = effectType;
    project.Tracks[0].Clips[0].Speed = 1.25;
    foreach (var time in new[] { 0.0, 0.25, 0.5, 0.99 })
    {
        var effectState = engine.ComputeState(time, project);
        Assert(effectState.Entities.Count == 4, $"L'effet {effectType} doit produire un état complet.");
        Assert(effectState.Entities.All(entity => entity.Channels.Length >= 3), $"L'effet {effectType} doit produire des canaux RGB.");
    }
}

var animationsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Liekinheitin", "CreativeTool", "Animations");
foreach (var showPath in Directory.EnumerateFiles(animationsFolder, "*.lshow"))
{
    var show = fileService.Load(showPath);
    Assert(show.Tracks.Count > 0, $"{Path.GetFileName(showPath)} doit contenir des pistes.");
    Assert(string.IsNullOrWhiteSpace(show.AudioFilePath) || File.Exists(show.AudioFilePath),
        $"La musique de {Path.GetFileName(showPath)} doit être accessible.");

    foreach (var time in new[] { 0.0, show.Duration / 2, Math.Max(0, show.Duration - 0.01) })
    {
        var state = engine.ComputeState(time, show);
        Assert(state.Entities.Count == show.WallWidth * show.WallHeight,
            $"{Path.GetFileName(showPath)} doit produire un état complet à {time:0.##} s.");
    }
}
var finlandTemplate = ShowTemplateService.CreateFinlandFortySeconds();
Assert(finlandTemplate.Duration == 40, "Le modèle Finland doit durer 40 secondes.");
Assert(finlandTemplate.Tracks.Count >= 5, "Le modèle Finland doit proposer plusieurs couches artistiques.");
Assert(finlandTemplate.Tracks.SelectMany(track => track.Clips).Count() >= 12, "Le modèle Finland doit être réellement monté.");
foreach (var time in Enumerable.Range(0, 41).Select(second => (double)second))
{
    var state = engine.ComputeState(time, finlandTemplate);
    Assert(state.Entities.Count == 128 * 128, $"Le modèle Finland doit produire une image complète à {time}s.");
}
foreach (var time in new[] { 1.5, 8.36, 10.5, 14.0, 18.2, 20.5, 22.5, 25.0, 29.7, 34.0, 38.5 })
{
    var state = engine.ComputeState(time, finlandTemplate);
    var litPixels = state.Entities.Count(entity => entity.Channels.Any(channel => channel > 0));
    Assert(litPixels > 20, $"La scène Finland doit être visuellement active à {time}s.");
}

foreach (var motifId in new[] { "Snowfall", "Frost", "Fire", "ToxicLove", "FireIce", "Impact" })
{
    var motif = ShowTemplateService.CreateMotif(motifId, 0, 128, 128, out var trackName);
    var motifProject = new ShowProject { Duration = motif.Duration, Tracks = { new Track { Name = trackName, Clips = { motif } } } };
    var sampleTime = motif.Duration <= 0.5 ? motif.Duration / 2 : Math.Min(2, motif.Duration / 2);
    var state = engine.ComputeState(sampleTime, motifProject);
    Assert(state.Entities.Any(entity => entity.Channels.Any(channel => channel > 0)), $"Le motif {motifId} doit être visible.");
}
Console.WriteLine("CreativeTool smoke tests: OK");

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
