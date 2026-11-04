using DateDemo;
using IngresDates;
using IngresDates.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace IngresDates.IngresTests;
public sealed class EfTests
{
    [Fact]
    public void Native_values_and_SQL_null_materialize_without_losing_intervals()
    {
        using var db = Database.Open();
        var rows = db.Samples.OrderBy(x => x.Id).ToList();
        Assert.Equal(8, rows.Count);
        Assert.True(rows[0].Native.IsEmpty);
        Assert.Null(rows[0].NullableNative);
        Assert.True(rows[1].NullableNative!.Value.IsEmpty);
        Assert.Equal(IngresDateKind.Date, rows[1].Native.Kind);
        Assert.Equal(IngresDateKind.Timestamp, rows[7].Native.Kind);
        Assert.Equal(IngresDate.Parse("5 days"), rows[3].Native);
        Assert.Equal(IngresDate.Parse("2026-01-31 12:34:56"), rows[2].Native);
    }

    [Fact]
    public void Ordinary_nullable_facade_query_finds_native_blanks()
    {
        using var db = Database.Open();
        Assert.Equal(new[] { 1, 4, 5, 6, 7 }, db.Samples.Where(x => x.RequiredDate == null).OrderBy(x => x.Id).Select(x => x.Id).ToArray());
        Assert.Equal(new[] { 2, 3, 8 }, db.Samples.Where(x => x.RequiredDate != null).OrderBy(x => x.Id).Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Native_blank_and_SQL_null_queries_stay_distinct()
    {
        using var db = Database.Open();
        Assert.Equal(new[] { 2 }, db.Samples.Where(x => x.NullableNative == IngresDate.Empty).Select(x => x.Id).ToArray());
        Assert.Equal(new[] { 1, 4, 5, 6, 7, 8 }, db.Samples.Where(x => x.NullableNative == null).OrderBy(x => x.Id).Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Native_member_queries_execute_on_the_server()
    {
        using var db = Database.Open();
        Assert.Equal(new[] { 4, 5, 6, 7 }, db.Samples.Where(x => x.Native.IsInterval).OrderBy(x => x.Id).Select(x => x.Id).ToArray());
        Assert.Equal(new[] { 2, 3, 8 }, db.Samples.Where(x => x.Native.IsAbsolute).OrderBy(x => x.Id).Select(x => x.Id).ToArray());
        Assert.Single(db.Samples.Where(x => x.Native.IsEmpty));
    }

    [Fact]
    public void Captured_parameters_and_native_arithmetic_translate()
    {
        using var db = Database.Open();
        IngresDate days = IngresInterval.Days(5);
        var query = db.Samples.Where(x => x.Id == 2).Select(x => x.Native + days);
        Assert.Equal(IngresDate.Parse("2026-02-05"), query.Single());
        Assert.Contains("ingresdate(", query.ToQueryString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Nested_queries_keep_inner_values_native()
    {
        using var db = Database.Open();
        var value = db.Samples.OrderBy(x => x.Id).Take(3).Where(x => x.Native == IngresDate.Empty).Select(x => x.Native).ToList();
        Assert.Single(value);
        Assert.True(value[0].IsEmpty);
    }

    [Fact]
    public void Nullable_facade_can_be_projected_as_an_ordinary_DateTime()
    {
        using var db = Database.Open();
        var value = db.Samples.Where(x => x.Id == 3).Select(x => x.RequiredDate).Single();
        Assert.Equal(new DateTime(2026, 1, 31, 12, 34, 56), value);
    }

    [Fact]
    public void Clearing_the_facade_writes_blank_into_the_NOT_NULL_column()
    {
        using var db = Database.Open();
        using var transaction = db.Database.BeginTransaction();
        var row = db.Samples.Single(x => x.Id == 3);
        row.RequiredDate = null;
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var restored = db.Samples.Single(x => x.Id == 3);
        Assert.True(restored.RequiredDateValue.IsEmpty);
        Assert.Null(restored.RequiredDate);
        Assert.False(db.Model.FindEntityType(typeof(SampleRow))!.FindProperty(nameof(SampleRow.RequiredDateValue))!.IsNullable);
        transaction.Rollback();
    }

    [Fact]
    public void Inserts_and_updates_preserve_intervals_and_true_SQL_null()
    {
        using var db = Database.Open();
        using var transaction = db.Database.BeginTransaction();
        db.Samples.Add(new SampleRow { Id = 9001, Label = "transactional round trip", Native = IngresDate.Parse("1 month"), NullableNative = null, LeadTime = IngresInterval.Days(5), RequiredDate = null });
        db.SaveChanges();
        db.ChangeTracker.Clear();
        var row = db.Samples.Single(x => x.Id == 9001);
        Assert.Equal(IngresDate.Parse("1 month"), row.Native);
        Assert.Null(row.NullableNative);
        Assert.Null(row.RequiredDate);
        row.Native = IngresDate.Parse("-5 days");
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Equal(IngresDate.Parse("-5 days"), db.Samples.Single(x => x.Id == 9001).Native);
        transaction.Rollback();
    }

    [Fact]
    public void An_unrelated_update_does_not_rewrite_SQL_null_to_blank()
    {
        using var db = Database.Open();
        using var transaction = db.Database.BeginTransaction();
        var row = db.Samples.Single(x => x.Id == 1);
        row.Label = "temporary";
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.Null(db.Samples.Single(x => x.Id == 1).NullableNative);
        transaction.Rollback();
    }

    [Fact]
    public void Invalid_profile_values_fail_before_writing()
    {
        using var db = Database.Open();
        var row = db.Samples.Single(x => x.Id == 1);
        row.RequiredDateValue = IngresDate.Parse("5 days");
        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public void The_type_mapping_selects_ingresdate_without_a_per_property_conversion()
    {
        using var db = Database.Open();
        var property = db.Model.FindEntityType(typeof(SampleRow))!.FindProperty(nameof(SampleRow.Native))!;
        Assert.Equal("ingresdate", property.GetRelationalTypeMapping().StoreType);
        Assert.Contains("ingresdate", db.Database.GenerateCreateScript(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sentinel_facade_translates_queries_and_writes_blank()
    {
        using var db = Database.Open();
        using var transaction = db.Database.BeginTransaction();
        var sentinel = new DateTime(9999, 12, 31);
        Assert.Equal(new[] { 1, 4, 5, 6, 7 }, db.Samples.Where(x => x.RequiredDateOrSentinel == sentinel).OrderBy(x => x.Id).Select(x => x.Id).ToArray());
        Assert.Equal(sentinel, db.Samples.Where(x => x.Id == 1).Select(x => x.RequiredDateOrSentinel).Single());
        var row = db.Samples.Single(x => x.Id == 3);
        row.RequiredDateOrSentinel = sentinel;
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.True(db.Samples.Single(x => x.Id == 3).RequiredDateValue.IsEmpty);
        transaction.Rollback();
    }

    [Fact]
    public void Profile_validation_also_applies_to_scalar_projection_reads()
    {
        using var db = Database.Open();
        using var transaction = db.Database.BeginTransaction();
        db.Database.ExecuteSqlRaw("update ingres.date_samples set required_date = ingresdate('5 days') where id = 1");
        Assert.Throws<InvalidOperationException>(() => db.Samples.Where(x => x.Id == 1).Select(x => x.RequiredDateValue).Single());
        transaction.Rollback();
    }

    [Fact]
    public void Interval_only_properties_support_native_LINQ_arithmetic()
    {
        using var db = Database.Open();
        var extra = IngresInterval.Days(2);
        var result = db.Samples.Where(x => x.Id == 1).Select(x => x.LeadTime + extra).Single();
        Assert.Equal(IngresInterval.Days(7), result);
    }

    [Fact]
    public void Equivalent_interval_representation_changes_are_tracked()
    {
        using var db = Database.Open();
        using var transaction = db.Database.BeginTransaction();
        var row = db.Samples.Single(x => x.Id == 1);
        row.LeadTime = IngresInterval.CalendarMonths(1);
        db.SaveChanges();
        row.LeadTime = IngresInterval.Days(30);
        db.ChangeTracker.DetectChanges();
        Assert.True(db.Entry(row).Property(x => x.LeadTime).IsModified);
        db.SaveChanges();
        db.ChangeTracker.Clear();
        Assert.True(db.Samples.Single(x => x.Id == 1).LeadTime.HasSameRepresentation(IngresInterval.Days(30)));
        transaction.Rollback();
    }

    [Fact]
    public void Migration_script_uses_native_types_and_implicit_transactions()
    {
        using var db = Database.Open();
        var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        string script = migrator.GenerateScript();
        Assert.Contains("ingresdate NOT NULL", script);
        Assert.DoesNotContain("START TRANSACTION", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("COMMIT;", script);
    }
}
