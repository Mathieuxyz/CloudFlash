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

public partial class LockerItemViewModel : ViewModelBase
{
    [ObservableProperty] private decimal _height = 42;
    [ObservableProperty] private string _color = "White";[ObservableProperty] private bool _hasDoors = false;
    [ObservableProperty] private string _doorColor = "White";
    public int Position { get; set; }
    public ObservableCollection<string> CalculatedParts { get; } = new();
    public List<Part> RawParts { get; } = new();
}

public partial class OrderStep1ViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _main;[ObservableProperty] private decimal _cabinetWidth = 62;
    [ObservableProperty] private decimal _cabinetDepth = 42;
    [ObservableProperty] private string _angleIronColor = "White";[ObservableProperty] private decimal _angleIronCutHeight = 0;
    [ObservableProperty] private string _angleIronPartCode = "N/A";
    [ObservableProperty] private Part? _selectedAngleIron;
    [ObservableProperty] private string _statusMessage = "Ready to configure (max 7 lockers).";

    public ObservableCollection<LockerItemViewModel> Lockers { get; } = new();

    public OrderStep1ViewModel(MainWindowViewModel main) { _main = main; AddLocker(); }[RelayCommand] public void AddLocker() { if (Lockers.Count < 7) Lockers.Add(new LockerItemViewModel { Position = Lockers.Count + 1 }); else StatusMessage = "Maximum 7 lockers."; }
    [RelayCommand] public void RemoveLocker(LockerItemViewModel locker) { if (Lockers.Count > 1) { Lockers.Remove(locker); for (int i=0; i<Lockers.Count; i++) Lockers[i].Position = i+1; } }

    [RelayCommand]
    public async Task CalculatePartsAsync()
    {
        StatusMessage = "Calculating parts...";
        try
        {
            var allParts = await _main.Db.GetAllPartsAsync();
            AngleIronCutHeight = 0;
            _main.GlobalCart.Clear();
            var partCounts = new Dictionary<Part, int>();

            void AddPart(Part? p, int qty, LockerItemViewModel l, string desc) {
                if (p == null) return; l.CalculatedParts.Add($"{qty}x {p.Code} ({desc})"); l.RawParts.Add(p);
                if (partCounts.ContainsKey(p)) partCounts[p] += qty; else partCounts[p] = qty;
            }

            foreach (var locker in Lockers)
            {
                locker.CalculatedParts.Clear(); locker.RawParts.Clear();
                AngleIronCutHeight += locker.Height;
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Vertical Batten") && p.Height == locker.Height), 4, locker, "Vertical Batten");
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Front crossbar") && p.Width == CabinetWidth), 2, locker, "Front crossbar");
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Back crossbar") && p.Width == CabinetWidth), 2, locker, "Back crossbar");
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Left or right crossbar") && p.Depth == CabinetDepth), 4, locker, "Side crossbar");
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Bottom or top panel") && p.Width == CabinetWidth && p.Depth == CabinetDepth && (p.Color == locker.Color || p.Color == null)), 2, locker, "Bottom/top panel");
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Left or right panel") && p.Height == locker.Height && p.Depth == CabinetDepth && (p.Color == locker.Color || p.Color == null)), 2, locker, "Side panel");
                AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Back panel") && p.Height == locker.Height && p.Width == CabinetWidth && (p.Color == locker.Color || p.Color == null)), 1, locker, "Back panel");

                if (locker.HasDoors) {
                    AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Door") && p.Height == locker.Height && p.Width == CabinetWidth / 2 && p.Color == locker.DoorColor), 2, locker, "Door");
                    if (locker.DoorColor != "Glass") AddPart(allParts.FirstOrDefault(p => p.Kind.Contains("Cup handle")), 2, locker, "Cup handle");
                }
            }

            SelectedAngleIron = allParts.Where(p => p.Kind.Contains("Angle iron") && p.Color == AngleIronColor && p.Height >= AngleIronCutHeight).OrderBy(p => p.Height).FirstOrDefault();
            if (SelectedAngleIron != null) { AngleIronPartCode = SelectedAngleIron.Code; if (partCounts.ContainsKey(SelectedAngleIron)) partCounts[SelectedAngleIron] += 4; else partCounts[SelectedAngleIron] = 4; }
            else { AngleIronPartCode = "Introuvable!"; }

            _main.TotalCabinetPrice = 0;
            foreach (var kvp in partCounts) { _main.GlobalCart.Add(new CartItem { PartInfo = kvp.Key, QuantityNeeded = kvp.Value }); _main.TotalCabinetPrice += (kvp.Key.CustomerPrice * kvp.Value); }
            
            _main.RequiredDeposit = _main.GlobalCart.Any(c => !c.IsInStock) ? _main.TotalCabinetPrice / 2 : 0;
            _main.DraftCabinet = this;
            StatusMessage = "Calculation complete!";
        }
        catch (Exception ex) { StatusMessage = $"Erreur DB : {ex.Message}"; }
    }

    [RelayCommand] public void GoToCart() => _main.GoToStep2Command.Execute(null);
}