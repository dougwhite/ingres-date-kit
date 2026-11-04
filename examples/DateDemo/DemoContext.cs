using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Ingres.Client;
using IngresDates;
using IngresDates.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DateDemo;
public sealed class SampleRow
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public IngresDate Native { get; set; }
    public IngresDate? NullableNative { get; set; }
    public IngresInterval LeadTime { get; set; }

    [JsonIgnore]
    public IngresDate RequiredDateValue { get; set; }

    // Existing UI/application code can use DateTime?. Only this small facade
    // knows how to map an application null back to a non-null Ingres blank.
    [NotMapped, IngresOptionalDate(nameof(RequiredDateValue))]
    public DateTime? RequiredDate { get => RequiredDateValue.ToOptionalDateTime(); set => RequiredDateValue = IngresDate.FromOptionalDateTime(value); }

    // An alternative view of the same column for an application that deliberately
    // reserves 9999-12-31 as its blank marker. This loses the distinction between
    // a real date with that value and a blank, so make the choice per column.
    [NotMapped, JsonIgnore, IngresDateSentinel(nameof(RequiredDateValue), 9999, 12, 31)]
    public DateTime RequiredDateOrSentinel { get => DateProfiles.ReadSentinel(RequiredDateValue, new DateTime(9999, 12, 31)); set => RequiredDateValue = DateProfiles.WriteSentinel(value, new DateTime(9999, 12, 31)); }
}

public sealed class DemoContext(DbContextOptions<DemoContext> options) : DbContext(options)
{
    public DbSet<SampleRow> Samples => Set<SampleRow>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        var row = model.Entity<SampleRow>();
        row.ToTable("date_samples", "ingres");
        row.HasKey(x => x.Id);
        row.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        row.Property(x => x.Label).HasColumnName("label").HasMaxLength(80).IsUnicode(false);
        row.Property(x => x.Native).HasColumnName("native_value"); // automatic ingresdate mapping
        row.Property(x => x.NullableNative).HasColumnName("nullable_value");
        row.Property(x => x.LeadTime).HasColumnName("lead_time");
        row.Property(x => x.RequiredDateValue).HasColumnName("required_date").HasIngresDateProfile(DateProfile.DateTimeOnly).HasDefaultValueSql("''");
    }

    public static string ConnectionString(string connectionString)
    {
        var builder = new IngresConnectionStringBuilder(connectionString);
        // Explicit server formatting is part of the transport contract.
        builder.DateFormat = "SWEDEN";
        builder.Timezone = "Australia-Melbourne";
        return builder.ConnectionString;
    }

    public static DemoContext Open(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DemoContext>();
        options.UseActian(ConnectionString(connectionString));
        options.UseIngresDates("Australia/Melbourne");
        return new(options.Options);
    }
}
