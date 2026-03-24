using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CloudFlash.Models;
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class OrderStep2ViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;

    public ObservableCollection<CartItem> Cart => _main.GlobalCart;
    public decimal TotalPrice => _main.TotalCabinetPrice;
    public decimal Deposit => _main.RequiredDeposit;
    public bool HasMissingParts => Deposit > 0;

    [ObservableProperty] private string _customerEmail = "";[ObservableProperty] private string _customerPhone = "";
    [ObservableProperty] private string _statusMessage = "Vérifiez le stock avant de confirmer.";

    public OrderStep2ViewModel(MainWindowViewModel main) 
    { 
        _main = main; 
        
        // Si le client est déjà connecté, on pré-remplit les champs
        if (_main.CurrentCustomer != null)
        {
            CustomerEmail = _main.CurrentCustomer.Email ?? "";
            CustomerPhone = _main.CurrentCustomer.Phone ?? "";
        }
    }

    [RelayCommand]
    public async Task ConfirmOrderAsync()
    {
        if (_main.DraftCabinet == null || !Cart.Any()) { StatusMessage = "Cart is empty!"; return; }
        if (string.IsNullOrEmpty(CustomerEmail) && string.IsNullOrEmpty(CustomerPhone)) { StatusMessage = "Email or Phone required."; return; }

        StatusMessage = "Saving order...";
        try
        {
            int customerId;

            // Si le client a créé un compte via l'onglet Customer, on utilise son ID
            if (_main.CurrentCustomer != null)
            {
                customerId = _main.CurrentCustomer.Id;
            }
            // Sinon, c'est un achat rapide, on le crée à la volée
            else
            {
                customerId = await _main.Db.AddCustomerAsync(new Customer { Email = CustomerEmail, Phone = CustomerPhone });
                _main.CurrentCustomer = new Customer { Id = customerId, Email = CustomerEmail, Phone = CustomerPhone };
            }

            int orderId = await _main.Db.AddOrderAsync(new Order { CustomerId = customerId, TotalAmount = TotalPrice, DepositAmount = Deposit, DepositPaid = Deposit == 0, Status = "Pending" });
            int cabId = await _main.Db.AddCabinetAsync(new Cabinet { OrderId = orderId, Quantity = 1, AngleIronPartCode = _main.DraftCabinet.AngleIronPartCode, AngleIronCutHeight = _main.DraftCabinet.AngleIronCutHeight, Width = _main.DraftCabinet.CabinetWidth, Depth = _main.DraftCabinet.CabinetDepth });

            foreach (var loc in _main.DraftCabinet.Lockers) {
                int locId = await _main.Db.AddLockerAsync(new Locker { CabinetId = cabId, Position = loc.Position, Height = loc.Height, Color = loc.Color, HasDoors = loc.HasDoors, DoorColor = loc.DoorColor });
                foreach (var group in loc.RawParts.GroupBy(p => p.Code)) await _main.Db.AddLockerPartAsync(locId, group.Key, group.Count());
            }

            foreach (var item in Cart) if (item.IsInStock) await _main.Db.UpdateStockAsync(item.PartInfo.Code, item.PartInfo.InStock - item.QuantityNeeded);

            StatusMessage = $"Success! Order #{orderId} confirmed."; _main.GlobalCart.Clear();
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }
}