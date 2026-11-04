using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IngresDates.Portable;
/// <summary>
/// A lossless storage adapter, not an emulation of Ingres SQL arithmetic.
/// JSON null is never used for a blank: the discriminator preserves that state.
/// SQL NULL remains SQL NULL through EF's normal nullable-property behaviour.
/// </summary>
public sealed class PortableDateConverter : ValueConverter<IngresDate, string>
{
    public PortableDateConverter() : base(value => Serialize(value), text => Deserialize(text))
    {
    }

    private static string Serialize(IngresDate value) => JsonSerializer.Serialize(value);
    private static IngresDate Deserialize(string text) => JsonSerializer.Deserialize<IngresDate>(text);
}

public sealed class PortableDateComparer : ValueComparer<IngresDate>
{
    public PortableDateComparer() : base((a, b) => a.HasSameRepresentation(b), value => HashCode.Combine(value.Kind, value.ToString(), value.TimeZoneId), value => value)
    {
    }
}

public static class PortableDateConventions
{
    /// <summary>Use with PostgreSQL, SQL Server or SQLite; do not use on native Ingres columns.</summary>
    public static void UsePortableIngresDates(this ModelConfigurationBuilder builder) => builder.Properties<IngresDate>().HaveConversion<PortableDateConverter, PortableDateComparer>().HaveMaxLength(512).AreUnicode(false);
}
