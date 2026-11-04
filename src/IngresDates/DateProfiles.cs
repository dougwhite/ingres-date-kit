namespace IngresDates;
public enum DateProfile
{
    Native,
    DateOnly,
    DateTimeOnly,
    IntervalOnly
}

public static class DateProfiles
{
    public static void Validate(IngresDate value, DateProfile profile)
    {
        bool valid = profile switch
        {
            DateProfile.Native => true,
            DateProfile.DateOnly => value.IsEmpty || value.Kind == IngresDateKind.Date,
            DateProfile.DateTimeOnly => value.IsEmpty || value.IsAbsolute,
            DateProfile.IntervalOnly => value.IsInterval,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException($"Profile {profile} does not accept {value.Kind}.");
    }

    public static DateTime ReadSentinel(IngresDate value, DateTime sentinel)
    {
        Validate(value, DateProfile.DateTimeOnly);
        return value.IsEmpty ? sentinel : value.GetDateTime();
    }

    public static IngresDate WriteSentinel(DateTime value, DateTime sentinel) => value == sentinel ? IngresDate.Empty : IngresDate.FromTimestamp(value);
}
