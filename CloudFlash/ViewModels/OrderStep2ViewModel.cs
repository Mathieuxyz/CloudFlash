using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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
    [ObservableProperty] private string _statusMessage = "Review your cart before confirming.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCartVisible))]
    private bool _isOrderConfirmed;

    [ObservableProperty]
    private string _confirmedOrderNumber = "";

    public bool IsCartVisible => !IsOrderConfirmed;

    public OrderStep2ViewModel(MainWindowViewModel main) 
    { 
        _main = main; 
        
        // If a customer is already selected, pre-fill the fields
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

            // If a customer was created via the Customer tab, use their ID
            if (_main.CurrentCustomer != null)
            {
                customerId = _main.CurrentCustomer.Id;
            }
            // Otherwise create the customer on the fly (quick purchase)
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

            // Auto-restocking: after updating stock, check for parts that have fallen below minimum
            var lowStockParts = await _main.Db.GetLowStockPartsAsync();
            int supplierOrdersCreated = 0;
            if (lowStockParts.Any())
            {
                var ordersToCreate = new Dictionary<int, List<Part>>();
                foreach (var part in lowStockParts)
                {
                    var bestSupplier = await _main.Db.GetBestSupplierForPartAsync(part.Code);
                    if (bestSupplier != null)
                    {
                        if (!ordersToCreate.ContainsKey(bestSupplier.SupplierId))
                            ordersToCreate[bestSupplier.SupplierId] = new List<Part>();
                        ordersToCreate[bestSupplier.SupplierId].Add(part);
                    }
                }
                foreach (var kvp in ordersToCreate)
                {
                    int supplierOrderId = await _main.Db.AddSupplierOrderAsync(new SupplierOrder
                    {
                        SupplierId   = kvp.Key,
                        Status       = "Pending",
                        ExpectedDate = DateTime.Now.AddDays(7)
                    });
                    foreach (var p in kvp.Value)
                        await _main.Db.AddSupplierOrderDetailAsync(new SupplierOrderDetail
                        {
                            SupplierOrderId = supplierOrderId,
                            PartCode        = p.Code,
                            Quantity        = (p.MinStock * 2) - p.InStock,
                            UnitPrice       = 0
                        });
                }
                supplierOrdersCreated = ordersToCreate.Count;
            }

            string restockNote = supplierOrdersCreated > 0
                ? $" {supplierOrdersCreated} supplier order(s) auto-generated."
                : " Stock is healthy, no restocking needed.";
            StatusMessage = $"Success! Order #{orderId} confirmed.{restockNote}";
            ConfirmedOrderNumber = $"ORDER #{orderId}";
            IsOrderConfirmed = true;
            _main.GlobalCart.Clear();
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }
}