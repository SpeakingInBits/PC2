namespace PC2.Models;

/// <summary>
/// Calculates the dates of an <see cref="EventSeries"/>
/// </summary>
public static class EventRecurrence
{
    /// <summary>
    /// How many months ahead events are created for a series with no end.
    /// Matches how far ahead the public calendar can be browsed
    /// </summary>
    public const int OpenEndedHorizonMonths = 12;

    private static readonly string[] WeekOfMonthNames = { "first", "second", "third", "fourth" };

    /// <summary>
    /// Gets the last day events should be created through for a series
    /// </summary>
    /// <param name="series">The series being generated</param>
    /// <param name="today">The current date</param>
    public static DateOnly GetGenerationLimit(EventSeries series, DateOnly today)
    {
        if (series.EndDate.HasValue)
        {
            return series.EndDate.Value;
        }

        // Count based series stop on their own once the count is reached
        if (series.OccurrenceCount.HasValue)
        {
            return DateOnly.MaxValue;
        }

        return today.AddMonths(OpenEndedHorizonMonths);
    }

    /// <summary>
    /// Gets the dates of a series that fall between <paramref name="from"/> and <paramref name="through"/>, inclusive.
    /// The series' <see cref="EventSeries.EndDate"/> and <see cref="EventSeries.OccurrenceCount"/> are applied,
    /// with the count measured from <see cref="EventSeries.StartDate"/>.
    /// A <see cref="RecurrenceFrequency.SpecificDates"/> series has no pattern, so no dates are returned
    /// </summary>
    public static IEnumerable<DateOnly> GetDates(EventSeries series, DateOnly from, DateOnly through)
    {
        if (series.EndDate.HasValue && series.EndDate.Value < through)
        {
            through = series.EndDate.Value;
        }

        int remaining = series.OccurrenceCount ?? int.MaxValue;

        foreach (DateOnly date in GetPatternDates(series, through))
        {
            if (date > through || remaining <= 0)
            {
                yield break;
            }

            remaining--;

            if (date >= from)
            {
                yield return date;
            }
        }
    }

    /// <summary>
    /// Creates an event for one date of the series, copying the series details
    /// </summary>
    public static CalendarEvent CreateOccurrence(EventSeries series, DateOnly date)
    {
        return new CalendarEvent
        {
            DateOfEvent = date,
            StartingTime = series.StartingTime,
            EndingTime = series.EndingTime,
            EventDescription = series.EventDescription,
            PC2Event = series.PC2Event,
            CountyEvent = series.CountyEvent,
            EventSeries = series
        };
    }

    /// <summary>
    /// Describes how often the series repeats, e.g. "Every 2 weeks on Tuesday and Thursday"
    /// </summary>
    public static string Describe(EventSeries series)
    {
        string description = series.Frequency switch
        {
            RecurrenceFrequency.Weekly =>
                $"{Every(series.Interval, "week")} on {JoinDays(GetWeeklyDays(series))}",
            RecurrenceFrequency.MonthlyByWeekday =>
                $"{Every(series.Interval, "month")} on the {DescribeWeekOfMonth(series.WeekOfMonth)} {series.StartDate.DayOfWeek}",
            RecurrenceFrequency.MonthlyByDate =>
                $"{Every(series.Interval, "month")} on day {series.StartDate.Day}",
            _ => "On specific dates"
        };

        if (series.Frequency == RecurrenceFrequency.SpecificDates)
        {
            return description;
        }

        if (series.EndDate.HasValue)
        {
            return $"{description}, until {series.EndDate.Value:MMMM d, yyyy}";
        }

        if (series.OccurrenceCount.HasValue)
        {
            return $"{description}, {series.OccurrenceCount} times";
        }

        return description;
    }

    /// <summary>
    /// Gets the week of the month a date falls in, counting from the first of that weekday:
    /// 1 through 4, or 5 if it is the fifth occurrence of its weekday
    /// </summary>
    public static int GetWeekOfMonth(DateOnly date)
    {
        return (date.Day - 1) / 7 + 1;
    }

    /// <summary>
    /// Gets the <see cref="EventSeries.WeekOfMonth"/> matching a date. A fifth weekday is treated as the last,
    /// since most months do not have one
    /// </summary>
    public static int DefaultWeekOfMonth(DateOnly date)
    {
        int weekOfMonth = GetWeekOfMonth(date);
        return weekOfMonth > 4 ? EventSeries.LastWeekOfMonth : weekOfMonth;
    }

    /// <summary>
    /// True if the date is the last occurrence of its weekday in its month
    /// </summary>
    public static bool IsLastWeekdayOfMonth(DateOnly date)
    {
        return date.AddDays(7).Month != date.Month;
    }

