using System.Windows.Input;

namespace Liekinheitin.RoutingHost.ViewModels;

/// <summary>Un maillon du fil d'Ariane affiché au-dessus de la grille de cartes.</summary>
public class BreadcrumbItemViewModel
{
    public string Label { get; }
    public bool IsCurrent { get; }

    /// <summary>Commande de retour à ce niveau, ou <c>null</c> si ce maillon est le niveau courant.</summary>
    public ICommand? NavigateCommand { get; }

    public BreadcrumbItemViewModel(string label, bool isCurrent, ICommand? navigateCommand)
    {
        Label = label;
        IsCurrent = isCurrent;
        NavigateCommand = navigateCommand;
    }
}
