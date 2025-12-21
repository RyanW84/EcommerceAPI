using System.Text.Json;
using ECommerceApp.ConsoleClient.Helpers;
using ECommerceApp.ConsoleClient.Utilities;
using Spectre.Console;

namespace ECommerceApp.ConsoleClient.Handlers;

/// <summary>
/// Handles sales menu operations.
/// Encapsulates all sales-related UI logic following Single Responsibility Principle.
/// Split into smaller methods to reduce cyclomatic complexity.
/// </summary>
public class SalesMenuHandler : IConsoleMenuHandler
{
    public string MenuName => "Sales";

    public async Task ExecuteAsync(HttpClient http)
    {
        var actions = new Dictionary<string, Func<HttpClient, Task>>
        {
            { "List", ListAsync },
            { "Get by Id", h => ApiClient.GetByIdAsync(h, "/api/sales/{id}", "Sale") },
            { "With Deleted Products (all)", h => ApiClient.GetAndRenderAsync(h, "/api/sales/with-deleted-products") },
            { "With Deleted Products (by Id)", h => ApiClient.GetByIdAsync(h, "/api/sales/{id}/with-deleted-products", "Sale") },
            { "Create", CreateAsync },
            { "Update", UpdateAsync },
            { "Delete", DeleteAsync }
        };

        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[green]Sales[/] — Choose an action:")
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
        DateTime? start = ConsoleInputHelper.PromptOptionalDate("Start Date (yyyy-MM-dd)");
        DateTime? end = ConsoleInputHelper.PromptOptionalDate("End Date (yyyy-MM-dd)");
        string? customerName = ConsoleInputHelper.PromptOptional("Customer Name");
        string? customerEmail = ConsoleInputHelper.PromptOptional("Customer Email");

        var (sortBy, sortDirection) = PromptSortOptions(new[] { "(none)", "saledate", "totalamount", "customername" });

        var qs = new QueryStringBuilder()
            .Add("page", page.ToString())
            .Add("pageSize", pageSize.ToString())
            .Add("startDate", start?.ToString("yyyy-MM-dd"))
            .Add("endDate", end?.ToString("yyyy-MM-dd"))
            .Add("customerName", customerName)
            .Add("customerEmail", customerEmail)
            .Add("sortBy", sortBy == "(none)" ? null : sortBy)
            .Add("sortDirection", sortDirection)
            .Build();

        await ApiClient.GetAndRenderAsync(http, $"/api/sales{qs}");
    }

    private static async Task CreateAsync(HttpClient http)
    {
        var customerName = ConsoleInputHelper.PromptRequired("Customer Name");
        var customerEmail = ConsoleInputHelper.PromptRequired("Customer Email");
        var customerAddress = ConsoleInputHelper.PromptRequired("Customer Address");
        var saleDate = AnsiConsole.Prompt(new TextPrompt<DateTime>("Sale Date (yyyy-MM-dd):")
            .DefaultValue(DateTime.Now));

        var saleItems = await PromptSaleItemsAsync();
        if (saleItems.Count == 0)
        {
            ConsoleInputHelper.DisplayError("Must add at least one sale item");
            return;
        }

        var payload = new { customerName, customerEmail, customerAddress, saleDate, saleItems };
        await ApiClient.PostAsync(http, "/api/sales", payload);
    }

    private static async Task UpdateAsync(HttpClient http)
    {
        int saleId = ConsoleInputHelper.PromptPositiveInt("Sale ID");

        var saleResponse = await ApiClient.FetchEntityAsync<SaleDto>(http, $"/api/sales/{saleId}");
        if (saleResponse?.Data == null)
        {
            ConsoleInputHelper.DisplayError("Sale not found");
            return;
        }

        var current = saleResponse.Data;
        DisplayCurrentSaleValues(current);

        var customerName = PromptOptionalField("Customer Name", current.CustomerName);
        var customerEmail = PromptOptionalField("Email", current.CustomerEmail);
        var saleDateStr = AnsiConsole.Ask<string>("Sale Date (leave blank to keep, format: yyyy-MM-dd):", string.Empty);

        var payload = new
        {
            saleId,
            customerName,
            customerEmail,
            saleDate = string.IsNullOrWhiteSpace(saleDateStr) ? current.SaleDate : DateTime.Parse(saleDateStr)
        };

        await ApiClient.PutAsync(http, $"/api/sales/{saleId}", payload);
    }

    private static async Task DeleteAsync(HttpClient http)
    {
        int saleId = ConsoleInputHelper.PromptPositiveInt("Sale ID");

        if (!AnsiConsole.Confirm($"Delete sale {saleId}?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
            return;
        }

        await ApiClient.DeleteAsync(http, $"/api/sales/{saleId}");
    }

    private static async Task<List<object>> PromptSaleItemsAsync()
    {
        var saleItems = new List<object>();
        AnsiConsole.MarkupLine("[yellow]Enter Sale Items[/]");

        while (true)
        {
            var addMore = AnsiConsole.Confirm("Add a sale item?");
            if (!addMore) 
                break;

            var productId = ConsoleInputHelper.PromptPositiveInt("Product ID");
            var quantity = ConsoleInputHelper.PromptPositiveInt("Quantity");
            saleItems.Add(new { productId, quantity });
        }

        return saleItems;
    }

    private static void DisplayCurrentSaleValues(SaleDto current)
    {
        AnsiConsole.MarkupLine($"[yellow]Current values:[/]");
        AnsiConsole.MarkupLine($"  Customer Name: {current.CustomerName}");
        AnsiConsole.MarkupLine($"  Email: {current.CustomerEmail}");
        AnsiConsole.MarkupLine($"  Sale Date: {current.SaleDate:yyyy-MM-dd}");
        AnsiConsole.MarkupLine($"  Total: {current.TotalAmount:0.00}");
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

    private sealed record SaleDto
    {
        public int SaleId { get; init; }
        public DateTime SaleDate { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerEmail { get; init; }
        public decimal TotalAmount { get; init; }
    }
}
