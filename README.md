# Ingres dates in .NET and EF Core 10

A readable reference implementation for an OpenROAD/Ingres application moving into .NET. It preserves four native `ingresdate` states: empty date, date without time, timestamp, and interval. `IngresDate?` adds a separate SQL NULL state.

**A single connection setting does not provide this model, its arithmetic, EF translation, or application policies.** This project implements those pieces explicitly. It does not retest the `BlankDate` switch. The native example always reads rich results through a controlled text projection, without relying on a driver's DateTime sentinel.

The code has been built with .NET 10 and EF Core 10.0.3 and exercised against a real Ingres 12 database. Treat it as a tested integration example to review and adopt, rather than an assertion that every Ingres expression or every EF feature is supported. See [verification](evidence/VERIFICATION.md) and the [support boundaries](docs/COMPATIBILITY.md). Automatic migration application has an upstream provider locking defect; the verified SQL-script path is explained in [known provider issues](docs/KNOWN-PROVIDER-ISSUES.md).

## Start here

1. Read this page and run the examples.
2. Read [how it works](docs/IMPLEMENTATION.md), then [using it in an existing application](docs/INTEGRATION.md).
3. Inspect `examples/DateDemo` for native EF, nullable/sentinel facades, a browser UI, a generated migration, and real reverse-engineered source.
4. Inspect `examples/PortableDemo` for **one entity and one application flow across Ingres, PostgreSQL, and SQL Server**. SQLite provides a locally executable storage check.
5. Read [database setup](database/README.md) to restore the exact synthetic source database.

## Prerequisites and build

- .NET 10 SDK (tested with 10.0.100). `global.json` permits later feature bands in the 10.0 family.
- Your own licensed `Actian.Client` **8.0.8** NuGet package. Copy `actian.client.8.0.8.nupkg` into `local-packages/`. The proprietary driver is intentionally not redistributed. Its absence is the one licensing exception to a self-contained source handoff. Obtain it through your Actian entitlement; do not put it in Git.
- NuGet access for the public dependencies on first restore.
- Ingres integration runs additionally need a dedicated database restored from `database/export`, and an account that owns the example tables. No customer data or credentials are included.

From the repository root:

```sh
dotnet restore IngresDateKit.sln --locked-mode
dotnet build IngresDateKit.sln --no-restore
dotnet tool restore
dotnet test tests/IngresDates.Tests --no-build
dotnet test tests/IngresDates.PortableTests --no-build
dotnet run --project examples/PortableDemo --no-build -- sqlite
```

Set `INGRESDATE_CONNECTION` in your shell or IDE secret configuration, pointing at the restored database, using normal Actian connection-string syntax. Do not commit it. For example, with placeholders:

```sh
export INGRESDATE_CONNECTION='Host=YOUR_HOST;Port=II7;Database=YOUR_DEMO_DB;User ID=YOUR_USER;Password=YOUR_PASSWORD'
dotnet test tests/IngresDates.IngresTests --no-build
dotnet run --project examples/DateDemo --no-build
# Optional browser example: http://localhost:5127
dotnet run --project examples/DateDemo --no-build -- --serve
```

PowerShell uses `$env:INGRESDATE_CONNECTION = '...'`. The browser's clear button writes to the fixture database; use the disposable demo database. Restore the export again to reset it. Unit tests need no server; the integration suite fails clearly when its connection is absent and never reports a skipped suite as success. `scripts/test.sh` and `scripts/test.ps1` run the complete suite and require the native connection.

## The three-database snippet

The small adapter choice is the part that changes. The entity, null/blank/interval values, save, and read code stay the same. This excerpt matches the executable [PortableDemo](examples/PortableDemo/Program.cs):

```csharp
var options = new DbContextOptionsBuilder<PortableContext>();
switch (provider)
{
    case "ingres":
        var native = new IngresConnectionStringBuilder(connection)
        {
            DateFormat = "SWEDEN",
            Timezone = "Australia-Melbourne"
        };
        options.UseActian(native.ConnectionString)
            .UseIngresDates("Australia/Melbourne");
        break;
    case "postgres":
        options.UseNpgsql(connection);
        break;
    case "sqlserver":
        options.UseSqlServer(connection);
        break;
}

using var db = new PortableContext(options.Options);
db.Rows.Add(new PortableRow
{
    Id = 100,
    Value = IngresDate.Empty,       // An actual blank, not SQL NULL
    NullableValue = null           // A real SQL NULL
});
db.SaveChanges();
var rows = db.Rows.OrderBy(row => row.Id).ToList();
```

