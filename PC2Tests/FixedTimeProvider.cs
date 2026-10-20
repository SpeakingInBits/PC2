namespace PC2Tests;

/// <summary>
/// A clock stopped at a set UTC time
/// </summary>
internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public FixedTimeProvider(string utcNow) : this(DateTimeOffset.Parse(utcNow)) { }

    public override DateTimeOffset GetUtcNow() => utcNow;
}
