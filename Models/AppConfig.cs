namespace IndustrialControlMAUI.Models;

public class ServerSettings
{
    public string IpAddress { get; set; }
}
public class ApiEndpoints
{
    public string Login { get; set; }
}
public class LoggingSettings
{
    public string Level { get; set; } = "Information";
}
public class AppConfig
{
    public string SchemaVersion { get; set; } = "0";
    public ServerSettings Server { get; set; } = new();
    public ApiEndpoints ApiEndpoints { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}
