using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input; // Required for RelayCommand
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DataBaseServices _dbService;

    [ObservableProperty]
    private string _greeting = "Welcome to Avalonia!";

    [ObservableProperty]
    private string _connectionStatus = "Not checked";

    [ObservableProperty]
    private bool _isBusy;

    public MainWindowViewModel()
    {
        // Ideally, use Dependency Injection, but for a quick check:
        _dbService = new DataBaseServices();
    }

    [RelayCommand]
    public async Task CheckConnectionAsync()
    {
        IsBusy = true;
        ConnectionStatus = "Connecting via SSH Tunnel...";

        try
        {
            // We'll call a simple query to verify the link
            // Adjust "Users" and "Id" to a table/column that actually exists in your DB
            var data = await _dbService.GetTableDataAsync("information_schema.tables", "TABLE_NAME");

            if (data.Count > 0)
            {
                ConnectionStatus = "Success! Connected to MariaDB via SSH.";
            }
            else
            {
                ConnectionStatus = "Connected, but no data found.";
            }
        }
        catch (System.Exception ex)
        {
            ConnectionStatus = $"Failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}