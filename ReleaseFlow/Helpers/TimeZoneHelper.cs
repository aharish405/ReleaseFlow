using System;

namespace ReleaseFlow.Helpers
{
    public static class TimeZoneHelper
    {
        // Indian Standard Time (IST) - UTC+5:30
        private static readonly TimeZoneInfo IstTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        /// <summary>
        /// Gets the current time in IST
        /// </summary>
        public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstTimeZone);

        /// <summary>
        /// Converts UTC time to IST
        /// </summary>
        public static DateTime ToIst(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
            {
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, IstTimeZone);
        }

        /// <summary>
        /// Converts IST time to UTC for database storage
        /// </summary>
        public static DateTime ToUtc(DateTime istTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(istTime, IstTimeZone);
        }

        /// <summary>
        /// Formats datetime for display in IST
        /// </summary>
        public static string FormatForDisplay(DateTime utcTime, string format = "dd-MMM-yyyy hh:mm:ss tt")
        {
            return ToIst(utcTime).ToString(format);
        }
    }
}
