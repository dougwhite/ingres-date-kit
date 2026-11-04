# Provider migration issues found during validation

The pinned Actian EF Core 10 source has two migration issues independent of rich dates. These were found by actually generating and attempting to apply the example migration on disposable Ingres databases.

1. Automatic `dotnet ef database update` / `Database.Migrate()` calls SQL Server's `sp_getapplock` and `sp_releaseapplock`. Ingres rejects the syntax. This handoff does not bypass migration locking or claim to repair the provider's automatic migration orchestration. Use the reviewed script path below, with your deployment process providing a single schema writer. A production provider fix needs a real Ingres concurrency protocol and concurrent-migrator testing.
2. The generic `START TRANSACTION` statement in generated SQL is invalid in the tested Ingres SQL session. The custom `IngresDateSqlGenerationHelper` removes that statement: Ingres starts the transaction implicitly, and the generated COMMIT ends it. `UseIngresDates` registers this small helper; no vendored runtime source was changed.

## Verified path for this initial example

The checked-in `database/ef-initial.sql` was generated from the checked-in EF migration using the registered services. The corrected script was applied successfully to a fresh database. `database/ef-initial.monitor.sql` is the same fixed sample with Ingres terminal-monitor execution markers in place of SQL statement separators; it is not a general-purpose SQL script converter.

Use this path only against a new, empty example database, as the account matching the `ingres` table owner:

```sh
createdb YOUR_EMPTY_MIGRATION_DEMO -no_x100
sql -s YOUR_EMPTY_MIGRATION_DEMO < database/ef-initial.monitor.sql
```

This creates the schema and EF history entry, not fixture data. For the fully populated example and integration tests, restore `database/export/copy.in` instead. Do not run the initial migration over that restored database: its native tables already exist.

For your own changes, generate and review a non-idempotent script with `dotnet ef migrations script`, then use your established Ingres deployment tooling. The upstream idempotent script templates also use SQL Server-style IF/BEGIN syntax; no idempotent-script support is claimed here. Keep a reviewed baseline/history strategy for pre-existing OpenROAD databases. Treat automatic migration support as a provider follow-up, rather than a reason to abandon the type mapping or native EF query layer.
