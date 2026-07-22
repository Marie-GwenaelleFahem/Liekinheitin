using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class ShowProject
    {
        public string Name { get; set; } = "Nouveau projet";

        public double Duration { get; set; } = 30.0;

        // Le mur physique réel est composé de 64 bandes de LED de 259 de large chacune (une
        // bande = 2 univers DMX, 170 + 89 = 259 entités), pas d'une grille carrée 128x128 :
        // voir Liekinheitin/patch.json et la doc d'architecture du projet.
        public int WallWidth { get; set; } = 259;

        public int WallHeight { get; set; } = 64;

        public string? AudioFilePath { get; set; }

        public double AudioVolume { get; set; } = 1.0;

        public double AudioFadeOutDuration { get; set; }

        public List<Track> Tracks { get; set; } = new();

        public List<MediaOverlayClip> MediaOverlays { get; set; } = new();
    }
}
