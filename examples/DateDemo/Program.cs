using DateDemo;
using IngresDates;
using Microsoft.EntityFrameworkCore;

string connection = Environment.GetEnvironmentVariable("INGRESDATE_CONNECTION") ?? throw new InvalidOperationException("Set INGRESDATE_CONNECTION; see README.md. Credentials never belong in source.");
if (args.Contains("--serve"))
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args.Where(value => value != "--serve").ToArray(),
        WebRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot")
    });
    var app = builder.Build();
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapGet("/api/samples", () =>
    {
        using var db = DemoContext.Open(connection);
        return db.Samples.OrderBy(x => x.Id).ToList();
    });
    app.MapPost("/api/samples/{id:int}/clear", (int id) =>
    {
        using var db = DemoContext.Open(connection);
        var row = db.Samples.Single(x => x.Id == id);
        row.RequiredDate = null;
        db.SaveChanges();
        return Results.Ok(row);
    });
    app.Run("http://localhost:5127");
    return;
}

using (var db = DemoContext.Open(connection))
{
    Console.WriteLine("Actual EF Core version: " + typeof(DbContext).Assembly.GetName().Version);
    foreach (var row in db.Samples.OrderBy(x => x.Id))
        Console.WriteLine($"{row.Id}: {row.Label}; {row.Native.Kind} [{row.Native}]; optional={row.RequiredDate}; interval={row.LeadTime}");
    var blanks = db.Samples.Where(x => x.RequiredDate == null).Select(x => x.Id);
    Console.WriteLine("\nOrdinary nullable facade query:\n" + blanks.ToQueryString());
    Console.WriteLine("Matched: " + string.Join(", ", blanks));
    IngresDate fiveDays = IngresInterval.Days(5);
    var query = db.Samples.Where(x => x.Native.IsAbsolute).Select(x => new { x.Id, Shifted = x.Native + fiveDays });
    Console.WriteLine("\nNative arithmetic query:\n" + query.ToQueryString());
    foreach (var row in query)
        Console.WriteLine($"{row.Id} + 5 days = {row.Shifted}");
}
