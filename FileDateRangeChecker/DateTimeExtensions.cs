using System;
using System.Collections.Generic;
using System.Globalization;

namespace FileDateRangeChecker
{
    /// <summary>
    /// Extension methods for DateTime manipulation.
    /// </summary>
    internal static class DateTimeExtensions
    {
        /// <summary>
        /// Parse a date from the ISO 8601 YYYY-MM-DD format.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>A DateTime with zero time component.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is null.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not a valid ISO 8601 formatted date.</exception>
        public static DateTime ParseIso8601Date(this string s)
        {
            if (s == null) { throw new ArgumentNullException(nameof(s)); }

            return DateTime.ParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert a range of dates, given by a start and end date, into all of the dates in that range.
        /// </summary>
        /// <param name="range">The range of dates.</param>
        /// <returns>All of the dates within the given range, with zero time component.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the start date of the range is later than the end date.</exception>
        public static IEnumerable<DateTime> RangeToDates(this (DateTime start, DateTime end) range)
        {
            DateTime start = range.start.Date;
            DateTime end = range.end.Date;

            if (start > end) { throw new ArgumentOutOfRangeException(nameof(range), "Range start date cannot be later than the end date."); }

            for (DateTime dt = start; dt <= end; dt = dt.AddDays(1)) { yield return dt; }
        }

        /// <summary>
        /// Convert an ordered set of dates into ranges of sequential dates, each given by a start and end date.
        /// </summary>
        /// <param name="dates">The dates to convert, in ascending order.</param>
        /// <returns>Ranges of sequential dates, given by a start and end date, with zero time component.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dates"/> is null.</exception>
        public static IEnumerable<(DateTime start, DateTime end)> DatesToRanges(this IEnumerable<DateTime> dates)
        {
            if (dates == null) { throw new ArgumentNullException(nameof(dates)); }

            using (IEnumerator<DateTime> datesEnumerator = dates.GetEnumerator())
            {
                if (!datesEnumerator.MoveNext()) { yield break; }

                DateTime start = datesEnumerator.Current.Date;
                DateTime end = datesEnumerator.Current.Date;
                while (datesEnumerator.MoveNext())
                {
                    DateTime current = datesEnumerator.Current.Date;
                    if (current == end.AddDays(1))
                    {
                        end = current;
                    }
                    else
                    {
                        yield return (start, end);
                        start = end = current;
                    }
                }

                yield return (start, end);
            }
        }
    }
}