`PortableContext` selects native `ingresdate` on Ingres and discriminated JSON text storage elsewhere. This preserves the same rich values on all three. It does **not** make PostgreSQL or SQL Server run Ingres arithmetic over those JSON columns. The native path and SQLite round trips were executed; PostgreSQL/SQL Server adapters compile but were not connected to live servers in this example.

Run the same executable with a fresh dedicated target database:

```sh
# Set PORTABLE_CONNECTION to that provider's connection string first.
dotnet run --project examples/PortableDemo -- ingres
dotnet run --project examples/PortableDemo -- postgres
dotnet run --project examples/PortableDemo -- sqlserver
```

For Ingres, restore `multi_database_samples` from the supplied export. For the other providers the demo calls `EnsureCreated` in a dedicated empty database. It is a demonstration convenience, not an upgrade strategy for existing databases.

## Native .NET usage

```csharp
IngresDate blank = IngresDate.Empty;
IngresDate? sqlNull = null;
IngresDate date = new DateOnly(2026, 1, 31);
IngresDate fiveDays = IngresInterval.Days(5);
IngresDate due = date + fiveDays;

// These predicates and arithmetic execute in Ingres with UseIngresDates enabled.
var shifted = db.Samples
    .Where(row => row.Native.IsAbsolute)
    .Select(row => row.Native + fiveDays)
    .ToList();

// RequiredDate is an ordinary DateTime? facade over a NOT NULL ingresdate.
var missing = db.Samples.Where(row => row.RequiredDate == null).ToList();
row.RequiredDate = null; // Its setter writes IngresDate.Empty into the backing field.
db.SaveChanges();
```

Choose policies per column rather than inventing a separate CLR type for every combination:

| Application requirement | Implementation in this kit |
| --- | --- |
| Preserve every native state | `IngresDate` / `IngresDate?` |
| Intervals only, reject dates and blanks | `IngresInterval` |
| Date/timestamp/blank, reject intervals | `HasIngresDateProfile(DateProfile.DateTimeOnly)` |
| Date-only/blank, reject timestamps | `DateProfile.DateOnly` |
| Blank reads as application null, null writes blank | Mapped rich backing field + `[IngresOptionalDate]` nullable facade |
| Blank uses a reserved application sentinel | `[IngresDateSentinel]` facade + explicit getter/setter; example uses 9999-12-31 |
| Distinguish blank from SQL NULL in queries | `row.NullableNative == IngresDate.Empty` versus `== null` |

Sentinel conversion deliberately reserves that actual date; it cannot preserve its distinction from blank. Facades are explicit per-column views; EF converters do not receive SQL NULL automatically. The rich backing value is the authority whenever you need the full distinction.

## Repository contents

- `src/IngresDates`: independent value types, parsing, JSON, comparison, arithmetic and policy helpers.
- `src/IngresDates.EntityFrameworkCore`: native mapping, query translators, result transport, policy checks and facade rewrites.
- `src/IngresDates.Portable`: lossless text storage conventions.
- `examples/DateDemo`: console, small web UI, migrations, and actual scaffold output.
- `examples/PortableDemo`: shared application example with a provider switch.
- `tests`: unit, native EF/SQL oracle, and SQLite storage tests.
- `database/export`: actual `copydb` scripts and data from the database used in validation.
- `evidence`: checked-in results and exact validation boundaries.
- `vendor/Actian.EFCore`: pinned Apache-2.0 provider source, with documented local build changes.

There is no hidden production database, proprietary driver binary, password, or dependency cache in the example. See [third-party provenance](THIRD-PARTY-NOTICES.md).

## Licence

The original value-type library, integration code, examples, tests and documentation
are provided under the [MIT licence](LICENSE). Vendored Actian EF provider source
retains its upstream Apache-2.0 licence and file-level MIT notices; both licence
texts are included. The proprietary Actian.Client driver is not redistributed.
See [third-party notices](THIRD-PARTY-NOTICES.md) for provenance and the exact local
build changes. This example is independently maintained and is not an official
Actian release.

## Support

If this project is useful to you, you can [buy me a coffee](https://buymeacoffee.com/dougwhite).
