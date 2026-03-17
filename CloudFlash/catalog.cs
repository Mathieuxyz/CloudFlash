public class Catalog
{
    public int Id {get; set;}
    public string PartCode {get; set;} = string.Empty;
    public int SupplierId {get; set;}
    public decimal Price {get; set;}
    public int DeliveryTime {get; set;} 
}