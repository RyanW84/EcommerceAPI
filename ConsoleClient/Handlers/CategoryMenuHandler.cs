using ECommerceApp.ConsoleClient.Helpers;
using ECommerceApp.ConsoleClient.Interfaces;
using ECommerceApp.ConsoleClient.Models;
using ECommerceApp.ConsoleClient.Utilities;
using Spectre.Console;

namespace ECommerceApp.ConsoleClient.Handlers;

/// <summary>
/// Handles category menu operations.
/// Encapsulates all category-related UI logic following Single Responsibility Principle.
/// </summary>
public class CategoryMenuHandler : IConsoleMenuHandler
{
    public string MenuName => "Categories";

    public async Task ExecuteAsync(HttpClient http)
    {
        var actions = new Dictionary<string, Func<HttpClient, Task>>
        {
            { "List", ListAsync },
            { "Get by Id", GetByIdAsync },
            { "Get by Name", GetByNameAsync },
            { "Create", CreateAsync },
            { "Update", UpdateAsync },
            { "Delete", DeleteAsync },
        };

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Categories[/] — Choose an action:")
                    .AddChoices(actions.Keys.Concat(new[] { "Back" }).ToList())
            );

            if (choice == "Back")
                return;

            if (actions.TryGetValue(choice, out var action))
                await action(http);
        }
    }

    private static async Task ListAsync(HttpClient http)
    {
        var pageSize = ConsoleInputHelper.PromptPositiveInt("Items per page");

        // Ask if user wants to apply filters
        var applyFilters = AnsiConsole.Confirm("Apply filters?", false);

        string? search = null;
        string? sortBy = null;
        string? sortDirection = null;

        if (applyFilters)
        {
            search = ConsoleInputHelper.PromptOptional("Search");
            var (sortByResult, sortDirectionResult) = PromptSortOptions(new[] { "(none)", "name", "createdat" });
            sortBy = sortByResult;
            sortDirection = sortDirectionResult;
        }

        var query = new CategoryListQuery
        {
            Page = 1,
            PageSize = pageSize,
            Search = search,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        await BuildAndExecuteListQueryWithPagination(http, query);
    }

    private static async Task BuildAndExecuteListQueryWithPagination(HttpClient http, CategoryListQuery query)
    {
        while (true)
        {
            var qs = new QueryStringBuilder()
                .Add("page", query.Page.ToString())
                .Add("pageSize", query.PageSize.ToString())
                .Add("search", query.Search)
                .Add("sortBy", query.SortBy == "(none)" || query.SortBy == null ? null : query.SortBy)
                .Add("sortDirection", query.SortDirection)
                .Build();

            var response = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, $"/api/categories{qs}");
            if (response?.Data != null && response.Data.Count > 0)
            {
                var pagination = new PaginationState
                {
                    CurrentPage = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = response.TotalCount
                };

                TableRenderer.DisplayTable(
                    response.Data,
                    $"Categories (Page {pagination.CurrentPage}/{pagination.TotalPages}, Total: {pagination.TotalCount})",
                    pagination.IndexOffset
                );

                // Show pagination navigation
                var navChoice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Navigation:")
                    .AddChoices(pagination.GetNavigationChoices()));

                if (navChoice == "Back to Menu")
                    break;
                else if (navChoice == "Next Page")
                    query.Page++;
                else if (navChoice == "Previous Page")
                    query.Page--;
                else if (navChoice == "Jump to Page")
                {
                    int targetPage = ConsoleInputHelper.PromptPositiveInt($"Enter page number (1-{pagination.TotalPages})");
                    if (targetPage >= 1 && targetPage <= pagination.TotalPages)
                        query.Page = targetPage;
                    else
                        AnsiConsole.MarkupLine($"[red]Invalid page number. Valid range: 1-{pagination.TotalPages}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]No categories found[/]");
                break;
            }
        }
    }

    private static async Task GetByIdAsync(HttpClient http)
    {
        var response = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, "/api/categories?page=1&pageSize=32");
        if (response?.Data == null || response.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No categories available[/]");
            return;
        }

        var selected = await TableRenderer.SelectFromPromptAsync(
            async (pageNum) =>
            {
                var pageResponse = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, $"/api/categories?page={pageNum}&pageSize=32");
                return pageResponse?.Data ?? new List<CategoryDto>();
            },
            response.TotalCount,
            32,
            "Select a Category",
            cat => cat.Name
        );

        if (selected != null)
        {
            TableRenderer.DisplayTable(new[] { selected }.ToList(), "Category Details");
        }
    }

    private static async Task GetByNameAsync(HttpClient http)
    {
        var name = ConsoleInputHelper.PromptRequired("Name");
        var response = await ApiClient.FetchEntityAsync<CategoryDto>(http, $"/api/categories/name/{Uri.EscapeDataString(name)}");
        if (response?.Data != null)
        {
            TableRenderer.DisplayTable(new[] { response.Data }.ToList(), "Category Details");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Category not found[/]");
        }
    }

    private static async Task CreateAsync(HttpClient http)
    {
        var name = ConsoleInputHelper.PromptRequired("Category Name");
        var description = ConsoleInputHelper.PromptRequired("Description");

        var category = new { name, description };
        var payload = new { payload = category };
        await ApiClient.PostAsync(http, "/api/categories", payload);
    }

    private static async Task UpdateAsync(HttpClient http)
    {
        var qs = new QueryStringBuilder()
            .Add("page", "1")
            .Add("pageSize", "256")
            .Build();

        var response = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, "/api/categories?page=1&pageSize=32");
        if (response?.Data == null || response.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No categories available[/]");
            return;
        }

        var selected = await TableRenderer.SelectFromPromptAsync(
            async (pageNum) =>
            {
                var pageResponse = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, $"/api/categories?page={pageNum}&pageSize=32");
                return pageResponse?.Data ?? new List<CategoryDto>();
            },
            response.TotalCount,
            32,
            "Select a Category to Update",
            cat => cat.Name
        );

        if (selected == null)
            return;

        var categoryResponse = await ApiClient.FetchEntityAsync<CategoryDto>(http, $"/api/categories/{selected.CategoryId}");
        if (categoryResponse?.Data == null)
        {
            ConsoleInputHelper.DisplayError("Category not found");
            return;
        }

        var current = categoryResponse.Data;
        AnsiConsole.MarkupLine($"[yellow]Current values:[/]");
        AnsiConsole.MarkupLine($"  Name: {current.Name}");
        AnsiConsole.MarkupLine($"  Description: {current.Description}");

        var name = PromptOptionalField("Category Name", current.Name);
        var description = PromptOptionalField("Description", current.Description);

        var category = new
        {
            categoryId = selected.CategoryId,
            name,
            description,
        };
        var payload = new { payload = category };
        await ApiClient.PutAsync(http, $"/api/categories/{selected.CategoryId}", payload);
    }

    private static async Task DeleteAsync(HttpClient http)
    {
        var response = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, "/api/categories?page=1&pageSize=32");
        if (response?.Data == null || response.Data.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No categories available[/]");
            return;
        }

        var selected = await TableRenderer.SelectFromPromptAsync(
            async (pageNum) =>
            {
                var pageResponse = await ApiClient.FetchPaginatedAsync<CategoryDto>(http, $"/api/categories?page={pageNum}&pageSize=32");
                return pageResponse?.Data ?? new List<CategoryDto>();
            },
            response.TotalCount,
            32,
            "Select a Category to Delete",
            cat => cat.Name
        );

        if (selected == null)
            return;

        if (!AnsiConsole.Confirm($"[red]Are you sure you want to delete '{selected.Name}'?[/]"))
            return;

        await ApiClient.DeleteAsync(http, $"/api/categories/{selected.CategoryId}");
    }

    private static string? PromptOptionalField(string label, string? currentValue)
    {
        var input = AnsiConsole.Ask($"{label} (leave blank to keep):", string.Empty);
        return string.IsNullOrWhiteSpace(input) ? currentValue : input;
    }

    private static (string SortBy, string? SortDirection) PromptSortOptions(string[] options)
    {
        var sortBy = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Sort By (optional)").AddChoices(options)
        );

        string? sortDirection = null;
        if (sortBy != "(none)")
        {
            sortDirection = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title("Sort Direction").AddChoices("asc", "desc")
            );
        }

        return (sortBy, sortDirection);
    }

    private sealed record CategoryDto
    {
        public int CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
