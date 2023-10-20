using Microsoft.Extensions.Configuration;

namespace Demo.Lambda;

public interface IConfigurationService
{
    IConfiguration GetConfiguration();
}

public class ConfigurationService : IConfigurationService
{
    public IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local"}.json", optional: true)
            .Build();
    }
}