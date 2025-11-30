namespace ECommerceApp.ConsoleClient.Models;

public class ApiResponse<T>
{
    public bool RequestFailed { get; set; }
    public int ResponseCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public T? Data { get; set; }
    public bool Success => !RequestFailed;
}

public class PaginatedResponse<T>
{
    public bool RequestFailed { get; set; }
    public int ResponseCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool Success => !RequestFailed;
}

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = new();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class Category
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class Sale
{
    public int SaleId { get; init; }
    public DateTime SaleDate { get; init; }
    public decimal TotalAmount { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerAddress { get; init; } = string.Empty;
    public List<SaleItem> SaleItems { get; init; } = [];
}

public class SaleItem
{
    public int ProductId { get; set; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
    public Product? Product { get; init; }
}

public record ProductQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? CategoryId = null,
    string? SortBy = null,
    string? SortDirection = null
);

public record CategoryQuery(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null,
    bool IncludeDeleted = false
);

public record SaleQuery(
    int Page = 1,
    int PageSize = 10,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    string? CustomerName = null,
    string? CustomerEmail = null,
    string? SortBy = null,
    string? SortDirection = null
);

public class ApiRequest<T>(T payload)
    where T : class
{
    public T Payload
    {
        get => field;
        set;
    } = payload;
}
