using Ingres.Client;

namespace IngresDates.EntityFrameworkCore;
/// <summary>
/// Explicit authoritative evaluation for operations that depend on the server's
/// timezone rules or unusual legacy values. Unlike value operators, these methods
/// perform database I/O; callers can see that dependency in their code.
/// </summary>
public sealed class IngresSession(string connectionString, string timeZoneId)
{
    public IngresDate Add(IngresDate left, IngresDate right) => Calculate(left, right, "+");
    public IngresDate Subtract(IngresDate left, IngresDate right) => Calculate(left, right, "-");
    private IngresDate Calculate(IngresDate left, IngresDate right, string operation)
    {
        using var connection = new IngresConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        // operation is selected inside this class, never supplied by application text.
        command.CommandText = $"select cast(ingresdate(?) {operation} ingresdate(?) as varchar(200))";
        command.Parameters.Add(new IngresParameter("left", IngresType.VarChar) { Value = left.ToIngresText() });
        command.Parameters.Add(new IngresParameter("right", IngresType.VarChar) { Value = right.ToIngresText() });
        return IngresDate.Parse((string)command.ExecuteScalar()!, timeZoneId);
    }
}
