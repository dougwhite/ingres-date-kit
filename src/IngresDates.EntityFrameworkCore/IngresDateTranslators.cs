using System.Reflection;
using Actian.EFCore.Infrastructure.Internal;
using Actian.EFCore.Query.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace IngresDates.EntityFrameworkCore;
public static class IngresDateFunctions
{
    public static DateTime? OptionalDateTime(IngresDate value) => value.ToOptionalDateTime();
    // Explicit server-side alternative to the operator syntax.
    public static IngresDate Add(IngresDate value, IngresDate interval) => value + interval;
    public static IngresDate Subtract(IngresDate value, IngresDate other) => value - other;
}

public sealed class IngresDateMemberProvider : ActianMemberTranslatorProvider
{
    public IngresDateMemberProvider(RelationalMemberTranslatorProviderDependencies dependencies, IRelationalTypeMappingSource mappings) : base(dependencies, mappings) => AddTranslators([new IngresDateMemberTranslator(dependencies.SqlExpressionFactory, mappings)]);
}

public sealed class IngresDateMemberTranslator(ISqlExpressionFactory sql, IRelationalTypeMappingSource mappings) : IMemberTranslator
{
    public SqlExpression? Translate(SqlExpression? instance, MemberInfo member, Type returnType, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (instance is null || member.DeclaringType != typeof(IngresDate))
            return null;
        var empty = sql.Constant(IngresDate.Empty, mappings.FindMapping(typeof(IngresDate)));
        var firstDate = sql.Constant(IngresDate.FromDate(new DateOnly(1582, 1, 1)), mappings.FindMapping(typeof(IngresDate)));
        return member.Name switch
        {
            nameof(IngresDate.IsEmpty) => sql.Equal(instance, empty),
            nameof(IngresDate.IsInterval) => sql.AndAlso(sql.NotEqual(instance, empty), sql.LessThan(instance, firstDate)),
            nameof(IngresDate.IsAbsolute) => sql.GreaterThanOrEqual(instance, firstDate),
            _ => null
        };
    }
}

public sealed class IngresDateMethodProvider : ActianMethodCallTranslatorProvider
{
    public IngresDateMethodProvider(RelationalMethodCallTranslatorProviderDependencies dependencies, IRelationalTypeMappingSource mappings, IActianSingletonOptions options) : base(dependencies, options) => AddTranslators([new IngresDateMethodTranslator(dependencies.SqlExpressionFactory, mappings)]);
}

public sealed class IngresDateMethodTranslator(ISqlExpressionFactory sql, IRelationalTypeMappingSource mappings) : IMethodCallTranslator
{
    public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> args, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        if (method.DeclaringType != typeof(IngresDateFunctions))
            return null;
        if (method.Name == nameof(IngresDateFunctions.Add))
            return sql.Add(args[0], args[1], mappings.FindMapping(typeof(IngresDate)));
        if (method.Name == nameof(IngresDateFunctions.Subtract))
            return sql.Subtract(args[0], args[1], mappings.FindMapping(typeof(IngresDate)));
        var raw = args[0];
        var empty = sql.Constant(IngresDate.Empty, mappings.FindMapping(typeof(IngresDate)));
        var dateTime = mappings.FindMapping("timestamp without time zone")!;
        // CAST avoids the legacy driver's ingresdate DateTime timezone conversion.
        return sql.Case([new CaseWhenClause(sql.Equal(raw, empty), sql.Constant(null, typeof(DateTime), dateTime))], sql.Convert(raw, typeof(DateTime), dateTime));
    }
}
