# Verification record — 4 November 2026

This is a record of executed checks, not a certification of every ingresdate or EF feature.

| Check | Executed result |
| --- | --- |
| Complete solution build | Passed, 0 warnings, 0 errors |
| Clean source ZIP extraction | Locked restore, tool restore, build and complete 326-test script passed after supplying the licensed client separately |
| Core value tests | 35 passed; 0 failed/skipped |
| Portable SQLite tests | 8 passed; 0 failed/skipped |
| Native Ingres + EF tests, source fixture | 283 passed; 0 failed/skipped |
| Same native suite, independently restored COPY fixture | 283 passed; 0 failed/skipped |
| Shared entity/application executable, Ingres | Passed, 3 expected rows |
| Same shared executable, SQLite | Passed, 3 expected rows |
| Native console / emitted SQL | Passed, all 8 rich rows; nullable predicate and native addition executed |
| Web example | GET page/JSON and POST clear succeeded; browser rows and layout inspected |
| Reverse engineering | EF scaffolder generated rich IngresDate / nullable properties; output included and compiled |
| Migration generation | Initial migration and snapshot generated; pending-model check reported no changes |
| Initial migration SQL | Corrected generated script applied successfully to a new Ingres database |
| Automatic migration application | Failed in upstream lock SQL; documented in docs/KNOWN-PROVIDER-ISSUES.md |
| PostgreSQL / SQL Server | Provider adapters compile; no live execution claimed |

There are **326 distinct tests** in the supplied suites. The 283 native tests were additionally rerun on a separately restored database. The native matrix includes 196 comparison pairs, 64 date/interval additions, 5 subtraction cases and 18 EF/session/schema tests. SQL oracle expectations are obtained from the server on each run.

## Exact stack and fixture

- .NET SDK 10.0.100, .NET runtime 10.0.0, EF Core 10.0.3.
- Vendored Actian EF provider: net10.0 branch commit b9d298a9f4bce47ca7b2be9c0af5c2f072285332, local build changes described in THIRD-PARTY-NOTICES.md.
- Already licensed Actian.Client 8.0.8, supplied locally for validation, excluded from delivery.
- Ingres II 12.0.0 (a64.lnx/100), native (non-X100) disposable databases.
- Source fixture: example_source_database; exported with native copydb as owner ingres.
- Final export restored to: example_restored_database. Both native suites use table owner ingres.
- Corrected EF migration SQL tested on a separate fresh database example_migration_database.
- Runtime DateFormat SWEDEN; server session timezone Australia-Melbourne; CLR context Australia/Melbourne.
- Source export: 8 date_samples rows and 3 multi_database_samples rows, with primary keys and native types.

`database-export.sha256` fingerprints the exact native export files. `results.json` and the TRX files record the measured counts. Logs and TRX preserve result data; local workspace paths (including case variants), test host identifiers and disposable database names were normalized to placeholders for publication. No credential or complete connection string is recorded.

All example data is synthetic. Integration test mutations use rolled-back transactions. The web clear endpoint was smoke-tested on an already-blank row so the exported fixture remained unchanged. The actual source and restored databases were separate from existing application databases.

## Practical limits

See docs/COMPATIBILITY.md. This run does not establish Windows runtime acceptance, every Ingres historic calendar/timezone case, every EF feature, automatic migrations, or live PostgreSQL/SQL Server semantics. The portable examples deliberately demonstrate shared storage/application code rather than an Ingres arithmetic emulator on those other engines.
