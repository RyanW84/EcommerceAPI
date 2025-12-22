using System.Text.Json;
using ECommerceApp.ConsoleClient.Helpers;
using ECommerceApp.ConsoleClient.Interfaces;
using ECommerceApp.ConsoleClient.Models;
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
            { "Get by Id", GetByIdAsync },
            { "By Category", GetByCategoryAsync },
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

    private static async Task GetByIdAsync(HttpClient http)
    {
        // First, show a list for the user to select from
        var (page, pageSize) = (1, 32);
        var qs = new QueryStringBuilder()
            .Add("page", page.ToString())
            .Add("pageSize", pageSize.ToString())
            .Build();

        var response = await ApiClient.FetchPaginatedAsync<ProductDto>(http, $"/api/product{qs}");
        if (response?.Data == null || response.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No products available[/]");
            return;
        }

        var pagination = new PaginationState
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = response.TotalCount
        };

        var selected = TableRenderer.SelectFromPrompt(response.Data, "Select a Product", pagination.IndexOffset, "Name");
        if (selected != null)
        {
            // Display the selected product directly from the list
            TableRenderer.DisplayTable(new[] { selected }.ToList(), "Product Details", pagination.IndexOffset, "CategoryId");
        }
    }

    private static async Task GetByCategoryAsync(HttpClient http)
    {
        int categoryId = ConsoleInputHelper.PromptPositiveInt("Category ID");
        var qs = new QueryStringBuilder()
            .Add("page", "1")
            .Add("pageSize", "50")
            .Build();

        var response = await ApiClient.FetchPaginatedAsync<ProductDto>(http, $"/api/product/category/{categoryId}{qs}");
        if (response?.Data != null && response.Data.Count > 0)
        {
            TableRenderer.DisplayTable(response.Data, $"Products in Category {categoryId}", 0, "CategoryId");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No products found in this category[/]");
        }
    }

    private static async Task ListAsync(HttpClient http)
    {
        var pageSize = ConsoleInputHelper.PromptPositiveInt("Items per page");

        // Ask if user wants to apply filters
        var applyFilters = AnsiConsole.Confirm("Apply filters?", false);

        string? search = null;
        decimal? minPrice = null;
        decimal? maxPrice = null;
        int? categoryId = null;
        string? sortBy = null;
        string? sortDirection = null;

        if (applyFilters)
        {
            search = ConsoleInputHelper.PromptOptional("Search");
            minPrice = ConsoleInputHelper.PromptOptionalDecimal("Min Price");
            maxPrice = ConsoleInputHelper.PromptOptionalDecimal("Max Price");
            categoryId = ConsoleInputHelper.PromptOptionalInt("Category Id");
            var (sortByResult, sortDirectionResult) = PromptSortOptions(new[] { "(none)", "name", "price", "stock", "createdat", "category" });
            sortBy = sortByResult;
            sortDirection = sortDirectionResult;
        }

        var query = new ProductListQuery
        {
            Page = 1,
            PageSize = pageSize,
            Search = search,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            CategoryId = categoryId,
            SortBy = sortBy ?? "(none)",
            SortDirection = sortDirection
        };

        await BuildAndExecuteListQueryWithPagination(http, query);
    }

    private static async Task BuildAndExecuteListQueryWithPagination(HttpClient http, ProductListQuery query)
    {
        while (true)
        {
            var qs = new QueryStringBuilder()
                .Add("page", query.Page.ToString())
                .Add("pageSize", query.PageSize.ToString())
                .Add("search", query.Search)
                .Add("minPrice", query.MinPrice?.ToString())
                .Add("maxPrice", query.MaxPrice?.ToString())
                .Add("categoryId", query.CategoryId?.ToString())
                .Add("sortBy", query.SortBy == "(none)" || query.SortBy == null ? null : query.SortBy)
                .Add("sortDirection", query.SortDirection)
                .Build();

            var response = await ApiClient.FetchPaginatedAsync<ProductDto>(http, $"/api/product{qs}");
            if (response?.Data == null || response.Data.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No products found[/]");
                break;
            }

            var pagination = new PaginationState
            {
                CurrentPage = query.Page,
                PageSize = query.PageSize,
                TotalCount = response.TotalCount
            };

            TableRenderer.DisplayTable(
                response.Data,
                $"Products (Page {pagination.CurrentPage}/{pagination.TotalPages}, Total: {pagination.TotalCount})",
                pagination.IndexOffset,
                "CategoryId"
            );

            if (!HandlePaginationNavigation(pagination, query))
                break;
        }
    }

    private static bool HandlePaginationNavigation(PaginationState pagination, ProductListQuery query)
    {
        var navChoice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Navigation:")
            .AddChoices(pagination.GetNavigationChoices()));

        if (navChoice == "Back to Menu")
            return false;

        if (navChoice == "Next Page")
        {
            query.Page++;
        }
        else if (navChoice == "Previous Page")
        {
            query.Page--;
        }
        else if (navChoice == "Jump to Page")
        {
            int targetPage = ConsoleInputHelper.PromptPositiveInt($"Enter page number (1-{pagination.TotalPages})");
            if (targetPage >= 1 && targetPage <= pagination.TotalPages)
                query.Page = targetPage;
            else
                AnsiConsole.MarkupLine($"[red]Invalid page number. Valid range: 1-{pagination.TotalPages}[/]");
        }

        return true;
    }

    private static async Task CreateAsync(HttpClient http)
    {
        // Show list of categories to select from
        var categoriesResponse = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, "/api/categories?page=1&pageSize=32");
        if (categoriesResponse?.Data == null || categoriesResponse.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No categories available. Please create a category first.[/]");
            return;
        }

        var selectedCategory = TableRenderer.SelectFromPrompt(categoriesResponse.Data, "Select a Category", 0, "Name");
        if (selectedCategory == null)
            return;

        var name = ConsoleInputHelper.PromptRequired("Product Name");
        var description = ConsoleInputHelper.PromptRequired("Description");
        var price = ConsoleInputHelper.PromptPositiveDecimal("Price");
        var stock = ConsoleInputHelper.PromptNonNegativeInt("Stock");

        var product = new
        {
            name,
            description,
            price,
            stock,
            categoryId = selectedCategory.CategoryId,
            isActive = true
        };

        var payload = new { payload = product };
        await ApiClient.PostAsync(http, "/api/product", payload);
    }

    private static async Task UpdateAsync(HttpClient http)
    {
        // Show list for selection
        var qs = new QueryStringBuilder()
            .Add("page", "1")
            .Add("pageSize", "50")
            .Build();

        var response = await ApiClient.FetchPaginatedAsync<ProductDto>(http, $"/api/product{qs}");
        if (response?.Data == null || response.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No products available[/]");
            return;
        }

        var pagination = new PaginationState
        {
            CurrentPage = 1,
            PageSize = 32,
            TotalCount = response.TotalCount
        };

        var selected = TableRenderer.SelectFromPrompt(response.Data, "Select a Product to Update", pagination.IndexOffset, "Name");
        if (selected == null)
            return;

        var productResponse = await ApiClient.FetchEntityAsync<ProductDto>(http, $"/api/product/{selected.ProductId}");
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

        // Show category selection list instead of ID prompt
        var categoriesResponse = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, "/api/categories?page=1&pageSize=32");
        int categoryId = current.CategoryId;
        if (categoriesResponse?.Data != null && categoriesResponse.Data.Count > 0)
        {
            var changeCategoryChoice = AnsiConsole.Confirm("Change category?", false);
            if (changeCategoryChoice)
            {
                var selectedCategory = TableRenderer.SelectFromPrompt(categoriesResponse.Data, "Select a Category", 0, "Name");
                if (selectedCategory != null)
                    categoryId = selectedCategory.CategoryId;
            }
        }

        var isActiveStr = AnsiConsole.Ask<string>("Active? (yes/no, leave blank to keep):", string.Empty);

        var product = new
        {
            productId = selected.ProductId,
            name,
            description,
            price = string.IsNullOrWhiteSpace(priceStr) ? current.Price : decimal.Parse(priceStr),
            stock = string.IsNullOrWhiteSpace(stockStr) ? current.Stock : int.Parse(stockStr),
            categoryId = categoryId,
            isActive = string.IsNullOrWhiteSpace(isActiveStr) ? current.IsActive :
                isActiveStr.Equals("yes", StringComparison.OrdinalIgnoreCase)
        };

        var payload = new { payload = product };
        await ApiClient.PutAsync(http, $"/api/product/{selected.ProductId}", payload);
    }

    private static async Task DeleteAsync(HttpClient http)
    {
        // Show list for selection
        var qs = new QueryStringBuilder()
            .Add("page", "1")
            .Add("pageSize", "32")
            .Build();

        var response = await ApiClient.FetchPaginatedAsync<ProductDto>(http, $"/api/product{qs}");
        if (response?.Data == null || response.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No products available[/]");
            return;
        }

        var pagination = new PaginationState
        {
            CurrentPage = 1,
            PageSize = 32,
            TotalCount = response.TotalCount
        };

        var selected = TableRenderer.SelectFromPrompt(response.Data, "Select a Product to Delete", pagination.IndexOffset, "Name");
        if (selected == null)
            return;

        if (!AnsiConsole.Confirm($"[red]Are you sure you want to delete '{selected.Name}'?[/]"))
            return;
        await ApiClient.DeleteAsync(http, $"/api/product/{selected.ProductId}");
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
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string? Search { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? CategoryId { get; set; }
        public string SortBy { get; set; } = "(none)";
        public string? SortDirection { get; set; }
    }

    private sealed record CategoryDto
    {
        public int CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
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
