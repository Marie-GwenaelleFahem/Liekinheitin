using System;
using System.ComponentModel;
using System.Windows.Media;
using Liekinheitin.CreativeTool.Domain;

namespace Liekinheitin.CreativeTool.ViewModels
{
    public sealed class ShapeInspectorViewModel : INotifyPropertyChanged
    {
        private readonly SceneManager _scene;
        private PlacedShape? _shape;
        private bool _suppressPropagation; // évite de renvoyer vers la scène une valeur qu'on vient d'en recevoir

        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>Déclenché après toute modification qui doit rafraîchir la grille affichée.</summary>
        public event Action? ShapeModified;

        public ShapeInspectorViewModel(SceneManager scene)
        {
            _scene = scene;
        }

        public bool HasSelection => _shape is not null;
        public string IdText => _shape?.Id.ToString() ?? "";

        public int X
        {
            get => _shape?.X ?? 0;
            set { if (_shape is null) return; _scene.MoveShape(_shape.Id, value, _shape.Y); Sync(); }
        }

        public int Y
        {
            get => _shape?.Y ?? 0;
            set { if (_shape is null) return; _scene.MoveShape(_shape.Id, _shape.X, value); Sync(); }
        }

        public int Width
        {
            get => _shape?.BaseWidth ?? 1;
            set { if (_shape is null) return; _scene.ResizeShape(_shape.Id, value, _shape.BaseHeight); Sync(); }
        }

        public int Height
        {
            get => _shape?.BaseHeight ?? 1;
            set { if (_shape is null) return; _scene.ResizeShape(_shape.Id, _shape.BaseWidth, value); Sync(); }
        }

        public double Scale
        {
            get => _shape?.Scale ?? 1.0;
            set { if (_shape is null) return; _scene.SetScale(_shape.Id, value); Sync(); }
        }

        public byte ColorR
        {
            get => _shape?.Color.R ?? 0;
            set => SetColorComponent(r: value);
        }

        public byte ColorG
        {
            get => _shape?.Color.G ?? 0;
            set => SetColorComponent(g: value);
        }

        public byte ColorB
        {
            get => _shape?.Color.B ?? 0;
            set => SetColorComponent(b: value);
        }

        public Color CurrentColor => _shape?.Color ?? Colors.Black;

        private void SetColorComponent(byte? r = null, byte? g = null, byte? b = null)
        {
            if (_shape is null) return;
            var c = _shape.Color;
            var newColor = Color.FromRgb(r ?? c.R, g ?? c.G, b ?? c.B);
            _scene.SetColor(_shape.Id, newColor);
            Sync();
        }

        /// <summary>
        /// Charge une forme dans le panneau. Appelée à la sélection ET à chaque mise à jour
        /// temps réel pendant un glisser sur la grille (via GridView.SelectionChanged).
        /// </summary>
        public void Load(PlacedShape? shape)
        {
            _shape = shape;
            RaiseAll();
        }

        /// <summary>
        /// Après une modification via le panneau : on ne recharge PAS depuis la forme
        /// (elle vient d'être modifiée par la ligne précédente), on notifie juste les
        /// abonnés (grille) et on rafraîchit l'affichage des champs pour refléter le clamp
        /// éventuel (ex: position clampée aux bords par SceneManager).
        /// </summary>
        private void Sync()
        {
            RaiseAll();
            ShapeModified?.Invoke();
        }

        private void RaiseAll()
        {
            foreach (var name in new[] { nameof(HasSelection), nameof(IdText), nameof(X), nameof(Y),
                nameof(Width), nameof(Height), nameof(Scale), nameof(ColorR), nameof(ColorG), nameof(ColorB), nameof(CurrentColor) })
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}