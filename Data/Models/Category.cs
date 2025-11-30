using System.Text.Json.Serialization;

namespace ECommerceApp.RyanW84.Data.Models;

public class Category : BaseEntity
{
    // EF Core needs settable properties for materialization
    public int CategoryId { get; set; }
    public required string Name { get; set; } = null!;
    public required string Description { get; set; } = null!;

    // Navigation property to Products - don't serialize to avoid circular references
    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();

    // Navigation property to Sales - don't serialize to avoid circular references
    [JsonIgnore]
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
}
