using IngresDates;
using IngresDates.EntityFrameworkCore;
using Ingres.Client;
using Microsoft.EntityFrameworkCore;
using PortableDemo;

string provider = args.FirstOrDefault() ?? "sqlite";
string? connection = Environment.GetEnvironmentVariable("PORTABLE_CONNECTION");
var options = new DbContextOptionsBuilder<PortableContext>();
switch (provider)
{
    case "ingres":
        var native = new IngresConnectionStringBuilder(connection ?? throw new Exception("Set PORTABLE_CONNECTION to the Ingres example database."))
        {
            DateFormat = "SWEDEN",
            Timezone = "Australia-Melbourne"
        };
        options.UseActian(native.ConnectionString).UseIngresDates("Australia/Melbourne");
        break;
    case "sqlite":
        options.UseSqlite(connection ?? "Data Source=:memory:");
        break;
    case "postgres":
        options.UseNpgsql(connection ?? throw new Exception("Set PORTABLE_CONNECTION to a dedicated test database."));
        break;
    case "sqlserver":
        options.UseSqlServer(connection ?? throw new Exception("Set PORTABLE_CONNECTION to a dedicated test database."));
        break;
    default:
        throw new ArgumentException("Choose ingres, postgres, sqlserver, or sqlite.");
}

using var db = new PortableContext(options.Options);
if (provider == "sqlite")
    db.Database.OpenConnection();
// Deliberately require a dedicated new database; do not use this demo against an
// existing application database. EnsureCreated never drops existing objects.
if (provider != "ingres")
    db.Database.EnsureCreated();
if (!db.Rows.Any())
    db.Rows.AddRange(
        new PortableRow { Id = 1, Value = IngresDate.Empty, NullableValue = null },
        new PortableRow
        {
            Id = 2,
            Value = IngresDate.Parse("2026-01-31"),
            NullableValue = IngresDate.Empty
        },
        new PortableRow
        {
            Id = 3,
            Value = IngresDate.Parse("5 days"),
            NullableValue = IngresDate.Parse("1 month")
        });
db.SaveChanges();
db.ChangeTracker.Clear();
foreach (var row in db.Rows.OrderBy(x => x.Id))
    Console.WriteLine($"{row.Id}: {row.Value.Kind} [{row.Value}], SQL null={row.NullableValue is null}");
Console.WriteLine("The same entity and application code ran with provider: " + provider + ". PostgreSQL/SQL Server use lossless JSON storage; Ingres uses native ingresdate.");
