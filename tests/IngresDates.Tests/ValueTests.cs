using System.Text.Json;
using IngresDates;
using Xunit;

namespace IngresDates.Tests;
public sealed class ValueTests
{
    [Theory]
    [InlineData("", IngresDateKind.Empty)]
    [InlineData("2026-01-31", IngresDateKind.Date)]
    [InlineData("2026-01-31 00:00:00", IngresDateKind.Timestamp)]
    [InlineData("2026-01-31 12:34:56.123", IngresDateKind.Timestamp)]
    [InlineData("5 days", IngresDateKind.Interval)]
    [InlineData("1 yrs 2 mos -2 days -20 hrs", IngresDateKind.Interval)]
    public void Text_round_trip_preserves_kind_and_components(string text, IngresDateKind expected)
    {
        var value = IngresDate.Parse(text);
        Assert.Equal(expected, value.Kind);
        Assert.True(value.HasSameRepresentation(IngresDate.Parse(value.ToString())));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026-01-31")]
    [InlineData("2026-01-31 00:00:00")]
    [InlineData("5 days")]
    [InlineData("1 year 2 months -3 days 4 hours")]
    public void Json_round_trip_preserves_the_native_view(string text)
    {
        var value = IngresDate.Parse(text, "Australia/Melbourne");
        var restored = JsonSerializer.Deserialize<IngresDate>(JsonSerializer.Serialize(value));
        Assert.True(value.HasSameRepresentation(restored));
        Assert.Equal(value.TimeZoneId, restored.TimeZoneId);
    }

    [Fact]
    public void Sql_null_and_empty_are_distinct()
    {
        IngresDate? sqlNull = null;
        IngresDate? blank = IngresDate.Empty;
        Assert.Null(sqlNull);
        Assert.True(blank.HasValue);
    }

    [Fact]
    public void Optional_view_reverses_null_to_empty()
    {
        Assert.Null(IngresDate.Empty.ToOptionalDateTime());
        Assert.True(IngresDate.FromOptionalDateTime(null).IsEmpty);
    }

    [Fact]
    public void Intervals_do_not_silently_become_timestamps()
    {
        Assert.Throws<InvalidOperationException>(() => IngresDate.Parse("5 days").GetDateTime());
    }

    [Theory]
    [InlineData("1 month", "30 days", true)]
    [InlineData("1 year", "365 days", true)]
    [InlineData("1 year", "12 months", true)]
    public void Interval_comparison_uses_Ingres_units(string left, string right, bool equal)
    {
        Assert.Equal(equal, IngresDate.Parse(left) == IngresDate.Parse(right));
    }

    [Fact]
    public void Tracking_can_distinguish_equal_representations()
    {
        var date = IngresDate.Parse("2026-01-31");
        var midnight = IngresDate.Parse("2026-01-31 00:00:00");
        Assert.Equal(date, midnight);
        Assert.False(date.HasSameRepresentation(midnight));
        Assert.Equal(date.GetHashCode(), midnight.GetHashCode());
    }

    [Theory]
    [InlineData(DateProfile.DateOnly, "5 days")]
    [InlineData(DateProfile.DateOnly, "2026-01-31 00:00:00")]
    [InlineData(DateProfile.DateTimeOnly, "5 days")]
    [InlineData(DateProfile.IntervalOnly, "2026-01-31")]
    [InlineData(DateProfile.IntervalOnly, "")]
    public void Restricted_profiles_reject_the_wrong_kind(DateProfile profile, string value)
    {
        Assert.Throws<InvalidOperationException>(() => DateProfiles.Validate(IngresDate.Parse(value), profile));
    }

    [Fact]
    public void Sentinel_mapping_is_explicit_and_reversible()
    {
        var sentinel = new DateTime(9999, 12, 31, 23, 59, 59);
        Assert.Equal(sentinel, DateProfiles.ReadSentinel(IngresDate.Empty, sentinel));
        Assert.True(DateProfiles.WriteSentinel(sentinel, sentinel).IsEmpty);
    }

    [Theory]
    [InlineData("2026-01-31", "1 month", "2026-02-28")]
    [InlineData("2024-02-29", "1 year 1 month", "2025-03-29")]
    [InlineData("2026-01-31 12:00:00", "5 days", "2026-02-05 12:00:00")]
    [InlineData("", "5 days", "")]
    public void Local_addition_has_readable_expected_results(string left, string right, string expected)
    {
        Assert.Equal(IngresDate.Parse(expected), IngresDate.Parse(left) + IngresDate.Parse(right));
    }

    [Fact]
    public void Timestamp_addition_observes_declared_DST_context()
    {
        var before = IngresDate.Parse("2026-10-03 12:00:00", "Australia/Melbourne");
        Assert.Equal(IngresDate.Parse("2026-10-04 13:00:00"), before + IngresDate.Parse("1 day"));
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("5 days trailing garbage")]
    [InlineData("1.5 months")]
    public void Parsing_never_guesses_ambiguous_input(string text)
    {
        Assert.Throws<FormatException>(() => IngresDate.Parse(text));
    }

    [Fact]
    public void Unspecified_TimeDate_is_required_at_the_wall_clock_boundary()
    {
        Assert.Throws<ArgumentException>(() => IngresDate.FromTimestamp(DateTime.UtcNow));
    }

    [Fact]
    public void Submillisecond_precision_is_not_silently_truncated()
    {
        Assert.Throws<ArgumentException>(() => IngresDate.FromTimestamp(new DateTime(2026, 1, 1).AddTicks(1)));
    }

    [Fact]
    public void Contradictory_JSON_kind_is_rejected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IngresDate>("{\"kind\":\"date\",\"value\":\"5 days\"}"));
    }
}
