# How the implementation works

## The value model

`IngresDate` is an immutable struct with `Empty`, `Date`, `Timestamp`, and `Interval` kinds. An `IngresDate?` containing null is SQL NULL. `default(IngresDate)` is the native empty date. The type never silently converts intervals or blanks to DateTime. DateOnly converts implicitly; converting to DateTime is explicit and validates the kind.

`IngresInterval` keeps calendar years, months and a millisecond day/time component. A calendar month is retained as a month when adding to dates. Comparison uses the Ingres normalization observed in the oracle suite: calendar months normalize into years; a year compares as 365 days and a remaining month as 30 days. Consequently 12 months equals 365 days, while one month equals 30 days. Repeated addition can differ from adding the combined calendar interval because end-of-month clamping applies at each step.

Comparison equality is intentionally different from representation identity. A date and the corresponding midnight timestamp can compare equal; a month and 30 days can compare equal. EF's custom comparers retain the representation change so an edit is not silently ignored. Keep this distinction in mind for dictionaries, sets, and domain equality.

JSON uses a discriminator, a canonical text value and, where present, a time-zone identifier. A blank is an object with kind `empty`; SQL null is JSON null. Do not feed a browser date input an interval or a synthetic sentinel without a deliberate UI policy. The UI example displays native JSON separately from its nullable facade.

## Why the EF integration has several pieces

1. The mapping source tells runtime EF and the scaffolder that `ingresdate` can be `IngresDate`, and maps typed `IngresInterval` properties to the same store type.
2. Value converters encode parameters and decode canonical text. These converters alone cannot fix the legacy driver result path.
3. The SQL generator casts only the outer rich result projection to `varchar(200)`. The tested client cannot read interval-valued native results directly, even through `GetString`. Reading varchar avoids that failure and preserves the distinction between a date, a midnight timestamp, an interval, and empty.
4. Inner query expressions remain native dates. Predicates, ordering and addition run in Ingres, not over their text representations. Query parameters are wrapped in `ingresdate(...)`; assignment coercion for INSERT/UPDATE was tested against Ingres.
5. Member/method translators implement the documented predicates, explicit functions and nullable view. Operators flow through EF's relational arithmetic translation.
6. The query interceptor replaces explicitly annotated facade properties with their backing expression. `RequiredDate == null` becomes a CASE expression over an empty native date; ordinary C# getter/setter logic implements the same view in memory.
7. Profile converters validate scalar result reads; the materialization/save interceptor validates entity reads and writes. Constraints are application policies, not automatically added CHECK constraints on an existing OpenROAD database.

All server inputs are parameters or escaped provider literals. User values are not concatenated into SQL.

## Arithmetic and the session context

The database is authoritative for expressions executed through EF. Local operators are tested against standard parameterized Ingres SQL for the supplied matrix of calendar, negative interval, month-end, leap-year and Melbourne DST cases. The test expectations are read from Ingres on each run rather than copied from the implementation.

A date-only value plus whole days stays date-only. Sub-day arithmetic creates a timestamp. Timestamps carry wall-clock fields; a declared time zone is needed to reproduce elapsed-time changes across DST. For example, in this fixture, 2026-10-03 12:00 plus one day becomes 2026-10-04 13:00 in Melbourne. Calendar adjustment retains calendar components and the original offset before rendering the final timestamp in the destination zone.

`UseIngresDates("Australia/Melbourne")` configures CLR interpretation. The connection uses Ingres `Timezone=Australia-Melbourne` and `DateFormat=SWEDEN`. Those are two names for matching contexts, not interchangeable strings. When changing the application zone, change both together and rerun the SQL oracle suite. The service-provider cache key includes the context; there is no mutable process-wide zone.

Ambiguous or nonexistent starting wall-clock timestamps throw in local arithmetic. Use `IngresSession.Add` / `Subtract`, whose database I/O is explicit, for an authoritative result in those cases or for unvalidated combinations. It returns rich text results without the driver DateTime conversion path. Arbitrary historical time-zone equivalence and all possible ingresdate expressions are not claimed.

## Deliberate design choices

- Policies are per property. A native rich field can coexist with an ordinary nullable view or a reserved sentinel view without a proliferation of incompatible wrapper structs.
- Native SQL NULL remains native SQL NULL. A nullable facade over a non-nullable backing column has the narrower blank-to-null contract.
- The portable adapter retains kind and calendar components as JSON text. Relational queries on those columns are a separate concern from lossless storage.
- Runtime and design-time use the same mapping source. Existing columns do not require a date-to-text schema migration just to use the native type.
- The provider is pinned and vendored because the extension uses provider services and internal API namespaces. EF/provider upgrades require rebuilding and rerunning these tests.
