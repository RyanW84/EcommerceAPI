namespace ECommerceApp.RyanW84.Data.Models;

public class SalesSummary
{
    public string ProductName { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime LastSaleDate { get; set; }
}
