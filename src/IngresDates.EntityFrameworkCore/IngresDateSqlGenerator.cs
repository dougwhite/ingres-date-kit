using System.Linq.Expressions;
using Actian.EFCore.Infrastructure.Internal;
using Actian.EFCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace IngresDates.EntityFrameworkCore;
public sealed class IngresDateSqlGeneratorFactory(QuerySqlGeneratorDependencies dependencies, IRelationalTypeMappingSource mappings, IActianSingletonOptions options) : IQuerySqlGeneratorFactory
{
    public QuerySqlGenerator Create() => new IngresDateSqlGenerator(dependencies, mappings, options);
}

public sealed class IngresDateSqlGenerator(QuerySqlGeneratorDependencies dependencies, IRelationalTypeMappingSource mappings, IActianSingletonOptions options) : ActianQuerySqlGenerator(dependencies, mappings, options)
{
    private int selectDepth;
    private static bool IsRich(RelationalTypeMapping? mapping) => mapping?.ClrType == typeof(IngresDate) || mapping?.ClrType == typeof(IngresInterval);
    protected override Expression VisitSelect(SelectExpression expression)
    {
        selectDepth++;
        try
        {
            return base.VisitSelect(expression);
        }
        finally
        {
            selectDepth--;
        }
    }

    protected override Expression VisitProjection(ProjectionExpression expression)
    {
        // Only the outermost result crosses the wire. Casting inner projections
        // would change the types used by enclosing filters, joins and arithmetic.
        if (selectDepth != 1 || !IsRich(expression.Expression.TypeMapping))
            return base.VisitProjection(expression);
        Sql.Append("CAST(");
        Visit(expression.Expression);
        Sql.Append(" AS varchar(200))");
        if (!string.IsNullOrEmpty(expression.Alias))
            Sql.Append(" AS ").Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(expression.Alias));
        return expression;
    }

    protected override Expression VisitSqlParameter(SqlParameterExpression expression)
    {
        if (!IsRich(expression.TypeMapping))
            return base.VisitSqlParameter(expression);
        Sql.Append("ingresdate(");
        base.VisitSqlParameter(expression);
        Sql.Append(")");
        return expression;
    }
}
