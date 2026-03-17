using SGS.Services;

namespace CloudFlash.Models;

public class CartItem
{
    public Part PartInfo { get; set; } = new();
    public int QuantityNeeded { get; set; }
    public bool IsInStock => PartInfo.InStock >= QuantityNeeded;
    public string StatusIcon => IsInStock ? "✅" : "⚠️";
}