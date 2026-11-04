using Actian.EFCore.Storage.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace IngresDates.EntityFrameworkCore;

/// <summary>
/// Ingres begins a transaction implicitly with the first statement. The generic
/// relational START TRANSACTION emitted by the pinned provider is invalid here.
/// This affects generated migration scripts; COMMIT remains the explicit end.
/// </summary>
public sealed class IngresDateSqlGenerationHelper(RelationalSqlGenerationHelperDependencies dependencies)
    : ActianSqlGenerationHelper(dependencies)
{
    public override string StartTransactionStatement => "";
}
