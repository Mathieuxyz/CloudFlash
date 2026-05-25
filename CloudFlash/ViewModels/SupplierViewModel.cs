using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using SGS.Services;

namespace CloudFlash.ViewModels;

public partial class SupplierViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;
    public ObservableCollection<Part> LowStockParts { get; } = new();
    public ObservableCollection<Part> AllParts      { get; } = new();
    [ObservableProperty] private string _statusMessage = "Chargement des stocks...";

    public SupplierViewModel(MainWindowViewModel main) { _main = main; _ = LoadStockAsync(); }

    private async Task LoadStockAsync()
    {
        try
        {
            var low = await _main.Db.GetLowStockPartsAsync();
            LowStockParts.Clear();
            foreach (var p in low) LowStockParts.Add(p);

            var all = await _main.Db.GetAllPartsAsync();
            AllParts.Clear();
            foreach (var p in all) AllParts.Add(p);

            StatusMessage = $"{LowStockParts.Count} part(s) below minimum stock.";
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }

    // kept for callers that only need a refresh
    private Task LoadLowStockAsync() => LoadStockAsync();

    [RelayCommand]
    public async Task GenerateOrdersAsync()
    {
        if (!LowStockParts.Any()) { StatusMessage = "No low-stock parts found."; return; }
        StatusMessage = "Generating orders...";
        try {
            var ordersToCreate = new Dictionary<int, List<Part>>();
            foreach (var part in LowStockParts) { var bestSupplier = await _main.Db.GetBestSupplierForPartAsync(part.Code); if (bestSupplier != null) { if (!ordersToCreate.ContainsKey(bestSupplier.SupplierId)) ordersToCreate[bestSupplier.SupplierId] = new List<Part>(); ordersToCreate[bestSupplier.SupplierId].Add(part); } }
            foreach (var kvp in ordersToCreate) {
                int orderId = await _main.Db.AddSupplierOrderAsync(new SupplierOrder { SupplierId = kvp.Key, Status = "Pending", ExpectedDate = DateTime.Now.AddDays(7) });
                foreach (var p in kvp.Value) await _main.Db.AddSupplierOrderDetailAsync(new SupplierOrderDetail { SupplierOrderId = orderId, PartCode = p.Code, Quantity = (p.MinStock * 2) - p.InStock, UnitPrice = 0 });
            }
            StatusMessage = $"{ordersToCreate.Count} order(s) sent!"; await LoadLowStockAsync();
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }

    [RelayCommand]
    public async Task ReceiveAllOrdersAsync()
    {
        StatusMessage = "Receiving orders...";
        try
        {
            var pending = await _main.Db.GetPendingSupplierOrdersAsync();
            if (!pending.Any()) { StatusMessage = "No pending supplier orders to receive."; return; }
            foreach (var order in pending)
                await _main.Db.ReceiveSupplierOrderAsync(order.Id);
            StatusMessage = $"{pending.Count} order(s) received. Stock updated.";
            await LoadLowStockAsync();
        }
        catch (Exception ex) { StatusMessage = $"Error: {ex.Message}"; }
    }

}