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
    public static T? SelectFromTable<T>(IList<T> items, string title, params string[] excludeColumns) where T : class
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items to display[/]");
            return null;
        }

        var table = new Table { Title = title };
        table.AddColumn("[bold]#[/]");

        var properties = GetDisplayProperties<T>(excludeColumns);
        foreach (var prop in properties)
        {
            table.AddColumn($"[bold]{prop.Name}[/]");
        }

        for (int i = 0; i < items.Count; i++)
        {
            var cells = new List<string> { (i + 1).ToString() };
            foreach (var prop in properties)
            {
                var value = prop.GetValue(items[i]);
                cells.Add(FormatValue(value));
            }
            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);

        // Prompt for selection
        var choices = Enumerable.Range(1, items.Count)
            .Select(i => i.ToString())
            .Concat(new[] { "Cancel" })
            .ToList();

        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Select an item:")
            .AddChoices(choices));

        if (choice == "Cancel" || !int.TryParse(choice, out int selectedIndex))
            return null;

        return items[selectedIndex - 1];
    }

    /// <summary>
    /// Displays a collection of items as a formatted table without selection.
    /// </summary>
    public static void DisplayTable<T>(IList<T> items, string title, params string[] excludeColumns) where T : class
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No items to display[/]");
            return;
        }

        var table = new Table { Title = title };
        table.AddColumn("[bold]#[/]");

        var properties = GetDisplayProperties<T>(excludeColumns);
        foreach (var prop in properties)
        {
            table.AddColumn($"[bold]{prop.Name}[/]");
        }

        for (int i = 0; i < items.Count; i++)
        {
            var cells = new List<string> { (i + 1).ToString() };
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
    /// </summary>
    private static List<PropertyInfo> GetDisplayProperties<T>(string[] excludeColumns) where T : class
    {
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && !excludeColumns.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) ||
                        p.PropertyType == typeof(decimal) || p.PropertyType == typeof(DateTime) ||
                        p.PropertyType == typeof(DateTime?))
            .OrderBy(p => p.Name)
            .ToList();

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
