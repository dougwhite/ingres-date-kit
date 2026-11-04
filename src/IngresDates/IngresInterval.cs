using System.Globalization;
using System.Text.RegularExpressions;

namespace IngresDates;
/// <summary>
/// An Ingres calendar interval. Calendar months normalize into whole years
/// before comparison: 12 months equals one year; one remaining month is 30 days.
/// Milliseconds hold the day/time part without discarding calendar components.
/// </summary>
public readonly record struct IngresInterval(int Years, int Months, long Milliseconds) : IComparable<IngresInterval>
{
    public static IngresInterval Days(int value) => new(0, 0, checked(value * 86_400_000L));
    public static IngresInterval CalendarMonths(int value) => new(0, value, 0);
    public static IngresInterval CalendarYears(int value) => new(value, 0, 0);
    public static IngresInterval FromTimeSpan(TimeSpan value)
    {
        if (value.Ticks % TimeSpan.TicksPerMillisecond != 0)
            throw new ArgumentException("Ingresdate has millisecond precision.", nameof(value));
        return new(0, 0, value.Ticks / TimeSpan.TicksPerMillisecond);
    }

    // This key is for comparison only. Do not use it to add calendar months.
    public decimal ComparisonMilliseconds => ((Years * 12L + Months) / 12) * 365m * 86_400_000 + ((Years * 12L + Months) % 12) * 30m * 86_400_000 + Milliseconds;

    public int CompareTo(IngresInterval other) => ComparisonMilliseconds.CompareTo(other.ComparisonMilliseconds);
    public bool Equals(IngresInterval other) => CompareTo(other) == 0;
    public override int GetHashCode() => ComparisonMilliseconds.GetHashCode();
    public bool HasSameRepresentation(IngresInterval other) => Years == other.Years && Months == other.Months && Milliseconds == other.Milliseconds;
    public static bool operator <(IngresInterval a, IngresInterval b) => a.CompareTo(b) < 0;
    public static bool operator>(IngresInterval a, IngresInterval b) => a.CompareTo(b) > 0;
    public static bool operator <=(IngresInterval a, IngresInterval b) => a.CompareTo(b) <= 0;
    public static bool operator >=(IngresInterval a, IngresInterval b) => a.CompareTo(b) >= 0;
    public static IngresInterval operator +(IngresInterval a, IngresInterval b) => new(checked(a.Years + b.Years), checked(a.Months + b.Months), checked(a.Milliseconds + b.Milliseconds));
    public static IngresInterval operator -(IngresInterval a) => new(checked(-a.Years), checked(-a.Months), checked(-a.Milliseconds));
    public static IngresInterval operator -(IngresInterval a, IngresInterval b) => a + -b;
    public override string ToString()
    {
        var parts = new List<string>();
        if (Years != 0)
            parts.Add($"{Years} years");
        if (Months != 0)
            parts.Add($"{Months} months");
        if (Milliseconds != 0 || parts.Count == 0)
            parts.Add((Milliseconds / 1000m).ToString("0.###", CultureInfo.InvariantCulture) + " seconds");
        return string.Join(" ", parts);
    }

    /// <summary>Parse explicit units, including the abbreviations returned by Ingres CAST.</summary>
    public static IngresInterval Parse(string text)
    {
        var matches = Regex.Matches(text, @"([+-]?\d+(?:\.\d+)?)\s*(years?|yrs?|months?|mos?|days?|hours?|hrs?|minutes?|mins?|seconds?|secs?|milliseconds?|msecs?)\b", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
            throw new FormatException("An interval needs explicit units, for example '5 days'.");
        var residue = Regex.Replace(text, @"([+-]?\d+(?:\.\d+)?)\s*(years?|yrs?|months?|mos?|days?|hours?|hrs?|minutes?|mins?|seconds?|secs?|milliseconds?|msecs?)\b", "", RegexOptions.IgnoreCase);
        if (!string.IsNullOrWhiteSpace(residue))
            throw new FormatException("Unrecognized interval text: " + text);
        int years = 0, months = 0;
        decimal milliseconds = 0;
        foreach (Match match in matches)
        {
            decimal value = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            string unit = match.Groups[2].Value.ToLowerInvariant();
            if (unit.StartsWith("y"))
                years = checked(years + Integral(value));
            else if (unit.StartsWith("mo"))
                months = checked(months + Integral(value));
            else
                milliseconds += value * (unit.StartsWith("d") ? 86_400_000 : unit.StartsWith("h") ? 3_600_000 : unit.StartsWith("min") ? 60_000 : unit.StartsWith("ms") || unit.StartsWith("mill") ? 1 : 1000);
        }

        if (milliseconds != decimal.Truncate(milliseconds))
            throw new FormatException("Ingresdate intervals require whole milliseconds.");
        return new(years, months, checked((long)milliseconds));
    }

    private static int Integral(decimal value) => value == decimal.Truncate(value) ? checked((int)value) : throw new FormatException("Calendar years and months must be integers.");
}
