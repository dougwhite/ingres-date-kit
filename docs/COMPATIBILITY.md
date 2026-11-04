# Supported behaviour and boundaries

| Area | Supplied / verified | Boundary |
| --- | --- | --- |
| Value model | Empty, date, timestamp, interval; separate nullable SQL state; JSON round trips | Canonical explicit-unit interval text; not an OpenROAD locale parser |
| Ingres runtime | EF Core 10.0.3, Actian provider net10 source, licensed client 8.0.8, Ingres 12 | Validate other versions before adoption |
| Reading/writing | Native rich values, intervals, true nulls, blanks, insert/update, non-null blank facade | Direct driver GetDateTime is not overridden |
| LINQ | Native comparisons/operators, kind predicates, nullable and sentinel facade examples, nested queries | No blanket translation of arbitrary custom methods |
| Policies | Native, DateOnly, DateTimeOnly, IntervalOnly; optional and sentinel application views | Profiles enforce application validation, not automatically DB CHECK constraints |
| Tooling | Actual rich-type scaffold output; generated initial migration/snapshot; initial SQL applied on a fresh Ingres database | Automatic database update blocked by upstream lock SQL; use reviewed scripts. Domain policies cannot be inferred from schema alone; no custom template generator |
| Local arithmetic | Oracle matrix includes normalization, leap/month-end, negative units and Melbourne DST | Not a formal proof over every legal/historical ingresdate; ambiguous local timestamps use IngresSession |
| PostgreSQL / SQL Server | Shared app builds; same entity/values; lossless JSON text conventions | Live servers not run; native Ingres query/arithmetic emulation is not supplied |
| SQLite | Executed shared demo and storage round-trip suite | Storage harness only |
| UI | Executed local HTTP/JSON smoke checks; simple clear-to-blank UI | Not a production auth, validation, paging, or concurrency layer |
| Deployment | Source, build scripts, tests, fixture, native export | Recipient supplies .NET, public NuGet dependencies, licensed driver and database access |

Ingres supports semantics beyond this kit's exercised cases. Partial/relative date strings, locale-dependent date input, historic Gregorian calendar gaps, timestamp overflow, extreme intervals and differing time-zone databases deserve targeted tests before use. Values use .NET DateTime's range and Gregorian representation and millisecond precision; submillisecond inputs are rejected. SQL server evaluation remains authoritative.

Core values without a declared zone perform wall-clock arithmetic. When replicating session-sensitive logic locally, construct/parse values with the matching IANA zone. Do not combine values from unrelated session zones and assume instant equality. Comparison follows the same-session wall-clock contract used by this fixture. For cross-session instant comparisons, define a separate application instant model.

The profiles are intentionally compositional rather than a family of similarly named classes. `IngresInterval` is the strict interval type; `IngresDate` carries all native kinds. Optional and sentinel facades make narrowed application contracts explicit while retaining an inspectable rich backing field.

This package demonstrates that .NET/EF extensibility can remove the immediate integration barrier. Adopting it should include the team's own business cases, chosen Actian driver/provider versions, deployment timezone, actual schema owner, and query inventory.
