using System.Text.Json.Serialization;

namespace ECommerceApp.RyanW84.Data.Models;

public class Product : BaseEntity
{
    public int ProductId { get; set; }
    public required string Name { get; set; } = null!;
    public required string Description { get; set; } = null!;
    public required decimal Price { get; set; }
    public required int Stock { get; set; }

    public required bool IsActive { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    // Don't serialize SaleItems to avoid circular references
    [JsonIgnore]
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
