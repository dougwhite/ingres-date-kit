using System.Data;
using System.Data.Common;
using Actian.EFCore.Storage.Internal;
using Ingres.Client;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IngresDates.EntityFrameworkCore;
/// <summary>
/// EF's model type stays rich; the wire representation is text. Projection SQL
/// casts to varchar before the driver sees it, because this driver cannot read
/// interval-valued ingresdate results. Database predicates remain native dates.
/// </summary>
public sealed class IngresDateMapping : RelationalTypeMapping
{
    private static ValueConverter<IngresDate, string> TextConverter(string timeZoneId, DateProfile profile)
        => new(value => Write(value, profile), text => Read(text, timeZoneId, profile));
    private static string Write(IngresDate value, DateProfile profile)
    {
        DateProfiles.Validate(value, profile);
        return value.ToIngresText();
    }

    private static IngresDate Read(string text, string timeZoneId, DateProfile profile)
    {
        var value = IngresDate.Parse(text, timeZoneId);
        DateProfiles.Validate(value, profile);
        return value;
    }

    private static readonly ValueComparer<IngresDate> RepresentationComparer = new(
        (a, b) => a.HasSameRepresentation(b),
        value => HashCode.Combine(value.Kind, value.ToString(), value.TimeZoneId),
        value => value);
    public IngresDateMapping(string timeZoneId, DateProfile profile = DateProfile.Native)
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
                typeof(IngresDate),
                converter: TextConverter(timeZoneId, profile),
                comparer: RepresentationComparer),
            "ingresdate",
            dbType: System.Data.DbType.AnsiString))
    {
    }

    private IngresDateMapping(RelationalTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new IngresDateMapping(parameters);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        // The SQL generator wraps query parameters in ingresdate(...). INSERT
        // and UPDATE assignments use Ingres's tested varchar-to-date coercion.
        ((IngresParameter)parameter).IngresType = IngresType.VarChar;
        parameter.Size = 200;
    }

    protected override string GenerateNonNullSqlLiteral(object value) => "ingresdate('" + value.ToString()!.Replace("'", "''") + "')";
}

public sealed class IngresIntervalMapping : RelationalTypeMapping
{
    private static readonly ValueConverter<IngresInterval, string> TextConverter = new(value => value.ToString(), text => IngresInterval.Parse(text));
    private static readonly ValueComparer<IngresInterval> RepresentationComparer = new(
        (a, b) => a.HasSameRepresentation(b),
        value => HashCode.Combine(value.Years, value.Months, value.Milliseconds),
        value => value);

    public IngresIntervalMapping()
        : base(new RelationalTypeMappingParameters(
            new CoreTypeMappingParameters(
                typeof(IngresInterval),
                converter: TextConverter,
                comparer: RepresentationComparer),
            "ingresdate",
            dbType: System.Data.DbType.AnsiString))
    {
    }

    private IngresIntervalMapping(RelationalTypeMappingParameters parameters) : base(parameters)
    {
    }

    protected override RelationalTypeMapping Clone(RelationalTypeMappingParameters parameters) => new IngresIntervalMapping(parameters);
    protected override void ConfigureParameter(DbParameter parameter)
    {
        ((IngresParameter)parameter).IngresType = IngresType.VarChar;
        parameter.Size = 200;
    }

    protected override string GenerateNonNullSqlLiteral(object value) => "ingresdate('" + value.ToString()!.Replace("'", "''") + "')";
}

public sealed class IngresDateTypeMappingSource(
    TypeMappingSourceDependencies dependencies,
    RelationalTypeMappingSourceDependencies relationalDependencies,
    IngresDateSettings settings)
    : ActianTypeMappingSource(dependencies, relationalDependencies)
{
    protected override RelationalTypeMapping? FindMapping(in RelationalTypeMappingInfo info)
    {
        if (info.ClrType is null && info.StoreTypeNameBase == "ingresdate")
            return new IngresDateMapping(settings.TimeZoneId);
        if (info.ClrType == typeof(IngresDate))
            return new IngresDateMapping(settings.TimeZoneId);
        if (info.ClrType == typeof(IngresInterval))
            return new IngresIntervalMapping();
        return base.FindMapping(info);
    }

    public override RelationalTypeMapping? FindMapping(IProperty property)
    {
        if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(IngresDate) && property.FindAnnotation("IngresDates:Profile")?.Value is DateProfile profile)
            return new IngresDateMapping(settings.TimeZoneId, profile);
        return (RelationalTypeMapping?)base.FindMapping(property);
    }
}
