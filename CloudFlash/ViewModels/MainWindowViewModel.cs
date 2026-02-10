using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CloudFlash.ViewModels;

// 1. Définition des sous-pages (Vues)
// pour rajouter une sous page il faut le faire ici
public class HomeViewModel : ViewModelBase { public string Title => "Accueil"; }

public partial class MainWindowViewModel : ViewModelBase
{
    // 2. Cette propriété contient la vue actuellement affichée à droite
    [ObservableProperty]
    private ViewModelBase _currentPage;

    public MainWindowViewModel()
    {
        // On définit la page par défaut au démarrage
        CurrentPage = new HomeViewModel();
    }

    // 3. Commandes pour changer de page
    [RelayCommand]
    public void GoHome() => CurrentPage = new HomeViewModel();
}