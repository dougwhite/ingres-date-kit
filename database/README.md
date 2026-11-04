# The exact synthetic Ingres fixture

`export/` contains actual native `copydb` output and copied data, generated from the database used for the live tests. It is not a fabricated dump or a customer's database. The scripts contain only the two synthetic tables and their primary keys.

The final export was restored into a second empty database and the native test suite rerun against that restored copy. See `../evidence/VERIFICATION.md` for measured results and the checksum manifest.

## Restore the supplied source database

Use an Ingres command environment, as the account that will own/read the tables. The SQL client used for restore must have timezone `Australia-Melbourne` (equivalently `Australia/Melbourne` on Ingres 12): timestamp text in this export represents that local session. Use an appropriately configured client environment; do not change a shared server's configuration for this example. See [Actian's timezone documentation](https://docs.actian.com/actianx/12.0/Install/Time_Zone_Names.htm). The tested fixture owner is `ingres`. The example's `ToTable` schema names must match the restore owner if you choose a different account. Use a fresh disposable database; do not run copy.in over an existing application database.

```sh
createdb YOUR_NEW_DEMO_DB -no_x100
cd database/export
sql -s YOUR_NEW_DEMO_DB < copy.in
```

Inspect the SQL monitor output for errors and confirm 8 rows in `date_samples` and 3 in `multi_database_samples`. The terminal monitor can return a successful shell status even when an SQL statement reports an error. The full integration tests also verify the rows and their values.

`copy.in` uses relative data paths, so run it from the export directory. Preserve the `.ingres` files byte for byte: Ingres COPY's varchar external format includes a length prefix. SQL NULL uses the explicit marker `]^NULL^[`; an empty date is an empty field. Do not open/re-save these as a spreadsheet or normalize whitespace.

The COPY scripts set date format to `multinational4` for their external data representation. Runtime .NET connections deliberately use `SWEDEN`; these are independent transport contexts. The sample connection also sets the Ingres timezone to `Australia-Melbourne`.

## Tables and representative rows

`date_samples` contains:

- integer id primary key and varchar label;
- `native_value ingresdate NOT NULL` with empty/date/timestamp/interval examples;
- `nullable_value ingresdate` containing both a true SQL NULL and a blank;
- `lead_time ingresdate NOT NULL` used as strict `IngresInterval`;
- `required_date ingresdate NOT NULL DEFAULT ''` exposed through an optional DateTime facade.

The eight rows include a blank, January 31, noon and midnight timestamps, 5 days, 1 month, 1 year, and mixed negative interval components. `multi_database_samples` is the three-row native backing table for the shared-provider example.

`seed.sql` is a human-readable alternative initializer for a new empty database. Use either the seed or the export restore, not both. `export/` is the evidence of the exact test database.

## Reproduce an export

In an empty directory under the same owning account:

```sh
copydb YOUR_DEMO_DB -c -with_tables -with_data -with_index -with_constr \
  -relpath -no_loc date_samples multi_database_samples
sql -s YOUR_DEMO_DB < copy.out
```

`copydb` generates scripts; executing `copy.out` produces the actual data files. We used `-uingres` explicitly during validation because that is the real connection account for this isolated fixture. There are no PUBLIC grants in the seed, export, or setup.
