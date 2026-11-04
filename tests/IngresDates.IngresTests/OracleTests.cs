using IngresDates;
using IngresDates.EntityFrameworkCore;
using Xunit;

namespace IngresDates.IngresTests;
public sealed class OracleTests
{
    public static IEnumerable<object[]> Additions()
    {
        string[] dates = ["2026-01-31", "2024-02-29", "2026-01-31 00:00:00", "2026-10-03 12:00:00", "2026-04-04 12:00:00", "2026-10-04", "2026-04-05", ""];
        string[] intervals = ["0 days", "5 days", "-5 days", "1 month", "-1 month", "1 year 1 month", "5 hours", "1 day"];
        foreach (var date in dates)
            foreach (var interval in intervals)
                yield return[date, interval];
    }

    [Theory]
    [MemberData(nameof(Additions))]
    public void CSharp_addition_matches_standard_Ingres_SQL(string leftText, string rightText)
    {
        var left = IngresDate.Parse(leftText, "Australia/Melbourne");
        var right = IngresDate.Parse(rightText);
        string serverText = (string)Database.Scalar("select cast(ingresdate(?) + ingresdate(?) as varchar(200))", left, right)!;
        var expected = IngresDate.Parse(serverText);
        Assert.Equal(expected, left + right);
    }

    public static IEnumerable<object[]> ComparisonPairs()
    {
        string[] values = ["", "-5 days", "0 days", "1 month", "30 days", "1 year", "365 days", "12 months", "13 months", "1 year -1 month", "-13 months", "2026-01-31", "2026-01-31 00:00:00", "2026-02-01"];
        foreach (var left in values)
            foreach (var right in values)
                yield return[left, right];
    }

    [Theory]
    [MemberData(nameof(ComparisonPairs))]
    public void CSharp_comparison_matches_standard_Ingres_SQL(string leftText, string rightText)
    {
        var left = IngresDate.Parse(leftText);
        var right = IngresDate.Parse(rightText);
        int actual = Convert.ToInt32(Database.Scalar("select case when ingresdate(?) < ingresdate(?) then -1 when ingresdate(?) > ingresdate(?) then 1 else 0 end", left, right, left, right));
        Assert.Equal(actual, Math.Sign(left.CompareTo(right)));
    }

    [Fact]
    public void Authoritative_session_handles_an_operation_with_explicit_database_IO()
    {
        var session = new IngresSession(Database.Connection, "Australia/Melbourne");
        var result = session.Add(IngresDate.Parse("2026-10-03 12:00:00"), IngresDate.Parse("1 day"));
        Assert.Equal(IngresDate.Parse("2026-10-04 13:00:00"), result);
    }

    [Theory]
    [InlineData("2026-10-04 12:00:00", "2026-10-03 12:00:00")]
    [InlineData("2026-04-05 12:00:00", "2026-04-04 12:00:00")]
    [InlineData("2026-02-05", "2026-01-31")]
    [InlineData("1 year 1 month", "5 days")]
    [InlineData("", "5 days")]
    public void Subtraction_matches_Ingres(string leftText, string rightText)
    {
        var left = IngresDate.Parse(leftText, "Australia/Melbourne");
        var right = IngresDate.Parse(rightText, "Australia/Melbourne");
        var expected = IngresDate.Parse((string)Database.Scalar("select cast(ingresdate(?) - ingresdate(?) as varchar(200))", left, right)!);
        Assert.Equal(expected, left - right);
    }
}
