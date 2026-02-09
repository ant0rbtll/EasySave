using CommunityToolkit.Mvvm.ComponentModel;

namespace EasySave.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public SidebarViewModel Sidebar { get; }

    // La page courante affichée dans ContentControl
    [ObservableProperty]
    private ViewModelBase currentPage;

    // ViewModels des différentes pages
    private readonly CreateViewModel _createViewModel;
    private readonly ManageViewModel _manageViewModel;
    private readonly ProgressViewModel _progressViewModel;
    private readonly LogViewModel _logViewModel;
    private readonly ConfigViewModel _configViewModel;

    public MainWindowViewModel()
    {
        // Initialisation des ViewModels
        _createViewModel = new CreateViewModel();
        _manageViewModel = new ManageViewModel();
        _progressViewModel = new ProgressViewModel();
        _logViewModel = new LogViewModel();
        _configViewModel = new ConfigViewModel();

        Sidebar = new SidebarViewModel(Navigate);
        currentPage = _createViewModel; // page par défaut
    }

    public void Navigate(string page)
    {
        CurrentPage = page switch
        {
            "creation" => _createViewModel,
            "manage" => _manageViewModel,
            "progress" => _progressViewModel,
            "log" => _logViewModel,
            "conf" => _configViewModel,
            _ => CurrentPage
        };
    }
}
