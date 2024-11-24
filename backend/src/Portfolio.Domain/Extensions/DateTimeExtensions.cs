
public static class DateTimeExtensions
{
    /// <summary>
    /// Truncates the DateTime to the nearest second.
    /// </summary>
    /// <param name="dateTime">The DateTime to truncate.</param>
    /// <returns>A new DateTime truncated to the nearest second.</returns>
    public static DateTime TruncateToSecond(this DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second,
            0, // Milliseconds set to 0
            dateTime.Kind); // Preserve the original DateTimeKind (Local, UTC, or Unspecified)
    }
}
