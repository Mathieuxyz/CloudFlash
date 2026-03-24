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
    [ObservableProperty] private string _statusMessage = "Chargement des stocks...";

    public SupplierViewModel(MainWindowViewModel main) { _main = main; _ = LoadLowStockAsync(); }

    private async Task LoadLowStockAsync() {
        try { var parts = await _main.Db.GetLowStockPartsAsync(); LowStockParts.Clear(); foreach (var p in parts) LowStockParts.Add(p); StatusMessage = $"{LowStockParts.Count} pièces en stock critique."; }
        catch (Exception ex) { StatusMessage = $"Erreur : {ex.Message}"; }
    }

    [RelayCommand]
    public async Task GenerateOrdersAsync()
    {
        if (!LowStockParts.Any()) return; StatusMessage = "Génération des commandes...";
        try {
            var ordersToCreate = new Dictionary<int, List<Part>>();
            foreach (var part in LowStockParts) { var bestSupplier = await _main.Db.GetBestSupplierForPartAsync(part.Code); if (bestSupplier != null) { if (!ordersToCreate.ContainsKey(bestSupplier.SupplierId)) ordersToCreate[bestSupplier.SupplierId] = new List<Part>(); ordersToCreate[bestSupplier.SupplierId].Add(part); } }
            foreach (var kvp in ordersToCreate) {
                int orderId = await _main.Db.AddSupplierOrderAsync(new SupplierOrder { SupplierId = kvp.Key, Status = "Pending", ExpectedDate = DateTime.Now.AddDays(7) });
                foreach (var p in kvp.Value) await _main.Db.AddSupplierOrderDetailAsync(new SupplierOrderDetail { SupplierOrderId = orderId, PartCode = p.Code, Quantity = (p.MinStock * 2) - p.InStock, UnitPrice = 0 });
            }
            StatusMessage = $"{ordersToCreate.Count} commandes envoyées !"; await LoadLowStockAsync();
        }
        catch (Exception ex) { StatusMessage = $"Erreur : {ex.Message}"; }
    }
}