    /// <summary>
    /// Enumerates the dates of the pattern, in order, from the series start date through <paramref name="through"/>
    /// </summary>
    private static IEnumerable<DateOnly> GetPatternDates(EventSeries series, DateOnly through)
    {
        int interval = Math.Max(1, series.Interval);

        switch (series.Frequency)
        {
            case RecurrenceFrequency.Weekly:
            {
                RecurrenceDays days = series.WeeklyDays == RecurrenceDays.None
                    ? ToRecurrenceDay(series.StartDate.DayOfWeek)
                    : series.WeeklyDays;

                // Weeks are counted from the Sunday of the week the series starts in
                DateOnly weekStart = series.StartDate.AddDays(-(int)series.StartDate.DayOfWeek);
                while (weekStart <= through)
                {
                    for (int day = 0; day < 7; day++)
                    {
                        DateOnly date = weekStart.AddDays(day);
                        if (date >= series.StartDate && days.HasFlag(ToRecurrenceDay(date.DayOfWeek)))
                        {
                            yield return date;
                        }
                    }

                    weekStart = weekStart.AddDays(7 * interval);
                }

                yield break;
            }

            case RecurrenceFrequency.MonthlyByWeekday:
            {
                int weekOfMonth = series.WeekOfMonth ?? DefaultWeekOfMonth(series.StartDate);
                DateOnly month = new(series.StartDate.Year, series.StartDate.Month, 1);
                while (month <= through)
                {
                    DateOnly? date = GetWeekdayOfMonth(month, series.StartDate.DayOfWeek, weekOfMonth);
                    if (date.HasValue && date.Value >= series.StartDate)
                    {
                        yield return date.Value;
                    }

                    month = month.AddMonths(interval);
                }

                yield break;
            }

            case RecurrenceFrequency.MonthlyByDate:
            {
                DateOnly month = new(series.StartDate.Year, series.StartDate.Month, 1);
                while (month <= through)
                {
                    int day = Math.Min(series.StartDate.Day, DateTime.DaysInMonth(month.Year, month.Month));
                    yield return new DateOnly(month.Year, month.Month, day);

                    month = month.AddMonths(interval);
                }

                yield break;
            }

            default:
                yield break;
        }
    }

    /// <summary>
    /// Gets the Nth <paramref name="dayOfWeek"/> of the month, or the last one if
    /// <paramref name="weekOfMonth"/> is <see cref="EventSeries.LastWeekOfMonth"/>.
    /// Returns null if the month does not have that many of the weekday
    /// </summary>
    private static DateOnly? GetWeekdayOfMonth(DateOnly firstOfMonth, DayOfWeek dayOfWeek, int weekOfMonth)
    {
        if (weekOfMonth == EventSeries.LastWeekOfMonth)
        {
            DateOnly lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
            int daysBack = ((int)lastOfMonth.DayOfWeek - (int)dayOfWeek + 7) % 7;
            return lastOfMonth.AddDays(-daysBack);
        }

        int daysForward = ((int)dayOfWeek - (int)firstOfMonth.DayOfWeek + 7) % 7;
        DateOnly date = firstOfMonth.AddDays(daysForward + 7 * (weekOfMonth - 1));
        return date.Month == firstOfMonth.Month ? date : null;
    }

    private static RecurrenceDays ToRecurrenceDay(DayOfWeek dayOfWeek)
    {
        return (RecurrenceDays)(1 << (int)dayOfWeek);
    }

    private static List<DayOfWeek> GetWeeklyDays(EventSeries series)
    {
        if (series.WeeklyDays == RecurrenceDays.None)
        {
            return new List<DayOfWeek> { series.StartDate.DayOfWeek };
        }

        return Enum.GetValues<DayOfWeek>()
            .Where(day => series.WeeklyDays.HasFlag(ToRecurrenceDay(day)))
            .ToList();
    }

    private static string Every(int interval, string unit)
    {
        return interval <= 1 ? $"{char.ToUpper(unit[0])}{unit[1..]}ly" : $"Every {interval} {unit}s";
    }

    private static string DescribeWeekOfMonth(int? weekOfMonth)
    {
        if (weekOfMonth is >= 1 and <= 4)
        {
            return WeekOfMonthNames[weekOfMonth.Value - 1];
        }

        return "last";
    }

    private static string JoinDays(List<DayOfWeek> days)
    {
        List<string> names = days.Select(day => day.ToString()).ToList();
        if (names.Count == 1)
        {
            return names[0];
        }

        return $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";
    }
}
