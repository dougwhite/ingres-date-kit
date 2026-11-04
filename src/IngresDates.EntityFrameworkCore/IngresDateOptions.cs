using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace IngresDates.EntityFrameworkCore;
public static class IngresDateOptions
{
    /// <summary>Call after UseActian. Register the same services at design time.</summary>
    public static DbContextOptionsBuilder UseIngresDates(this DbContextOptionsBuilder options, string timeZoneId = "UTC")
    {
        ((IDbContextOptionsBuilderInfrastructure)options).AddOrUpdateExtension(new IngresDateOptionsExtension(timeZoneId));
        options.ReplaceService<IRelationalTypeMappingSource, IngresDateTypeMappingSource>();
        options.ReplaceService<ISqlGenerationHelper, IngresDateSqlGenerationHelper>();
        options.ReplaceService<IQuerySqlGeneratorFactory, IngresDateSqlGeneratorFactory>();
        options.ReplaceService<IMemberTranslatorProvider, IngresDateMemberProvider>();
        options.ReplaceService<IMethodCallTranslatorProvider, IngresDateMethodProvider>();
        options.AddInterceptors(new OptionalDateQueryInterceptor(), new DateProfileValidationInterceptor());
        return options;
    }

    public static PropertyBuilder<IngresDate> HasIngresDateProfile(this PropertyBuilder<IngresDate> property, DateProfile profile) => property.HasColumnType("ingresdate").HasAnnotation("IngresDates:Profile", profile);
    public static PropertyBuilder<IngresDate?> HasIngresDateProfile(this PropertyBuilder<IngresDate?> property, DateProfile profile) => property.HasColumnType("ingresdate").HasAnnotation("IngresDates:Profile", profile);
}

/// <summary>Validate constrained columns on entity reads and before writes.</summary>
public sealed class DateProfileValidationInterceptor : SaveChangesInterceptor, IMaterializationInterceptor
{
    private static void Validate(object entity, Microsoft.EntityFrameworkCore.Metadata.IEntityType type)
    {
        foreach (var property in type.GetProperties())
            if (property.FindAnnotation("IngresDates:Profile")?.Value is DateProfile profile && property.PropertyInfo?.GetValue(entity)is IngresDate value)
                DateProfiles.Validate(value, profile);
    }

    public object InitializedInstance(MaterializationInterceptionData data, object entity)
    {
        Validate(entity, data.EntityType);
        return entity;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData data, InterceptionResult<int> result)
    {
        if (data.Context is not null)
            foreach (var entry in data.Context.ChangeTracker.Entries())
                Validate(entry.Entity, entry.Metadata);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data, InterceptionResult<int> result, CancellationToken token = default) => ValueTask.FromResult(SavingChanges(data, result));
}
