# Using the kit in an existing EF application

## Add the code and register services

Start with ProjectReferences to `IngresDates.EntityFrameworkCore` and `IngresDates`. The supplied provider source builds against EF 10.0.3. Do not load two different versions of Actian.EFCore into one application. For an existing project using an official package, first align the provider/EF versions and then replace the reference with this tested provider build, or port the extensions and rerun the suite against the chosen official package.

```csharp
var native = new IngresConnectionStringBuilder(connectionString)
{
    DateFormat = "SWEDEN",
    Timezone = "Australia-Melbourne"
};
services.AddDbContext<MyContext>(options =>
    options.UseActian(native.ConnectionString)
        .UseIngresDates("Australia/Melbourne"));
```

Keep credentials in your existing secret configuration. Setting DateFormat and time zone is part of this adapter's explicit wire contract.

Change a native entity property from DateTime to `IngresDate` (or `IngresDate?` only for a genuinely nullable column). Native `ingresdate` mapping is automatic. Do not add the portable text convention to the native context. The example fixture uses schema owner `ingres`; adapt `ToTable` to the actual owner in your application.

## Per-column policies and familiar application views

```csharp
row.Property(x => x.Native); // Full native model.
row.Property(x => x.DueValue)
    .HasIngresDateProfile(DateProfile.DateTimeOnly);
```

A nullable facade over a NOT NULL rich property looks like this:

```csharp
public IngresDate DueValue { get; set; }

[NotMapped, IngresOptionalDate(nameof(DueValue))]
public DateTime? Due
{
    get => DueValue.ToOptionalDateTime();
    set => DueValue = IngresDate.FromOptionalDateTime(value);
}
```

This lets existing application code assign `Due = null`, and lets ordinary LINQ use `Where(x => x.Due == null)`. It requires the registration above. The facade is not a second database column. Apply DateTimeOnly validation to its rich backing property so intervals fail clearly instead of masquerading as dates. `IngresDateSentinel` follows the same pattern; see the compiled example for a 9999-12-31 view. Reserve that date in the application domain.

A truly nullable backing column can contain both SQL NULL and blank. Keep it as `IngresDate?` if both states matter. Do not collapse them through a getter and later expect to infer the original state from application null. Neither EF nor a converter can recover information you discarded.

## Supported LINQ examples

```csharp
var blanks = db.Rows.Where(x => x.Native.IsEmpty);
var trueNulls = db.Rows.Where(x => x.NullableNative == null);
var nativeEmpty = db.Rows.Where(x => x.NullableNative == IngresDate.Empty);
var dates = db.Rows.Where(x => x.Native.IsAbsolute);
var intervals = db.Rows.Where(x => x.Native.IsInterval);
IngresDate fiveDays = IngresInterval.Days(5);
var shifted = db.Rows.Select(x => x.Native + fiveDays);
var missing = db.Rows.Where(x => x.Due == null);
```

Inspect SQL with `ToQueryString()` when introducing a new expression. Arbitrary methods on the struct do not automatically translate. Use the provided predicates/operators/functions; add a specific translator and a real Ingres test for new server operations. EF may evaluate a final projection on the client, but a non-translatable filter should fail rather than quietly loading a whole table. Do not use AsEnumerable as a performance workaround without a deliberate row bound.

## Reverse engineering existing schemas

The example contains a compiled `IDesignTimeServices` implementation. Put the equivalent in your startup project so scaffolding uses the rich mapping. It registers `IngresDateSettings` and `IngresDateTypeMappingSource`. This is separate from runtime context registration.

```sh
dotnet tool restore
dotnet ef dbcontext scaffold "$INGRESDATE_CONNECTION" Actian.EFCore \
  --project examples/DateDemo --context NewScaffoldContext \
  --output-dir NewScaffold --table ingres.date_samples --no-onconfiguring
```

PowerShell uses `$env:INGRESDATE_CONNECTION`. Prefer your established secure configuration reference for connection strings where supported; the command above passes an expanded secret as a process argument. `--no-onconfiguring` prevents credentials being generated into source. Do not paste command output containing a password into tickets.

`examples/DateDemo/Scaffolded` is actual generated output from the fixture, not a manually drawn approximation. All ingresdate columns become `IngresDate` / `IngresDate?`. A schema cannot tell the scaffolder whether an ingresdate column is interval-only or whether its blanks should become application null, so apply those domain policies explicitly in a partial context/model. Custom templates may generate facades from your own metadata, but are not included here.

## New schemas and migrations

New rich properties map to native `ingresdate` without repeating a converter on each property. The checked-in initial migration and snapshot were generated by EF 10. They illustrate new-table DDL, native types, nullability and the literal blank default `''`. They are not an upgrade script for a pre-existing OpenROAD table.

```sh
dotnet ef migrations add MyChange --project YOUR_PROJECT --context YOUR_CONTEXT
dotnet ef migrations script --project YOUR_PROJECT --context YOUR_CONTEXT
```

Automatic `database update` is blocked by a provider locking defect in this pinned build. The initial SQL script was applied to a fresh Ingres database through the native monitor. See [known provider issues](KNOWN-PROVIDER-ISSUES.md) for the verified script path and why locking is not silently disabled. Review the generated SQL, schema owner and defaults before running migrations in your deployment workflow. One migration set is provider-specific; keep separate migration assemblies for different physical schemas/providers. The portable schema stores JSON text and is a different physical schema from native Ingres, even when the CLR entity is the same.

## Frontend/API boundaries

For native values, use the discriminated JSON representation and display blanks/intervals explicitly. For ordinary date forms, expose the validated nullable facade. A browser date input has no interval state. The small web example lets you inspect native JSON alongside a nullable field, and clear the latter back to a native blank.

DTO mapping, validation messages, exports, reporting, bulk loaders, and raw ADO.NET code must choose the same policy. This kit is registered inside EF; it does not globally change DateTime or every third-party grid, serializer, migration tool, or bulk extension.
