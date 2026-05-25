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
    [ObservableProperty] private string _statusMessage = "Enter an order number to track its status.";
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
            StatusMessage = "Please enter a valid order ID.";
            IsOrderFound = false;
            return;
        }

        StatusMessage = "Looking up order...";
        try
        {
            var order = await _main.Db.GetOrderByIdAsync(SearchOrderId);
            if (order != null)
            {
                CurrentOrder = order;
                StatusMessage = $"Order #{order.Id} found.";
                IsOrderFound = true;
            }
            else
            {
                CurrentOrder = null;
                StatusMessage = "Order not found.";
                IsOrderFound = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsOrderFound = false;
        }
    }

    public bool CanMarkAsInvoiced => IsOrderFound && CurrentOrder?.Status == "Pending";

    partial void OnIsOrderFoundChanged(bool value)   => MarkAsInvoicedCommand.NotifyCanExecuteChanged();
    partial void OnCurrentOrderChanged(Order? value) => MarkAsInvoicedCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanMarkAsInvoiced))]
    public async Task MarkAsInvoicedAsync()
    {
        StatusMessage = "Processing invoice...";
        try
        {
            await _main.Db.MarkOrderInvoicedAsync(CurrentOrder!.Id);
            await LoadOrderAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void GoHome() => _main.GoHome();
}