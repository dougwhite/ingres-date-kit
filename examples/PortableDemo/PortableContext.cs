using IngresDates;
using IngresDates.Portable;
using Microsoft.EntityFrameworkCore;

namespace PortableDemo;
public sealed class PortableRow
{
    public int Id { get; set; }
    public IngresDate Value { get; set; }
    public IngresDate? NullableValue { get; set; }
}

public sealed class PortableContext(DbContextOptions<PortableContext> options) : DbContext(options)
{
    private bool IsIngres => Database.ProviderName?.Contains("Actian", StringComparison.OrdinalIgnoreCase) == true;
    public DbSet<PortableRow> Rows => Set<PortableRow>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        if (!IsIngres)
            builder.UsePortableIngresDates();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<PortableRow>().ToTable("multi_database_samples", IsIngres ? "ingres" : null);
        builder.Entity<PortableRow>().Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Entity<PortableRow>().Property(x => x.Value).HasColumnName("value");
        builder.Entity<PortableRow>().Property(x => x.NullableValue).HasColumnName("nullable_value");
    }
}
