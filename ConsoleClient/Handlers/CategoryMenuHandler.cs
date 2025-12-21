using System.Text.Json;
using ECommerceApp.ConsoleClient.Helpers;
using ECommerceApp.ConsoleClient.Interfaces;
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
            { "Get by Id", h => ApiClient.GetByIdAsync(h, "/api/categories/{id}", "Category") },
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
        var (page, pageSize) = ConsoleInputHelper.PromptPagination();
        string? search = ConsoleInputHelper.PromptOptional("Search");

        var (sortBy, sortDirection) = PromptSortOptions(new[] { "(none)", "name", "createdat" });

        var qs = new QueryStringBuilder()
            .Add("page", page.ToString())
            .Add("pageSize", pageSize.ToString())
            .Add("search", search)
            .Add("sortBy", sortBy == "(none)" ? null : sortBy)
            .Add("sortDirection", sortDirection)
            .Build();

        await ApiClient.GetAndRenderAsync(http, $"/api/categories{qs}");
    }

    private static async Task GetByNameAsync(HttpClient http)
    {
        var name = ConsoleInputHelper.PromptRequired("Name");
        await ApiClient.GetAndRenderAsync(
            http,
            $"/api/categories/name/{Uri.EscapeDataString(name)}"
        );
    }

    private static async Task CreateAsync(HttpClient http)
    {
        var name = ConsoleInputHelper.PromptRequired("Category Name");
        var description = ConsoleInputHelper.PromptRequired("Description");

        var payload = new { name, description };
        await ApiClient.PostAsync(http, "/api/categories", payload);
    }

    private static async Task UpdateAsync(HttpClient http)
    {
        int categoryId = ConsoleInputHelper.PromptPositiveInt("Category ID");

        var categoryResponse = await ApiClient.FetchEntityAsync<CategoryDto>(
            http,
            $"/api/categories/{categoryId}"
        );
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

        var payload = new
        {
            categoryId,
            name,
            description,
        };
        await ApiClient.PutAsync(http, $"/api/categories/{categoryId}", payload);
    }

    private static async Task DeleteAsync(HttpClient http)
    {
        int categoryId = ConsoleInputHelper.PromptPositiveInt("Category ID");

        if (!AnsiConsole.Confirm($"Delete category {categoryId}?"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
            return;
        }

        await ApiClient.DeleteAsync(http, $"/api/categories/{categoryId}");
    }

    private static string? PromptOptionalField(string label, string? currentValue)
    {
        var input = AnsiConsole.Ask<string>($"{label} (leave blank to keep):", string.Empty);
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
