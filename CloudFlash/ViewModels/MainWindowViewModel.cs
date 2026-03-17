using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CloudFlash.Models;
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public readonly DataBaseServices Db;

    [ObservableProperty] private ViewModelBase _currentPage;
    
    public ObservableCollection<CartItem> GlobalCart { get; set; } = new();
    public decimal TotalCabinetPrice { get; set; }
    public decimal RequiredDeposit { get; set; }
    public OrderStep1ViewModel? DraftCabinet { get; set; }

    public MainWindowViewModel()
    {
        Db = new DataBaseServices();
        CurrentPage = new HomeViewModel(); 
    }

    [RelayCommand] public void GoHome() => CurrentPage = new HomeViewModel();
    [RelayCommand] public void GoToStep1() => CurrentPage = new OrderStep1ViewModel(this);
    [RelayCommand] public void GoToStep2() => CurrentPage = new OrderStep2ViewModel(this);
    [RelayCommand] public void GoToSupplier() => CurrentPage = new SupplierViewModel(this);
}