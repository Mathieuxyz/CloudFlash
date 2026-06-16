using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class OrderStep3ViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;

    [ObservableProperty] private string _searchQuery = "";
    [ObservableProperty] private Order? _currentOrder;
    [ObservableProperty] private Customer? _currentCustomer;
    [ObservableProperty] private string _statusMessage = "Enter an order number, customer ID, email, or phone to track status.";
    [ObservableProperty] private bool _isOrderFound;
    [ObservableProperty] private bool _hasMultipleOrders;
    [ObservableProperty] private Order? _selectedOrder;

    public ObservableCollection<Order> FoundOrders { get; } = new();

    public OrderStep3ViewModel(MainWindowViewModel main, int orderId = 0)
    {
        _main = main;
        if (orderId > 0)
        {
            SearchQuery = orderId.ToString();
            _ = LoadOrderAsync();
        }
    }

    [RelayCommand]
    public async Task LoadOrderAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            StatusMessage = "Please enter an order number, customer ID, email, or phone.";
            IsOrderFound = false;
            FoundOrders.Clear();
            HasMultipleOrders = false;
            CurrentOrder = null;
            return;
        }

        string query = SearchQuery.Trim();
        StatusMessage = "Searching...";
        IsOrderFound = false;
        FoundOrders.Clear();
        HasMultipleOrders = false;
        CurrentOrder = null;

        try
        {
            var matchedOrders = new List<Order>();

            // 1. Try search by Order ID if it is a number
            if (int.TryParse(query, out int orderId))
            {
                var order = await _main.Db.GetOrderByIdAsync(orderId);
                if (order != null)
                {
                    matchedOrders.Add(order);
                }
            }

            // 2. Search customers to see if any match the query by Email, Phone, or Customer ID
            var customers = await _main.Db.GetAllCustomersAsync();
            var matchedCustomers = customers.Where(c =>
                (!string.IsNullOrEmpty(c.Email) && c.Email.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(c.Phone) && c.Phone.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (c.Id.ToString() == query)
            ).ToList();

            // 3. Retrieve all orders for matched customers
            foreach (var customer in matchedCustomers)
            {
                var orders = await _main.Db.GetOrdersByCustomerAsync(customer.Id);
                foreach (var o in orders)
                {
                    // Avoid duplicate orders in the results
                    if (matchedOrders.All(existing => existing.Id != o.Id))
                    {
                        matchedOrders.Add(o);
                    }
                }
            }

            // 4. Handle search results
            if (matchedOrders.Count == 0)
            {
                StatusMessage = "No orders found matching your search.";
            }
            else if (matchedOrders.Count == 1)
            {
                var singleOrder = matchedOrders[0];
                FoundOrders.Add(singleOrder);
                CurrentOrder = singleOrder;
                IsOrderFound = true;
                StatusMessage = $"Order #{singleOrder.Id} found.";
            }
            else
            {
                foreach (var o in matchedOrders.OrderByDescending(o => o.OrderDate))
                {
                    FoundOrders.Add(o);
                }
                HasMultipleOrders = true;
                StatusMessage = $"Found {matchedOrders.Count} orders. Please select one below.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    public bool CanMarkAsInvoiced => IsOrderFound && CurrentOrder?.Status == "Pending";

    partial void OnIsOrderFoundChanged(bool value)   => MarkAsInvoicedCommand.NotifyCanExecuteChanged();
    
    partial void OnCurrentOrderChanged(Order? value)
    {
        MarkAsInvoicedCommand.NotifyCanExecuteChanged();
        if (value != null)
        {
            _ = LoadCustomerForOrderAsync(value.CustomerId);
        }
        else
        {
            CurrentCustomer = null;
        }
    }

    partial void OnSelectedOrderChanged(Order? value)
    {
        if (value != null)
        {
            CurrentOrder = value;
            IsOrderFound = true;
        }
    }

    private async Task LoadCustomerForOrderAsync(int customerId)
    {
        try
        {
            CurrentCustomer = await _main.Db.GetCustomerByIdAsync(customerId);
        }
        catch
        {
            CurrentCustomer = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanMarkAsInvoiced))]
    public async Task MarkAsInvoicedAsync()
    {
        StatusMessage = "Processing invoice...";
        try
        {
            await _main.Db.MarkOrderInvoicedAsync(CurrentOrder!.Id);
            // Refresh order status
            var updatedOrder = await _main.Db.GetOrderByIdAsync(CurrentOrder.Id);
            if (updatedOrder != null)
            {
                CurrentOrder = updatedOrder;
                // update in FoundOrders if present
                for (int i = 0; i < FoundOrders.Count; i++)
                {
                    if (FoundOrders[i].Id == updatedOrder.Id)
                    {
                        FoundOrders[i] = updatedOrder;
                        break;
                    }
                }
            }
            StatusMessage = $"Order #{CurrentOrder.Id} marked as invoiced.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    public void GoHome() => _main.GoHome();
}
