namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// Converts local calendar-day boundaries (as shown in the date pickers) into the
    /// UTC instants Microsoft Graph's createdDateTime filter expects.
    /// </summary>
    public static class DateRangeFilter
    {
        /// <summary>
        /// Returns the UTC 'from'/'to' instants for the inclusive local calendar days
        /// [localFromDate, localToDate]. The Graph filter is exclusive on both ends
        /// (gt/lt), so 'from' is one second before local midnight and 'to' is exactly
        /// local midnight of the day after localToDate.
        /// </summary>
        /// <param name="localFromDate">The first calendar day of the range, as shown in the picker.</param>
        /// <param name="localToDate">The last calendar day of the range, as shown in the picker.</param>
        /// <param name="timeZone">
        /// The time zone the picker dates are expressed in. Defaults to <see cref="TimeZoneInfo.Local"/>
        /// when omitted, which is what production call sites (e.g. Form1) rely on. Tests should pass an
        /// explicit, fixed time zone so the expected values don't depend on the machine running them.
        /// </param>
        /// <remarks>
        /// The offset for each boundary is looked up independently, because the from- and to-boundaries
        /// can fall on opposite sides of a daylight-saving transition and therefore have different offsets.
        /// </remarks>
        public static (DateTimeOffset From, DateTimeOffset To) BuildUtcBoundaries(
            DateTime localFromDate, DateTime localToDate, TimeZoneInfo? timeZone = null)
        {
            var zone = timeZone ?? TimeZoneInfo.Local;

            var fromDate = localFromDate.Date;
            var toDate = localToDate.Date.AddDays(1);

            var from = new DateTimeOffset(fromDate, zone.GetUtcOffset(fromDate)).AddSeconds(-1);
            var to = new DateTimeOffset(toDate, zone.GetUtcOffset(toDate));

            return (from, to);
        }
    }
}
