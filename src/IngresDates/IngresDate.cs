using System.Globalization;
using System.Text.Json.Serialization;

namespace IngresDates;
public enum IngresDateKind
{
    Empty,
    Date,
    Timestamp,
    Interval
}

/// <summary>
/// A non-SQL-null Ingres date value. Use IngresDate? for SQL NULL.
/// default(IngresDate) deliberately means Empty. Timestamp values are wall-clock
/// values in the configured database session time zone, not implicit UTC instants.
/// </summary>
[JsonConverter(typeof(IngresDateJsonConverter))]
public readonly struct IngresDate : IEquatable<IngresDate>, IComparable<IngresDate>
{
    public IngresDateKind Kind { get; }
    public string? TimeZoneId { get; }

    private readonly DateTime absolute;
    private readonly IngresInterval interval;
    private IngresDate(
        IngresDateKind kind, DateTime absolute, IngresInterval interval, string? timeZoneId = null)
        => (Kind, this.absolute, this.interval, TimeZoneId) = (kind, absolute, interval, timeZoneId);

    public static IngresDate Empty => default;
    public bool IsEmpty => Kind == IngresDateKind.Empty;
    public bool IsInterval => Kind == IngresDateKind.Interval;
    public bool IsAbsolute => Kind is IngresDateKind.Date or IngresDateKind.Timestamp;

    public static IngresDate FromDate(DateOnly value) => new(IngresDateKind.Date, value.ToDateTime(TimeOnly.MinValue), default);
    public static IngresDate FromTimestamp(DateTime value, string? timeZoneId = null)
    {
        if (value.Kind != DateTimeKind.Unspecified)
            throw new ArgumentException("Use an Unspecified wall-clock value in the configured Ingres session time zone.", nameof(value));
        if (value.Ticks % TimeSpan.TicksPerMillisecond != 0)
            throw new ArgumentException("Ingresdate has millisecond precision.", nameof(value));
        if (timeZoneId is not null)
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return new(IngresDateKind.Timestamp, value, default, timeZoneId);
    }

    public static IngresDate FromInterval(IngresInterval value) => new(IngresDateKind.Interval, default, value);
    public IngresInterval GetInterval() => IsInterval ? interval : throw new InvalidOperationException("The value is not an interval.");
    public DateTime GetDateTime() => IsAbsolute ? absolute : throw new InvalidOperationException("An empty date or interval is not a DateTime.");
    public DateTime? ToOptionalDateTime() => IsEmpty ? null : GetDateTime();
    public DateOnly? ToOptionalDate() => IsEmpty ? null : Kind == IngresDateKind.Date ? DateOnly.FromDateTime(absolute) : throw new InvalidOperationException("This profile requires a date without a time component.");
    public static IngresDate FromOptionalDateTime(DateTime? value) => value is null ? Empty : FromTimestamp(value.Value);
    public static IngresDate FromOptionalDate(DateOnly? value) => value is null ? Empty : FromDate(value.Value);
    public static implicit operator IngresDate(DateOnly value) => FromDate(value);
    public static implicit operator IngresDate(IngresInterval value) => FromInterval(value);
    public static explicit operator DateTime(IngresDate value) => value.GetDateTime();
    public static IngresDate Parse(string text) => Parse(text, null);
    public static IngresDate Parse(string text, string? timeZoneId)
    {
        text = text.Trim();
        if (text.Length == 0)
            return Empty;
        if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || DateOnly.TryParseExact(text, "yyyy_MM_dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return new(IngresDateKind.Date, date.ToDateTime(TimeOnly.MinValue), default, timeZoneId);
        string[] formats = ["yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.FFF", "yyyy_MM_dd HH:mm:ss", "yyyy_MM_dd HH:mm:ss.FFF"];
        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
            return FromTimestamp(timestamp, timeZoneId);
        return FromInterval(IngresInterval.Parse(text));
    }

    public override string ToString() => Kind switch
    {
        IngresDateKind.Empty => "",
        IngresDateKind.Date => absolute.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        IngresDateKind.Timestamp => absolute.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        _ => interval.ToString()
    };
    /// <summary>Only for parameter values and escaped literals, never string-interpolate user SQL.</summary>
    public string ToIngresText() => Kind switch
    {
        IngresDateKind.Date => absolute.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture),
        IngresDateKind.Timestamp => absolute.ToString("yyyy_MM_dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
        _ => ToString()
    };
    public int CompareTo(IngresDate other)
    {
        int rank = Rank(Kind).CompareTo(Rank(other.Kind));
        if (rank != 0)
            return rank;
        return IsEmpty ? 0 : IsInterval ? interval.CompareTo(other.interval) : absolute.CompareTo(other.absolute);
    }

    private static int Rank(IngresDateKind kind) => kind == IngresDateKind.Empty ? 0 : kind == IngresDateKind.Interval ? 1 : 2;
    // SQL comparison equality differs from representation identity. EF tracking
    // uses HasSameRepresentation so a date-only -> midnight timestamp edit is saved.
    public bool Equals(IngresDate other) => CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IngresDate other && Equals(other);
    public override int GetHashCode() => IsEmpty ? 0 : IsInterval ? interval.ComparisonMilliseconds.GetHashCode() : absolute.GetHashCode();
    public bool HasSameRepresentation(IngresDate other) => Kind == other.Kind
        && absolute == other.absolute
        && interval.HasSameRepresentation(other.interval)
        && TimeZoneId == other.TimeZoneId;
    public static bool operator ==(IngresDate a, IngresDate b) => a.Equals(b);
    public static bool operator !=(IngresDate a, IngresDate b) => !a.Equals(b);
    public static bool operator <(IngresDate a, IngresDate b) => a.CompareTo(b) < 0;
    public static bool operator>(IngresDate a, IngresDate b) => a.CompareTo(b) > 0;
    public static bool operator <=(IngresDate a, IngresDate b) => a.CompareTo(b) <= 0;
    public static bool operator >=(IngresDate a, IngresDate b) => a.CompareTo(b) >= 0;
    public static IngresDate operator +(IngresDate a, IngresDate b)
    {
        if (a.IsEmpty || b.IsEmpty)
            return Empty;
        if (a.IsInterval && b.IsInterval)
            return FromInterval(a.interval + b.interval);
        if (a.IsInterval && b.IsAbsolute)
            return b + a;
        if (!a.IsAbsolute || !b.IsInterval)
            throw new InvalidOperationException("Only a date plus interval, or two intervals, can be added.");
        var i = b.interval;
        var value = a.absolute.AddMonths(checked(i.Years * 12 + i.Months));
        bool producesTimestamp = a.Kind == IngresDateKind.Timestamp || i.Milliseconds % 86_400_000 != 0;
        if (producesTimestamp && a.TimeZoneId is not null)
        {
            // Ingres timestamp + one day advances an elapsed 24 hours. Across
            // Melbourne DST this can change the displayed wall-clock hour.
            var zone = TimeZoneInfo.FindSystemTimeZoneById(a.TimeZoneId);
            if (zone.IsAmbiguousTime(a.absolute) || zone.IsInvalidTime(a.absolute))
                throw new InvalidOperationException("Ambiguous or missing wall-clock time: evaluate this operation with IngresSession instead.");
            // Calendar clamping uses the original wall date, but the resulting
            // elapsed adjustment retains the original offset before re-rendering
            // in the destination timezone. This also handles Jan 31 at midnight.
            var utc = new DateTimeOffset(value, zone.GetUtcOffset(a.absolute)).UtcDateTime.AddTicks(checked(i.Milliseconds * TimeSpan.TicksPerMillisecond));
            value = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        }
        else
            value = value.AddTicks(checked(i.Milliseconds * TimeSpan.TicksPerMillisecond));
        return a.Kind == IngresDateKind.Date && i.Milliseconds % 86_400_000 == 0 ? new(IngresDateKind.Date, value, default, a.TimeZoneId) : FromTimestamp(value, a.TimeZoneId);
    }

    public static IngresDate operator -(IngresDate a, IngresDate b)
    {
        if (a.IsEmpty || b.IsEmpty)
            return Empty;
        if (b.IsInterval)
            return a + FromInterval(-b.interval);
        if (a.IsAbsolute && b.IsAbsolute)
        {
            if (a.Kind == IngresDateKind.Timestamp && b.Kind == IngresDateKind.Timestamp && a.TimeZoneId is not null)
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(a.TimeZoneId);
                if (zone.IsAmbiguousTime(a.absolute) || zone.IsAmbiguousTime(b.absolute) || zone.IsInvalidTime(a.absolute) || zone.IsInvalidTime(b.absolute))
                    throw new InvalidOperationException("Evaluate ambiguous timestamp subtraction with IngresSession.");
                return FromInterval(IngresInterval.FromTimeSpan(TimeZoneInfo.ConvertTimeToUtc(a.absolute, zone) - TimeZoneInfo.ConvertTimeToUtc(b.absolute, zone)));
            }

            return FromInterval(IngresInterval.FromTimeSpan(a.absolute - b.absolute));
        }

        throw new InvalidOperationException("An interval cannot have an absolute date subtracted from it.");
    }
}
