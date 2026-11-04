using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IngresDates.EntityFrameworkCore;
public sealed record IngresDateSettings(string TimeZoneId);
/// <summary>Immutable, per-service-provider options rather than mutable process-wide settings.</summary>
public sealed class IngresDateOptionsExtension(string timeZoneId) : IDbContextOptionsExtension
{
    public IngresDateSettings Settings { get; } = new(timeZoneId);
    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public void ApplyServices(IServiceCollection services) => services.AddSingleton(Settings);
    public void Validate(IDbContextOptions options) => _ = TimeZoneInfo.FindSystemTimeZoneById(Settings.TimeZoneId);
    private sealed class ExtensionInfo(IngresDateOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;
        public override string LogFragment => "IngresDates ";

        public override int GetServiceProviderHashCode() => extension.Settings.TimeZoneId.GetHashCode();
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo info && info.extensionTimeZone == extension.Settings.TimeZoneId;
        private string extensionTimeZone => extension.Settings.TimeZoneId;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo) => debugInfo["IngresDates:TimeZone"] = extension.Settings.TimeZoneId;
    }
}
