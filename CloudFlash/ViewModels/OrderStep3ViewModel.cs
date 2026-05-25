using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class OrderStep3ViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private int _searchOrderId;
    [ObservableProperty] private Order? _currentOrder;
    [ObservableProperty] private string _statusMessage = "Entrez un numéro de commande pour suivre son état.";
    [ObservableProperty] private bool _isOrderFound;

    public OrderStep3ViewModel(MainWindowViewModel main, int orderId = 0)
    {
        _main = main;
        if (orderId > 0)
        {
            SearchOrderId = orderId;
            _ = LoadOrderAsync();
        }
    }

    [RelayCommand]
    public async Task LoadOrderAsync()
    {
        if (SearchOrderId <= 0)
        {
            StatusMessage = "Veuillez entrer un ID de commande valide.";
            IsOrderFound = false;
            return;
        }

        StatusMessage = "Recherche de la commande...";
        try
        {
            var order = await _main.Db.GetOrderByIdAsync(SearchOrderId);
            if (order != null)
            {
                CurrentOrder = order;
                StatusMessage = $"Commande N°{order.Id} trouvée.";
                IsOrderFound = true;
            }
            else
            {
                CurrentOrder = null;
                StatusMessage = "Commande introuvable.";
                IsOrderFound = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
            IsOrderFound = false;
        }
    }

    public bool CanMarkAsInvoiced => IsOrderFound && CurrentOrder?.Status == "Pending";

    partial void OnIsOrderFoundChanged(bool value)   => MarkAsInvoicedCommand.NotifyCanExecuteChanged();
    partial void OnCurrentOrderChanged(Order? value) => MarkAsInvoicedCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanMarkAsInvoiced))]
    public async Task MarkAsInvoicedAsync()
    {
        StatusMessage = "Facturation en cours...";
        try
        {
            await _main.Db.MarkOrderInvoicedAsync(CurrentOrder!.Id);
            await LoadOrderAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erreur : {ex.Message}";
        }
    }

    [RelayCommand]
    public void GoHome() => _main.GoHome();
}