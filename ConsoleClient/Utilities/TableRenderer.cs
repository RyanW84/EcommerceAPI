using System.Reflection;
using Spectre.Console;

namespace ECommerceApp.ConsoleClient.Utilities;

/// <summary>
/// Utility for rendering data collections as formatted tables in the console.
/// Abstracts away object IDs by using 1-based index numbers.
/// </summary>
public static class TableRenderer
{
    /// <summary>
    /// Renders a collection of items as a table and returns the selected item.
    /// Uses 1-based index numbers instead of IDs for user-friendly selection.
    /// </summary>
    /// <typeparam name="T">The type of items to display</typeparam>
    /// <param name="items">The items to display in the table</param>
    /// <param name="title">The table title</param>
    /// <param name="excludeColumns">Column names to exclude from display</param>
    /// <returns>The selected item, or null if cancelled</returns>
    public static T? SelectFromTable<T>(
        IList<T> items,
        string title,
        int indexOffset = 0,
        params string[] excludeColumns
    )
        where T : class
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items to display[/]");
            return null;
        }

        var table = new Table { Title = new TableTitle(title) };
        table.AddColumn("[bold]#[/]");

        var properties = GetDisplayProperties<T>(excludeColumns);
        foreach (var prop in properties)
        {
            table.AddColumn($"[bold]{prop.Name}[/]");
        }

        for (int i = 0; i < items.Count; i++)
        {
            var cells = new List<string> { (indexOffset + i + 1).ToString() };
            foreach (var prop in properties)
            {
                var value = prop.GetValue(items[i]);
                cells.Add(FormatValue(value));
            }
            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);

        // Prompt for selection
        var choices = Enumerable
            .Range(indexOffset + 1, items.Count)
            .Select(i => i.ToString())
            .Concat(new[] { "Cancel" })
            .ToList();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>().Title("Select an item:").AddChoices(choices)
        );

        if (choice == "Cancel" || !int.TryParse(choice, out int selectedIndex))
            return null;

        return items[selectedIndex - indexOffset - 1];
    }

    /// <summary>
    /// Creates a selection prompt that shows index and item name together.
    /// More user-friendly than selecting by number alone.
    /// Supports pagination for lists with more than 32 items.
    /// </summary>
    public static T? SelectFromPrompt<T>(
        IList<T> items,
        string title,
        int indexOffset = 0,
        string nameProperty = "Name"
    )
        where T : class
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items to display[/]");
            return null;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var nameProp = properties.FirstOrDefault(p =>
            p.Name.Equals(nameProperty, StringComparison.OrdinalIgnoreCase));

        const int pageSize = 32;
        int currentPage = 0;
        int totalPages = (items.Count + pageSize - 1) / pageSize;

        while (true)
        {
            // Calculate range for current page
            int startIndex = currentPage * pageSize;
            int endIndex = Math.Min(startIndex + pageSize, items.Count);
            int itemsOnThisPage = endIndex - startIndex;

            var choices = new List<string>();

            // Add items for this page
            for (int i = startIndex; i < endIndex; i++)
            {
                var index = indexOffset + i + 1;
                var nameValue = nameProp?.GetValue(items[i])?.ToString() ?? "Unknown";
                choices.Add($"{index} - {nameValue}");
            }

            // Add navigation options if multiple pages
            if (totalPages > 1)
            {
                choices.Add("---");
                if (currentPage > 0)
                    choices.Add("< Previous Page");
                if (currentPage < totalPages - 1)
                    choices.Add("Next Page >");
                choices.Add("Cancel");
            }
            else
            {
                choices.Add("Cancel");
            }

            string pageIndicator = totalPages > 1 ? $" (Page {currentPage + 1}/{totalPages})" : "";
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[green]{title}{pageIndicator}[/]")
                    .PageSize(Math.Min(40, choices.Count))
                    .MoreChoicesText("[grey](Use arrow keys to navigate)[/]")
                    .AddChoices(choices)
            );

            if (choice == "Cancel" || choice == "---")
                return null;

            if (choice == "< Previous Page")
            {
                currentPage--;
                continue;
            }

            if (choice == "Next Page >")
            {
                currentPage++;
                continue;
            }

            // Extract the index from the choice string (e.g., "1 - Product Name" -> 1)
            if (int.TryParse(choice.Split('-')[0].Trim(), out int selectedIndex))
            {
                return items[selectedIndex - indexOffset - 1];
            }

            return null;
        }
    }

    /// <summary>
    /// Displays a collection of items as a formatted table without selection.
    /// </summary>
    public static void DisplayTable<T>(IList<T> items, string title, int indexOffset = 0, params string[] excludeColumns)
        where T : class
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items to display[/]");
            return;
        }

        var table = new Table { Title = new TableTitle(title) };
        table.AddColumn("[bold]#[/]");

        var properties = GetDisplayProperties<T>(excludeColumns);
        foreach (var prop in properties)
        {
            table.AddColumn($"[bold]{prop.Name}[/]");
        }

        for (int i = 0; i < items.Count; i++)
        {
            var cells = new List<string> { (indexOffset + i + 1).ToString() };
            foreach (var prop in properties)
            {
                var value = prop.GetValue(items[i]);
                cells.Add(FormatValue(value));
            }
            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Gets properties suitable for display, excluding specified columns and internal fields.
    /// Automatically excludes ID columns (anything ending with "Id").
    /// Returns properties in the desired display order: Name, Description, Price, Stock, IsActive.
    /// </summary>
    private static List<PropertyInfo> GetDisplayProperties<T>(string[] excludeColumns)
        where T : class
    {
        var desiredOrder = new[] { "Name", "Description", "Price", "Stock", "IsActive", "CustomerName", "SaleDate", "TotalAmount", "CustomerEmail", "CustomerAddress" };

        var allProps = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p =>
                p.CanRead
                && !excludeColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase)
                && !p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)
            )
            .Where(p =>
                p.PropertyType.IsPrimitive
                || p.PropertyType == typeof(string)
                || p.PropertyType == typeof(decimal)
                || p.PropertyType == typeof(DateTime)
                || p.PropertyType == typeof(DateTime?)
            )
            .ToList();

        // Sort by desired order, then add any remaining properties alphabetically
        var props = new List<PropertyInfo>();
        foreach (var propName in desiredOrder)
        {
            var prop = allProps.FirstOrDefault(p => p.Name.Equals(propName, StringComparison.OrdinalIgnoreCase));
            if (prop != null)
            {
                props.Add(prop);
                allProps.Remove(prop);
            }
        }

        // Add any remaining properties in alphabetical order
        props.AddRange(allProps.OrderBy(p => p.Name));

        return props;
    }

    /// <summary>
    /// Formats a value for table display.
    /// </summary>
    private static string FormatValue(object? value)
    {
        if (value == null)
            return "[dim]—[/]";

        if (value is bool boolVal)
            return boolVal ? "[green]Yes[/]" : "[red]No[/]";

        if (value is DateTime dateVal)
            return dateVal.ToString("yyyy-MM-dd HH:mm");

        if (value is decimal decimalVal)
            return decimalVal.ToString("N2");

        var str = value.ToString() ?? "";
        return str.Length > 50 ? str[..47] + "..." : str;
    }
}
