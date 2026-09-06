using System;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class DateRangeFilterTests
{
    // A fixed, non-DST-observing zone (UTC+3) so these regression tests are deterministic
    // regardless of the machine/CI runner's local time zone. A UTC-configured CI runner would
    // make the old buggy code (which stamped dates as UTC+0) and the fixed code produce
    // identical results if the tests relied on TimeZoneInfo.Local, silently losing coverage.
    private static readonly TimeZoneInfo FixedNonDstZone = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");

    [Fact]
    public void BuildUtcBoundaries_SameDayRange_MatchesLocalMidnightToMidnightConvertedToUtc()
    {
        var date = new DateTime(2026, 3, 1);
        var offset = FixedNonDstZone.GetUtcOffset(date);

        var (utcFrom, utcTo) = DateRangeFilter.BuildUtcBoundaries(date, date, FixedNonDstZone);

        Assert.Equal(new DateTimeOffset(date, offset).AddSeconds(-1), utcFrom);
        Assert.Equal(new DateTimeOffset(date.AddDays(1), offset), utcTo);
    }

    [Fact]
    public void BuildUtcBoundaries_MultiDayRange_WidensToFullyIncludeBothDays()
    {
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 5);
        var fromOffset = FixedNonDstZone.GetUtcOffset(from);
        var toOffset = FixedNonDstZone.GetUtcOffset(to.AddDays(1));

        var (utcFrom, utcTo) = DateRangeFilter.BuildUtcBoundaries(from, to, FixedNonDstZone);

        Assert.Equal(new DateTimeOffset(from, fromOffset).AddSeconds(-1), utcFrom);
        Assert.Equal(new DateTimeOffset(to.AddDays(1), toOffset), utcTo);
    }

    [Fact]
    public void BuildUtcBoundaries_ResultIsExclusiveRangeCoveringWholeLocalDays()
    {
        var date = new DateTime(2026, 6, 15);

        var (utcFrom, utcTo) = DateRangeFilter.BuildUtcBoundaries(date, date, FixedNonDstZone);

        Assert.True(utcTo - utcFrom > TimeSpan.FromDays(1));
        Assert.True(utcTo - utcFrom < TimeSpan.FromDays(1) + TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void BuildUtcBoundaries_RangeCrossingDstSpringForward_UsesDifferentOffsetsPerBoundary()
    {
        // US spring-forward for 2026 is March 8th (clocks jump from 2:00 AM to 3:00 AM CST -> CDT).
        // A range starting before and ending after that date must use CST (UTC-6) for the 'from'
        // boundary and CDT (UTC-5) for the 'to' boundary, since the offset is looked up per-instant.
        var centralZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        var from = new DateTime(2026, 3, 6);
        var to = new DateTime(2026, 3, 9);

        var fromOffset = centralZone.GetUtcOffset(from);
        var toOffset = centralZone.GetUtcOffset(to.AddDays(1));

        var (utcFrom, utcTo) = DateRangeFilter.BuildUtcBoundaries(from, to, centralZone);

        Assert.Equal(TimeSpan.FromHours(-6), fromOffset);
        Assert.Equal(TimeSpan.FromHours(-5), toOffset);
        Assert.Equal(new DateTimeOffset(from, fromOffset).AddSeconds(-1), utcFrom);
        Assert.Equal(new DateTimeOffset(to.AddDays(1), toOffset), utcTo);
        Assert.Equal(fromOffset, utcFrom.Offset);
        Assert.Equal(toOffset, utcTo.Offset);
    }
}
