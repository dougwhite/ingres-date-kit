using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IngresDates.EntityFrameworkCore;
/// <summary>Marks an ordinary nullable facade and names its mapped rich backing property.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IngresOptionalDateAttribute(string backingProperty) : Attribute
{
    public string BackingProperty { get; } = backingProperty;
}

/// <summary>Opt-in sentinel view. Reserve this date in the application's domain.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IngresDateSentinelAttribute(string backingProperty, int year, int month, int day) : Attribute
{
    public string BackingProperty { get; } = backingProperty;
    public DateTime Sentinel { get; } = new(year, month, day);
}

public sealed class OptionalDateQueryInterceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData) => new RewriteFacades().Visit(queryExpression);
    private sealed class RewriteFacades : ExpressionVisitor
    {
        protected override Expression VisitMember(MemberExpression node)
        {
            var sentinel = node.Member.GetCustomAttribute<IngresDateSentinelAttribute>();
            if (sentinel is not null)
            {
                var raw = Expression.Property(Visit(node.Expression!), sentinel.BackingProperty);
                var optional = Expression.Call(typeof(IngresDateFunctions), nameof(IngresDateFunctions.OptionalDateTime), null, raw);
                return Expression.Coalesce(optional, Expression.Constant(sentinel.Sentinel));
            }

            var attribute = node.Member.GetCustomAttribute<IngresOptionalDateAttribute>();
            if (attribute is null)
                return base.VisitMember(node);
            var backing = Expression.Property(Visit(node.Expression!), attribute.BackingProperty);
            return Expression.Call(typeof(IngresDateFunctions), nameof(IngresDateFunctions.OptionalDateTime), null, backing);
        }
    }
}
