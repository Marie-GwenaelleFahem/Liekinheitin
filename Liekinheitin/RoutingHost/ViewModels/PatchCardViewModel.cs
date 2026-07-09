using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Liekinheitin.RoutingHost.ViewModels;

/// <summary>
/// Une carte affichée dans la grille de visualisation du patch : un contrôleur, un univers, ou
/// une LED, selon le niveau de navigation courant.
/// </summary>
public class PatchCardViewModel : INotifyPropertyChanged
{
    public string Title { get; }
    public string Body { get; }
    public StatusDot Status { get; }

    /// <summary>Identifiants des entités couvertes par cette carte (une seule pour une LED, toutes celles d'un univers ou d'un contrôleur sinon).</summary>
    public List<int> EntityIds { get; }

    /// <summary>Clé stable identifiant cette carte d'un rafraîchissement à l'autre (pour retrouver la couleur de test appliquée).</summary>
    public string Key { get; }

    /// <summary>Commande de navigation vers le niveau suivant, ou <c>null</c> si la carte n'est pas cliquable (niveau LED).</summary>
    public ICommand? NavigateCommand { get; }

    public bool IsClickable => NavigateCommand is not null;

    /// <summary>Envoie la couleur de test passée en <see cref="CardSwatch"/> comme paramètre de commande.</summary>
    public ICommand ApplySwatchCommand { get; }

    private CardSwatch _selectedSwatch;
    public CardSwatch SelectedSwatch
    {
        get => _selectedSwatch;
        set
        {
            _selectedSwatch = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public PatchCardViewModel(
        string key,
        string title,
        string body,
        StatusDot status,
        List<int> entityIds,
        CardSwatch initialSwatch,
        ICommand? navigateCommand,
        Action<PatchCardViewModel, CardSwatch> applySwatch)
    {
        Key = key;
        Title = title;
        Body = body;
        Status = status;
        EntityIds = entityIds;
        _selectedSwatch = initialSwatch;
        NavigateCommand = navigateCommand;
        ApplySwatchCommand = new RelayCommand(parameter => applySwatch(this, (CardSwatch)parameter!));
    }
}
