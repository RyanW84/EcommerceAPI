using System.Text.Json;
using ECommerceApp.ConsoleClient.Helpers;
using ECommerceApp.ConsoleClient.Utilities;
using Spectre.Console;

namespace ECommerceApp.ConsoleClient.Handlers;

/// <summary>
/// Handles product menu operations.
/// Encapsulates all product-related UI logic following Single Responsibility Principle.
/// </summary>
public class ProductMenuHandler : IConsoleMenuHandler
{
    public string MenuName => "Products";

    public async Task ExecuteAsync(HttpClient http)
    {
        var actions = new Dictionary<string, Func<HttpClient, Task>>
        {
            { "List", ListAsync },
            { "Get by Id", h => ApiClient.GetByIdAsync(h, "/api/product/{id}", "Product") },
            { "By Category", h => ApiClient.GetByIdAsync(h, "/api/product/category/{id}", "CategoryId") },
            { "Create", CreateAsync },
            { "Update", UpdateAsync },
            { "Delete", DeleteAsync }
        };

        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[green]Products[/] — Choose an action:")
                .AddChoices(actions.Keys.Concat(new[] { "Back" }).ToList()));

            if (choice == "Back")
                return;

            if (actions.TryGetValue(choice, out var action))
                await action(http);
        }
    }

    private static async Task ListAsync(HttpClient http)
    {
        var (page, pageSize) = ConsoleInputHelper.PromptPagination();
        string? search = ConsoleInputHelper.PromptOptional("Search");
        decimal? minPrice = ConsoleInputHelper.PromptOptionalDecimal("Min Price");
        decimal? maxPrice = ConsoleInputHelper.PromptOptionalDecimal("Max Price");
        int? categoryId = ConsoleInputHelper.PromptOptionalInt("Category Id");
        var (sortBy, sortDirection) = PromptSortOptions(new[] { "(none)", "name", "price", "stock", "createdat", "category" });

        var query = new ProductListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            CategoryId = categoryId,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        await BuildAndExecuteListQuery(http, query);
    }

    private static async Task BuildAndExecuteListQuery(HttpClient http, ProductListQuery query)
    {
        var qs = new QueryStringBuilder()
            .Add("page", query.Page.ToString())
            .Add("pageSize", query.PageSize.ToString())
            .Add("search", query.Search)
            .Add("minPrice", query.MinPrice?.ToString())
            .Add("maxPrice", query.MaxPrice?.ToString())
            .Add("categoryId", query.CategoryId?.ToString())
            .Add("sortBy", query.SortBy == "(none)" ? null : query.SortBy)
            .Add("sortDirection", query.SortDirection)
            .Build();

        await ApiClient.GetAndRenderAsync(http, $"/api/product{qs}");
    }

    private static async Task CreateAsync(HttpClient http)
    {
        var name = ConsoleInputHelper.PromptRequired("Product Name");
        var description = ConsoleInputHelper.PromptRequired("Description");
        var price = ConsoleInputHelper.PromptPositiveDecimal("Price");
        var stock = ConsoleInputHelper.PromptNonNegativeInt("Stock");
        var categoryId = ConsoleInputHelper.PromptPositiveInt("Category ID");

        var payload = new
        {
            name,
            description,
            price,
            stock,
            categoryId,
            isActive = true
        };

        await ApiClient.PostAsync(http, "/api/product", payload);
    }

    private static async Task UpdateAsync(HttpClient http)
    {
        int productId = ConsoleInputHelper.PromptPositiveInt("Product ID");

        var productResponse = await ApiClient.FetchEntityAsync<ProductDto>(http, $"/api/product/{productId}");
        if (productResponse?.Data == null)
        {
            ConsoleInputHelper.DisplayError("Product not found");
            return;
        }

        var current = productResponse.Data;
        DisplayCurrentValues(current);

        var name = PromptOptionalField("Product Name", current.Name);
        var description = PromptOptionalField("Description", current.Description);
        var priceStr = AnsiConsole.Ask<string>("Price (leave blank to keep):", string.Empty);
        var stockStr = AnsiConsole.Ask<string>("Stock (leave blank to keep):", string.Empty);
        var categoryIdStr = AnsiConsole.Ask<string>("Category ID (leave blank to keep):", string.Empty);
        var isActiveStr = AnsiConsole.Ask<string>("Active? (yes/no, leave blank to keep):", string.Empty);

        var payload = new
        {
            productId,
            name,
            description,
            price = string.IsNullOrWhiteSpace(priceStr) ? current.Price : decimal.Parse(priceStr),
            stock = string.IsNullOrWhiteSpace(stockStr) ? current.Stock : int.Parse(stockStr),
            categoryId = string.IsNullOrWhiteSpace(categoryIdStr) ? current.CategoryId : int.Parse(categoryIdStr),
            isActive = string.IsNullOrWhiteSpace(isActiveStr) ? current.IsActive :
                isActiveStr.Equals("yes", StringComparison.OrdinalIgnoreCase)
        };

        await ApiClient.PutAsync(http, $"/api/product/{productId}", payload);
    }

    private static async Task DeleteAsync(HttpClient http)
    {
        int productId = ConsoleInputHelper.PromptPositiveInt("Product ID");

        if (!AnsiConsole.Confirm($"Delete product {productId}?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
            return;
        }

        await ApiClient.DeleteAsync(http, $"/api/product/{productId}");
    }

    private static void DisplayCurrentValues(ProductDto current)
    {
        AnsiConsole.MarkupLine($"[yellow]Current values:[/]");
        AnsiConsole.MarkupLine($"  Name: {current.Name}");
        AnsiConsole.MarkupLine($"  Description: {current.Description}");
        AnsiConsole.MarkupLine($"  Price: {current.Price:0.00}");
        AnsiConsole.MarkupLine($"  Stock: {current.Stock}");
        AnsiConsole.MarkupLine($"  Active: {current.IsActive}");
        AnsiConsole.MarkupLine($"  Category ID: {current.CategoryId}");
    }

    private static string? PromptOptionalField(string label, string? currentValue)
    {
        var input = AnsiConsole.Ask<string>($"{label} (leave blank to keep):", string.Empty);
        return string.IsNullOrWhiteSpace(input) ? currentValue : input;
    }

    private static (string SortBy, string? SortDirection) PromptSortOptions(string[] options)
    {
        var sortBy = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Sort By (optional)")
            .AddChoices(options));

        string? sortDirection = null;
        if (sortBy != "(none)")
        {
            sortDirection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Sort Direction")
                .AddChoices("asc", "desc"));
        }

        return (sortBy, sortDirection);
    }

    private sealed record ProductListQuery
    {
        public int Page { get; init; }
        public int PageSize { get; init; }
        public string? Search { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MaxPrice { get; init; }
        public int? CategoryId { get; init; }
        public string SortBy { get; init; } = "(none)";
        public string? SortDirection { get; init; }
    }

    private sealed record ProductDto
    {
        public int ProductId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public decimal Price { get; init; }
        public int Stock { get; init; }
        public bool IsActive { get; init; }
        public int CategoryId { get; init; }
    }
}
