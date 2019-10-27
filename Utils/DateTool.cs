using System;

namespace DiscordBot.Utils
{
    public static class DateTool
    {
        public static bool IsValidDate(int day, int month, int year)
        {
            //Check the month
            if (month < 1 || month > 12) return false;

            //Check day
            if (day < 1 || day > DateTime.DaysInMonth(year, month)) return false;

            //Valid date
            return true;
        }

        public static string GetPrettyDate(long timestamp)
        {
            //Get default timezone offset
            var offset = GetDefaultTimezone();

            //Create a DateTimeOffset from the timestamp with our timezone
            var date = DateTimeOffset.FromUnixTimeSeconds(timestamp)
                                     .ToOffset(new TimeSpan(offset, 0, 0));

            //Create a nicely formatted string
            return $"{date.Day + Utility.GetOrdinal(date.Day)} of {Utility.GetMonth(date.Month)} {date.Year} at " +
                   $"{Utility.PadNum(date.Hour)}:{Utility.PadNum(date.Minute)} " +
                   $"{Utility.RenderTimezone(date.Offset.Hours)}";
        }

        public static int GetDefaultYear(int day, int month)
        {
            //Get default timezone offset
            var offset = GetDefaultTimezone();

            //Get current date
            var now = DateTimeOffset.Now.ToOffset(new TimeSpan(offset, 0, 0));

            //Check if month is behind
            if (month < now.Month) return now.Year + 1;

            //Check if day is behind
            if ((month == now.Month) && (day < now.Day)) return now.Year + 1;

            //Day is after today, so default to current year
            return now.Year;
        }

        public static int GetDefaultTimezone()
        {
            //TODO: Make GetDefaultTimezone return minutes
            return TzInfo.GetCurrentOffset() / 60;
        }
    }
}
