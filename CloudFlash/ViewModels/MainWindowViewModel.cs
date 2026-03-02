using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SGS.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;

namespace CloudFlash.ViewModels;

public class HomeViewModel : ViewModelBase { public string Title => "Home"; }

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly DataBaseServices _db = new DataBaseServices();

    [ObservableProperty]
    private ViewModelBase _currentPage;

    // The list the UI binds to
    [ObservableProperty]
    private ObservableCollection<Part> _parts = new();

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _statusMessage = "Press 'Load Parts' to test the database.";

    public MainWindowViewModel()
    {
        CurrentPage = new HomeViewModel();
    }

    [RelayCommand]
    public void GoHome() => CurrentPage = new HomeViewModel();

    // ----------------------------------------------------------------
    // TEST 1: Load all parts from the DB
    // ----------------------------------------------------------------
    [RelayCommand]
    public async Task LoadPartsAsync()
    {
        StatusMessage = "Connecting to database...";
        Parts.Clear();

        try
        {
            var allParts = await _db.GetAllPartsAsync();
            foreach (var p in allParts)
                Parts.Add(p);

            StatusMessage = $"OK — {Parts.Count} parts loaded.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ERROR: {ex.Message}";
        }
    }

    // ----------------------------------------------------------------
    // TEST 2: Filter by search text
    // ----------------------------------------------------------------
    [RelayCommand]
    public async Task SearchPartsAsync()
    {
        StatusMessage = "Searching...";
        Parts.Clear();

        try
        {
            var allParts = await _db.GetAllPartsAsync();
            var query = SearchText.Trim().ToLower();

            foreach (var p in allParts)
            {
                if (string.IsNullOrEmpty(query)          ||
                    p.Code.ToLower().Contains(query)     ||
                    p.Kind.ToLower().Contains(query)     ||
                    (p.Color?.ToLower().Contains(query) ?? false))
                {
                    Parts.Add(p);
                }
            }

            StatusMessage = $"{Parts.Count} parts found.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"ERROR: {ex.Message}";
        }
    }
}