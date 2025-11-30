using System.Net;
using System.Text;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;

namespace ECommerceApp.ConsoleClient;

public static class Program
{
    // Default ports based on Properties/launchSettings.json
    private const string DefaultBaseUrl = "http://localhost:51680";

    public static async Task<int> Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("ECommerce API").Color(Color.Green));

        var settings = new ClientSettings
        {
            BaseUrl = ResolveBaseUrl(args)
        };

        using var http = CreateHttpClient(settings.BaseUrl);

        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title($"[yellow]Base URL:[/] [blue]{settings.BaseUrl}[/]\nSelect an option:")
                .PageSize(10)
                .AddChoices(
                    "Products",
                    "Categories",
                    "Sales",
                    "Custom Request",
                    "Settings",
                    "Exit"));

            switch (choice)
            {
                case "Products":
                    await ProductsMenuAsync(http);
                    break;
                case "Categories":
                    await CategoriesMenuAsync(http);
                    break;
                case "Sales":
                    await SalesMenuAsync(http);
                    break;
                case "Custom Request":
                    await CustomRequestAsync(http);
                    break;
                case "Settings":
                    settings.BaseUrl = PromptBaseUrl(settings.BaseUrl);
                    http.BaseAddress = new Uri(settings.BaseUrl);
                    break;
                case "Exit":
                    return 0;
            }
        }
    }

    private static string ResolveBaseUrl(string[] args)
    {
        // Priority: CLI arg --base-url, env ECOMMERCE_BASE_URL, default
        var fromArg = GetArgValue(args, "--base-url");
        if (!string.IsNullOrWhiteSpace(fromArg))
            return fromArg!;

        var env = Environment.GetEnvironmentVariable("ECOMMERCE_BASE_URL");
        if (!string.IsNullOrWhiteSpace(env))
            return env!;

        return DefaultBaseUrl;
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 < args.Length) return args[i + 1];
            }
            else if (args[i].StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                return args[i][(key.Length + 1)..];
            }
        }
        return null;
    }

    private static HttpClient CreateHttpClient(string baseUrl)
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
        http.DefaultRequestHeaders.Accept.Clear();
        http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return http;
    }

    private static string PromptBaseUrl(string current)
    {
        var input = AnsiConsole.Prompt(new TextPrompt<string>("Enter API base URL:")
            .DefaultValue(current)
            .Validate(url => Uri.TryCreate(url, UriKind.Absolute, out _)
                ? ValidationResult.Success()
                : ValidationResult.Error("Invalid URL")));
        return input;
    }

    // PRODUCTS
    private static async Task ProductsMenuAsync(HttpClient http)
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[green]Products[/] — Choose an action:")
                .AddChoices("List", "Get by Id", "By Category", "Back"));
            switch (choice)
            {
                case "List":
                    await ListProductsAsync(http);
                    break;
                case "Get by Id":
                    await GetByIdAsync(http, "/api/product/{id}", "Product");
                    break;
                case "By Category":
                    await GetByIdAsync(http, "/api/product/category/{id}", "CategoryId");
                    break;
                case "Back":
                    return;
            }
        }
    }

    private static async Task ListProductsAsync(HttpClient http)
    {
        // Query params aligned with ProductQueryParametersValidator
        int page = AnsiConsole.Prompt(new TextPrompt<int>("Page").DefaultValue(1).Validate(i => i > 0
            ? ValidationResult.Success()
            : ValidationResult.Error("Page must be > 0")));
        int pageSize = AnsiConsole.Prompt(new TextPrompt<int>("Page Size (1-100)").DefaultValue(10)
            .Validate(i => i is >= 1 and <= 100 ? ValidationResult.Success() : ValidationResult.Error("1-100")));
        string? search = PromptOptional("Search");
        decimal? minPrice = PromptOptionalDecimal("Min Price");
        decimal? maxPrice = PromptOptionalDecimal("Max Price");
        int? categoryId = PromptOptionalInt("Category Id");

        var sortBy = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Sort By (optional)")
            .AddChoices("(none)", "name", "price", "stock", "createdat", "category"));
        string? sortDirection = null;
        if (sortBy != "(none)")
        {
            sortDirection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Sort Direction")
                .AddChoices("asc", "desc"));
        }

        var qs = new QueryStringBuilder()
            .Add("page", page)
            .Add("pageSize", pageSize)
            .Add("search", search)
            .Add("minPrice", minPrice)
            .Add("maxPrice", maxPrice)
            .Add("categoryId", categoryId)
            .Add("sortBy", sortBy == "(none)" ? null : sortBy)
            .Add("sortDirection", sortDirection)
            .Build();

        await GetAndRenderAsync(http, $"/api/product{qs}");
    }

    // CATEGORIES
    private static async Task CategoriesMenuAsync(HttpClient http)
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[green]Categories[/] — Choose an action:")
                .AddChoices("List", "Get by Id", "Get by Name", "Back"));
            switch (choice)
            {
                case "List":
                    await ListCategoriesAsync(http);
                    break;
                case "Get by Id":
                    await GetByIdAsync(http, "/api/categories/{id}", "Category");
                    break;
                case "Get by Name":
                    var name = AnsiConsole.Ask<string>("Name:");
                    await GetAndRenderAsync(http, $"/api/categories/name/{Uri.EscapeDataString(name)}");
                    break;
                case "Back":
                    return;
            }
        }
    }

    private static async Task ListCategoriesAsync(HttpClient http)
    {
        int page = AnsiConsole.Prompt(new TextPrompt<int>("Page").DefaultValue(1));
        int pageSize = AnsiConsole.Prompt(new TextPrompt<int>("Page Size (1-100)").DefaultValue(10));
        string? search = PromptOptional("Search");
        var sortBy = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Sort By (optional)")
            .AddChoices("(none)", "name", "createdat"));
        string? sortDirection = null;
        if (sortBy != "(none)")
        {
            sortDirection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Sort Direction")
                .AddChoices("asc", "desc"));
        }
        var qs = new QueryStringBuilder()
            .Add("page", page)
            .Add("pageSize", pageSize)
            .Add("search", search)
            .Add("sortBy", sortBy == "(none)" ? null : sortBy)
            .Add("sortDirection", sortDirection)
            .Build();

        await GetAndRenderAsync(http, $"/api/categories{qs}");
    }

    // SALES
    private static async Task SalesMenuAsync(HttpClient http)
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[green]Sales[/] — Choose an action:")
                .AddChoices("List", "Get by Id", "With Deleted Products (all)", "With Deleted Products (by Id)", "Back"));
            switch (choice)
            {
                case "List":
                    await ListSalesAsync(http);
                    break;
                case "Get by Id":
                    await GetByIdAsync(http, "/api/sales/{id}", "Sale");
                    break;
                case "With Deleted Products (all)":
                    await GetAndRenderAsync(http, "/api/sales/with-deleted-products");
                    break;
                case "With Deleted Products (by Id)":
                    await GetByIdAsync(http, "/api/sales/{id}/with-deleted-products", "Sale");
                    break;
                case "Back":
                    return;
            }
        }
    }

    private static async Task ListSalesAsync(HttpClient http)
    {
        int page = AnsiConsole.Prompt(new TextPrompt<int>("Page").DefaultValue(1));
        int pageSize = AnsiConsole.Prompt(new TextPrompt<int>("Page Size (1-100)").DefaultValue(10));
        DateTime? start = PromptOptionalDate("Start Date (yyyy-MM-dd)");
        DateTime? end = PromptOptionalDate("End Date (yyyy-MM-dd)");
        string? customerName = PromptOptional("Customer Name");
        string? customerEmail = PromptOptional("Customer Email");
        var sortBy = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Sort By (optional)")
            .AddChoices("(none)", "saledate", "totalamount", "customername"));
        string? sortDirection = null;
        if (sortBy != "(none)")
        {
            sortDirection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("Sort Direction")
                .AddChoices("asc", "desc"));
        }
        var qs = new QueryStringBuilder()
            .Add("page", page)
            .Add("pageSize", pageSize)
            .Add("startDate", start?.ToString("yyyy-MM-dd"))
            .Add("endDate", end?.ToString("yyyy-MM-dd"))
            .Add("customerName", customerName)
            .Add("customerEmail", customerEmail)
            .Add("sortBy", sortBy == "(none)" ? null : sortBy)
            .Add("sortDirection", sortDirection)
            .Build();

        await GetAndRenderAsync(http, $"/api/sales{qs}");
    }

    // Generic helpers
    private static async Task GetByIdAsync(HttpClient http, string template, string label)
    {
        int id = AnsiConsole.Prompt(new TextPrompt<int>($"{label} Id:").Validate(i => i > 0
            ? ValidationResult.Success()
            : ValidationResult.Error("Must be > 0")));
        var path = template.Replace("{id}", id.ToString());
        await GetAndRenderAsync(http, path);
    }

    private static async Task CustomRequestAsync(HttpClient http)
    {
        var method = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("HTTP Method")
            .AddChoices("GET", "POST", "PUT", "DELETE"));
        var path = AnsiConsole.Ask<string>("Path (e.g., /api/product):");
        string? body = null;
        if (method is "POST" or "PUT")
        {
            body = AnsiConsole.Ask<string>("JSON body (leave empty for none):", "");
        }

        try
        {
            using var req = new HttpRequestMessage(new HttpMethod(method), path);
            if (!string.IsNullOrWhiteSpace(body))
            {
                req.Content = new StringContent(body!, Encoding.UTF8, "application/json");
            }
            await SendAndRenderAsync(http, req);
        }
        catch (Exception ex)
        {
            Error(ex.Message);
        }
    }

    private static async Task GetAndRenderAsync(HttpClient http, string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        await SendAndRenderAsync(http, req);
    }

    private static async Task SendAndRenderAsync(HttpClient http, HttpRequestMessage req)
    {
        await AnsiConsole.Status().Spinner(Spinner.Known.Dots)
            .StartAsync("Calling API...", async _ =>
            {
                using var res = await http.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();

                var header = $"{(int)res.StatusCode} {res.ReasonPhrase}";
                var ct = res.Content.Headers.ContentType?.MediaType ?? "";

                AnsiConsole.Write(new Rule($"[bold]HTTP {req.Method}[/] {req.RequestUri}").LeftAligned());
                AnsiConsole.MarkupLine($"Status: [bold]{header}[/]  Content-Type: [italic]{ct}[/]");

                // Try to pretty print JSON; fallback to raw
                if (IsJson(ct, body))
                {
                    try
                    {
                        AnsiConsole.Write(new Panel(new JsonText(body)).Header("Response").Expand());
                        // Try to render summary tables for known shapes
                        await TryRenderKnownTablesAsync(body);
                    }
                    catch
                    {
                        AnsiConsole.Write(new Panel(Truncate(body, 8000)).Header("Response").Expand());
                    }
                }
                else
                {
                    AnsiConsole.Write(new Panel(Truncate(body, 8000)).Header("Response").Expand());
                }
            });
    }

    private static async Task TryRenderKnownTablesAsync(string json)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Try Product list shape
        try
        {
            var prodList = JsonSerializer.Deserialize<PaginatedResponse<List<ProductDto>>>(json, options);
            if (prodList?.Data is { Count: > 0 })
            {
                var table = new Table().Title($"Products (Page {prodList.CurrentPage}/{prodList.TotalPages})");
                table.AddColumn("Id");
                table.AddColumn("Name");
                table.AddColumn("Price");
                table.AddColumn("Stock");
                table.AddColumn("Active");
                table.AddColumn("CategoryId");
                foreach (var p in prodList.Data)
                {
                    table.AddRow(p.ProductId.ToString(), p.Name, p.Price.ToString("0.00"), p.Stock.ToString(), p.IsActive ? "Yes" : "No", p.CategoryId.ToString());
                }
                AnsiConsole.Write(table);
                return;
            }
        }
        catch { /* ignore */ }

        // Try Category list shape
        try
        {
            var catList = JsonSerializer.Deserialize<PaginatedResponse<List<CategoryDto>>>(json, options);
            if (catList?.Data is { Count: > 0 })
            {
                var table = new Table().Title($"Categories (Page {catList.CurrentPage}/{catList.TotalPages})");
                table.AddColumn("Id");
                table.AddColumn("Name");
                table.AddColumn("Description");
                foreach (var c in catList.Data)
                {
                    table.AddRow(c.CategoryId.ToString(), c.Name, c.Description ?? string.Empty);
                }
                AnsiConsole.Write(table);
                return;
            }
        }
        catch { /* ignore */ }

        // Try Sales list shape
        try
        {
            var saleList = JsonSerializer.Deserialize<PaginatedResponse<List<SaleDto>>>(json, options);
            if (saleList?.Data is { Count: > 0 })
            {
                var table = new Table().Title($"Sales (Page {saleList.CurrentPage}/{saleList.TotalPages})");
                table.AddColumn("Id");
                table.AddColumn("Date");
                table.AddColumn("Customer");
                table.AddColumn("Email");
                table.AddColumn("Total");
                foreach (var s in saleList.Data)
                {
                    table.AddRow(s.SaleId.ToString(), s.SaleDate.ToString("yyyy-MM-dd"), s.CustomerName ?? string.Empty, s.CustomerEmail ?? string.Empty, s.TotalAmount.ToString("0.00"));
                }
                AnsiConsole.Write(table);
            }
        }
        catch { /* ignore */ }
        await Task.CompletedTask;
    }

    private static string? PromptOptional(string label)
    {
        var v = AnsiConsole.Ask<string>($"{label} (optional):", string.Empty);
        return string.IsNullOrWhiteSpace(v) ? null : v;
    }
    private static int? PromptOptionalInt(string label)
    {
        var v = AnsiConsole.Ask<string>($"{label} (optional):", string.Empty);
        return int.TryParse(v, out var i) ? i : null;
    }
    private static decimal? PromptOptionalDecimal(string label)
    {
        var v = AnsiConsole.Ask<string>($"{label} (optional):", string.Empty);
        return decimal.TryParse(v, out var d) ? d : null;
    }
    private static DateTime? PromptOptionalDate(string label)
    {
        var v = AnsiConsole.Ask<string>($"{label}", string.Empty);
        return DateTime.TryParse(v, out var dt) ? dt : null;
    }

    private static bool IsJson(string contentType, string body)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.IsNullOrWhiteSpace(body)) return false;
        var t = body.TrimStart();
        return t.StartsWith("{") || t.StartsWith("[");
    }

    private static string Truncate(string s, int max)
    {
        return s.Length > max ? s[..max] + "..." : s;
    }

    private static void Error(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
    }

    // Minimal DTOs for rendering
    private sealed record ClientSettings
    {
        public string BaseUrl { get; set; } = DefaultBaseUrl;
    }

    private sealed record PaginatedResponse<T>
    {
        public bool RequestFailed { get; init; }
        public HttpStatusCode ResponseCode { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public T? Data { get; init; }
        public int CurrentPage { get; init; }
        public int PageSize { get; init; }
        public int TotalCount { get; init; }
        public int TotalPages { get; init; }
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

    private sealed record CategoryDto
    {
        public int CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }

    private sealed record SaleDto
    {
        public int SaleId { get; init; }
        public DateTime SaleDate { get; init; }
        public string? CustomerName { get; init; }
        public string? CustomerEmail { get; init; }
        public decimal TotalAmount { get; init; }
    }

    private sealed class QueryStringBuilder
    {
        private readonly List<string> _parts = new();
        public QueryStringBuilder Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                _parts.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value));
            return this;
        }
        public QueryStringBuilder Add<T>(string key, T? value) where T : struct
        {
            if (value.HasValue)
                _parts.Add(Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value.Value.ToString()!));
            return this;
        }
        public string Build()
        {
            return _parts.Count == 0 ? string.Empty : "?" + string.Join("&", _parts);
        }
    }
}
