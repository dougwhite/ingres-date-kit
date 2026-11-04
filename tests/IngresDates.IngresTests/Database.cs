using DateDemo;
using Ingres.Client;
using IngresDates;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace IngresDates.IngresTests;
internal static class Database
{
    public static string Connection => DemoContext.ConnectionString(Environment.GetEnvironmentVariable("INGRESDATE_CONNECTION") ?? throw new InvalidOperationException("Integration tests require INGRESDATE_CONNECTION. They never silently skip."));

    public static DemoContext Open() => DemoContext.Open(Connection);
    public static object? Scalar(string sql, params IngresDate[] values)
    {
        using var connection = new IngresConnection(Connection);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        for (int i = 0; i < values.Length; i++)
            command.Parameters.Add(new IngresParameter("p" + i, IngresType.VarChar) { Value = values[i].ToIngresText() });
        return command.ExecuteScalar();
    }
}
