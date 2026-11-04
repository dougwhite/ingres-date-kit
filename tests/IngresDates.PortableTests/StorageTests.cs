using IngresDates;
using Microsoft.EntityFrameworkCore;
using PortableDemo;
using Xunit;

namespace IngresDates.PortableTests;
public sealed class StorageTests
{
    [Theory]
    [InlineData("")]
    [InlineData("2026-01-31")]
    [InlineData("2026-01-31 00:00:00")]
    [InlineData("2026-01-31 12:34:56.123")]
    [InlineData("5 days")]
    [InlineData("1 year 2 months -3 days 4 hours")]
    public void Portable_storage_round_trip_preserves_all_value_kinds(string text)
    {
        var options = new DbContextOptionsBuilder<PortableContext>().UseSqlite("Data Source=:memory:").Options;
        using var db = new PortableContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        var original = IngresDate.Parse(text, "Australia/Melbourne");
        db.Rows.Add(new PortableRow { Id = 1, Value = original, NullableValue = null });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var row = db.Rows.Single();
        Assert.True(original.HasSameRepresentation(row.Value));
        Assert.Null(row.NullableValue);
        Assert.Equal(original.TimeZoneId, row.Value.TimeZoneId);
    }

    [Fact]
    public void A_blank_stays_different_from_SQL_null_in_portable_storage()
    {
        using var db = new PortableContext(new DbContextOptionsBuilder<PortableContext>().UseSqlite("Data Source=:memory:").Options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        db.Rows.Add(new PortableRow { Id = 1, Value = IngresDate.Empty, NullableValue = IngresDate.Empty });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.True(db.Rows.Single().NullableValue!.Value.IsEmpty);
    }

    [Fact]
    public void Changing_only_the_timezone_metadata_is_saved_in_portable_storage()
    {
        using var db = new PortableContext(new DbContextOptionsBuilder<PortableContext>()
            .UseSqlite("Data Source=:memory:").Options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        var row = new PortableRow
        {
            Id = 1,
            Value = IngresDate.Parse("2026-01-31 12:00:00", "Australia/Melbourne")
        };
        db.Rows.Add(row);
        db.SaveChanges();
        row.Value = IngresDate.Parse("2026-01-31 12:00:00", "UTC");
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Equal("UTC", db.Rows.Single().Value.TimeZoneId);
    }
}
