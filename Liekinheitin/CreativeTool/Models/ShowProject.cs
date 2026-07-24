using System.Collections.Generic;

namespace Liekinheitin.CreativeTool.Models
{
    public class ShowProject
    {
        public string Name { get; set; } = "Nouveau projet";

        public double Duration { get; set; } = 30.0;

        // Le mur visible fait bien 128 x 128 LED. Physiquement, il est câblé en 64 bandes de
        // 259 LED chacune, pliées en U (2 colonnes de 128 LED visibles par bande + 3 LED
        // invisibles de fixation/pli) : voir LoadRealEntityIds() dans MainWindow.xaml.cs pour le
        // détail du dépliage. WallWidth/WallHeight décrivent l'image logique (128x128), pas le
        // câblage physique.
        public int WallWidth { get; set; } = 128;

        public int WallHeight { get; set; } = 128;

        public string? AudioFilePath { get; set; }

        public double AudioVolume { get; set; } = 1.0;

        public double AudioFadeOutDuration { get; set; }

        public double? HardStopTime { get; set; }

        public bool DisableVisualFadeOut { get; set; }

        public List<Track> Tracks { get; set; } = new();

        public List<MediaOverlayClip> MediaOverlays { get; set; } = new();
    }
}
