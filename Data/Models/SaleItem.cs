using System.Text.Json.Serialization;

namespace ECommerceApp.RyanW84.Data.Models;

public class SaleItem
{
    public int SaleId { get; set; }

    // Don't serialize Sale to avoid circular references
    [JsonIgnore]
    public Sale? Sale { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
