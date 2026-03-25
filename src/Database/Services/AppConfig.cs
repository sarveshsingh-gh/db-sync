using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Database.Services;

public class AppConfig
{
    public string SourceDb       { get; set; } = string.Empty;
    public string DestinationDb  { get; set; } = string.Empty;
    public string BaselinePath   { get; set; } = "db/baseline.dacpac";
    public string SchemaPath     { get; set; } = "db/schema";
    public string MigrationsPath { get; set; } = "db/migrations";

    public static AppConfig Load()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddUserSecrets<AppConfig>()
            .AddEnvironmentVariables()
            .Build();

        return config.Get<AppConfig>()
            ?? throw new InvalidOperationException("Failed to bind appsettings.json to AppConfig.");
    }

    public string MaskConn(string connectionString)
    {
        try
        {
            var b = new SqlConnectionStringBuilder(connectionString);
            return $"{b.DataSource} / {b.InitialCatalog}";
        }
        catch { return "***"; }
    }
}
