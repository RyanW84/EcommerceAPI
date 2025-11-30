using ECommerceApp.ConsoleClient;
using ECommerceApp.ConsoleClient.Options;
using ECommerceApp.ConsoleClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

try
{
    IConfigurationRoot configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

    ApiClientOptions options = configuration.GetSection("ApiClient").Get<ApiClientOptions>() ?? new();

    // Validate configuration
    if (string.IsNullOrWhiteSpace(options.BaseAddress))
    {
        throw new InvalidOperationException("BaseAddress is required in ApiClient configuration.");
    }

    if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out _))
    {
        throw new InvalidOperationException($"BaseAddress '{options.BaseAddress}' is not a valid absolute URI.");
    }

    var services = new ServiceCollection();

    // Add logging
    services.AddLogging(builder =>
    {
        builder.AddConsole();
        builder.SetMinimumLevel(LogLevel.Information);
    });

    services.AddSingleton(options);

    // Configure HttpClient with proper SSL handling
    services.AddHttpClient<ECommerceApiClient>((client) =>
        {
            client.BaseAddress = new Uri(options.BaseAddress);
            client.Timeout = options.Timeout;
            client.DefaultRequestHeaders.Add("User-Agent", "ECommerceConsoleClient/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            // Handle SSL certificate errors in development
            ServerCertificateCustomValidationCallback = options.IgnoreCertificateErrors
                ? (message, cert, chain, errors) => true
                : null
        });

    services.AddTransient<ConsoleApp>();

    using ServiceProvider serviceProvider = services.BuildServiceProvider();

    // Enable logging
    ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting ECommerce Console Client...");

    ConsoleApp consoleApp = serviceProvider.GetRequiredService<ConsoleApp>();
    await consoleApp.RunAsync();

    logger.LogInformation("ECommerce Console Client completed successfully.");
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"Fatal error: {ex.Message}");

    // Exit with error code for console applications
    Environment.Exit(1);
}
