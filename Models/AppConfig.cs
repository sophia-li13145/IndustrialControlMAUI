namespace IndustrialControlMAUI.Models;

public class ServerSettings
{
    public string IpAddress { get; set; }
    public int Port { get; set; } = 8080;
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
    public int SchemaVersion { get; set; } = 3;
    public ServerSettings Server { get; set; } = new();
    public ApiEndpoints ApiEndpoints { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}
