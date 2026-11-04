using IngresDates.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace DateDemo;
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<DemoContext>
{
    public DemoContext CreateDbContext(string[] args) => DemoContext.Open(Environment.GetEnvironmentVariable("INGRESDATE_CONNECTION") ?? "Host=localhost;Port=II7;Database=ingresdate_demo");
}

/// <summary>Reverse engineering must use the same rich type mapping as runtime.</summary>
public sealed class IngresDateDesignTimeServices : IDesignTimeServices
{
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        services.AddSingleton(new IngresDateSettings("Australia/Melbourne"));
        services.AddSingleton<IRelationalTypeMappingSource, IngresDateTypeMappingSource>();
    }
}
