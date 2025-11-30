using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ECommerceApp.RyanW84.Data.Models;

public class Sale
{
    // EF Core needs settable properties for materialization
    public int SaleId { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public required string CustomerName { get; set; } = null!;
    public required string CustomerEmail { get; set; } = null!;
    public required string CustomerAddress { get; set; } = null!;

    // Sale has many items (each item references a product)
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    // Don't serialize Categories to avoid circular references
    [JsonIgnore]
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
