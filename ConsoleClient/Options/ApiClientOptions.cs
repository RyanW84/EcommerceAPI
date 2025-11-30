namespace ECommerceApp.ConsoleClient.Options;

public class ApiClientOptions
{
    public string BaseAddress { get; set; } = "https://localhost:5001/";
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool IgnoreCertificateErrors { get; set; } = true;
}
