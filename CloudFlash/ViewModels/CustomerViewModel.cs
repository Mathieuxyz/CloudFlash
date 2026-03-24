using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class CustomersViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;

    public ObservableCollection<Customer> AllCustomers { get; } = new();

    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _statusMessage = "Enter details to create or view customers.";

    public CustomersViewModel(MainWindowViewModel main)
    {
        _main = main;
        _ = LoadCustomersAsync();
    }

    public async Task LoadCustomersAsync()
    {
        try
        {
            // Note: Si GetAllCustomersAsync() n'existe pas encore dans tes services, il faudra l'ajouter.
            var customers = await _main.Db.GetAllCustomersAsync();
            AllCustomers.Clear();
            foreach (var c in customers) AllCustomers.Add(c);
            StatusMessage = $"{AllCustomers.Count} customer(s) found.";
        }
        catch (Exception ex) { StatusMessage = $"Error loading: {ex.Message}"; }
    }

    [RelayCommand]
    public async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) && string.IsNullOrWhiteSpace(Phone))
        {
            StatusMessage = "Error: Email or Phone required.";
            return;
        }

        try
        {
            var newCustomer = new Customer { Email = Email, Phone = Phone };
            int id = await _main.Db.AddCustomerAsync(newCustomer);
            newCustomer.Id = id;
            
            _main.CurrentCustomer = newCustomer; // Sélectionne le client pour la commande en cours
            
            StatusMessage = $"Success! Customer #{id} created.";
            Email = ""; Phone = "";
            await LoadCustomersAsync(); // Rafraîchit la liste
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }
}