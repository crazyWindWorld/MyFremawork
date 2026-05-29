using System;
using System.Globalization;

namespace Fuel.Tools
{
    public static class TimeUtil
    {
        /// <summary>
        /// Unix 起始时间 UTC
        /// </summary>
        private static readonly DateTimeOffset UnixEpoch = DateTimeOffset.FromUnixTimeSeconds(0);

        #region 时间戳与 DateTime 转换

        /// <summary>
        /// 秒级时间戳转 DateTime
        /// </summary>
        public static DateTime TimestampSecondsToDateTime(
            long timestampSeconds,
            TimeZoneInfo timeZone = null)
        {
            timeZone ??= TimeZoneInfo.Local;

            var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds);
            return TimeZoneInfo.ConvertTime(dateTimeOffset, timeZone).DateTime;
        }

        /// <summary>
        /// 毫秒级时间戳转 DateTime
        /// </summary>
        public static DateTime TimestampMillisecondsToDateTime(
            long timestampMilliseconds,
            TimeZoneInfo timeZone = null)
        {
            timeZone ??= TimeZoneInfo.Local;

            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(timestampMilliseconds);
            return TimeZoneInfo.ConvertTime(dateTimeOffset, timeZone).DateTime;
        }

        /// <summary>
        /// DateTime 转秒级时间戳
        /// </summary>
        public static long DateTimeToTimestampSeconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        /// <summary>
        /// DateTime 转毫秒级时间戳
        /// </summary>
        public static long DateTimeToTimestampMilliseconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }

        #endregion

        #region 时长格式化

        /// <summary>
        /// 秒数转 HH:mm:ss
        /// 例如：3661 -> 01:01:01
        /// </summary>
        public static string SecondsToHHMMSS(long totalSeconds)
        {
            if (totalSeconds < 0)
                totalSeconds = 0;

            var timeSpan = TimeSpan.FromSeconds(totalSeconds);
            return FormatTimeSpanToHHMMSS(timeSpan);
        }

        /// <summary>
        /// 毫秒数转 HH:mm:ss
        /// 例如：3661000 -> 01:01:01
        /// </summary>
        public static string MillisecondsToHHMMSS(long totalMilliseconds)
        {
            if (totalMilliseconds < 0)
                totalMilliseconds = 0;

            var timeSpan = TimeSpan.FromMilliseconds(totalMilliseconds);
            return FormatTimeSpanToHHMMSS(timeSpan);
        }

        /// <summary>
        /// 秒数转 mm:ss
        /// 例如：125 -> 02:05
        /// </summary>
        public static string SecondsToMMSS(long totalSeconds)
        {
            if (totalSeconds < 0)
                totalSeconds = 0;

            var timeSpan = TimeSpan.FromSeconds(totalSeconds);
            var minutes = (int)timeSpan.TotalMinutes;
            var seconds = timeSpan.Seconds;

            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// 毫秒数转 mm:ss
        /// </summary>
        public static string MillisecondsToMMSS(long totalMilliseconds)
        {
            if (totalMilliseconds < 0)
                totalMilliseconds = 0;

            var timeSpan = TimeSpan.FromMilliseconds(totalMilliseconds);
            var minutes = (int)timeSpan.TotalMinutes;
            var seconds = timeSpan.Seconds;

            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// 秒数转中文友好格式
        /// 例如：
        /// 65 -> 1分5秒
        /// 3661 -> 1小时1分1秒
        /// </summary>
        public static string SecondsToFriendlyText(long totalSeconds)
        {
            if (totalSeconds < 0)
                totalSeconds = 0;

            var timeSpan = TimeSpan.FromSeconds(totalSeconds);

            int days = timeSpan.Days;
            int hours = timeSpan.Hours;
            int minutes = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;

            if (days > 0)
                return $"{days}天{hours}小时{minutes}分{seconds}秒";

            if (hours > 0)
                return $"{hours}小时{minutes}分{seconds}秒";

            if (minutes > 0)
                return $"{minutes}分{seconds}秒";

            return $"{seconds}秒";
        }

        /// <summary>
        /// 毫秒数转中文友好格式
        /// </summary>
        public static string MillisecondsToFriendlyText(long totalMilliseconds)
        {
            if (totalMilliseconds < 0)
                totalMilliseconds = 0;

            return SecondsToFriendlyText(totalMilliseconds / 1000);
        }

        /// <summary>
        /// TimeSpan 转 HH:mm:ss
        /// 小时数允许超过 24
        /// </summary>
        private static string FormatTimeSpanToHHMMSS(TimeSpan timeSpan)
        {
            var totalHours = (int)timeSpan.TotalHours;
            return $"{totalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        #endregion

        #region DateTime 格式化

        /// <summary>
        /// 格式化 DateTime
        /// 默认：yyyy-MM-dd HH:mm:ss
        /// </summary>
        public static string FormatDateTime(
            DateTime dateTime,
            string format = "yyyy-MM-dd HH:mm:ss")
        {
            return dateTime.ToString(format);
        }

        /// <summary>
        /// 秒级时间戳格式化
        /// </summary>
        public static string FormatTimestampSeconds(
            long timestampSeconds,
            string format = "yyyy-MM-dd HH:mm:ss",
            TimeZoneInfo timeZone = null)
        {
            var dateTime = TimestampSecondsToDateTime(timestampSeconds, timeZone);
            return dateTime.ToString(format);
        }

        /// <summary>
        /// 毫秒级时间戳格式化
        /// </summary>
        public static string FormatTimestampMilliseconds(
            long timestampMilliseconds,
            string format = "yyyy-MM-dd HH:mm:ss",
            TimeZoneInfo timeZone = null)
        {
            var dateTime = TimestampMillisecondsToDateTime(timestampMilliseconds, timeZone);
            return dateTime.ToString(format);
        }

        #endregion

        #region 判断同一天 / 同一周 / 同一月

        /// <summary>
        /// 判断两个秒级时间戳是否是同一天
        /// </summary>
        public static bool IsSameDayBySeconds(
            long timestampSeconds1,
            long timestampSeconds2,
            TimeZoneInfo timeZone = null)
        {
            var date1 = TimestampSecondsToDateTime(timestampSeconds1, timeZone);
            var date2 = TimestampSecondsToDateTime(timestampSeconds2, timeZone);

            return IsSameDay(date1, date2);
        }

        /// <summary>
        /// 判断两个毫秒级时间戳是否是同一天
        /// </summary>
        public static bool IsSameDayByMilliseconds(
            long timestampMilliseconds1,
            long timestampMilliseconds2,
            TimeZoneInfo timeZone = null)
        {
            var date1 = TimestampMillisecondsToDateTime(timestampMilliseconds1, timeZone);
            var date2 = TimestampMillisecondsToDateTime(timestampMilliseconds2, timeZone);

            return IsSameDay(date1, date2);
        }

        /// <summary>
        /// 判断两个 DateTime 是否是同一天
        /// </summary>
        public static bool IsSameDay(DateTime date1, DateTime date2)
        {
            return date1.Date == date2.Date;
        }

        /// <summary>
        /// 判断两个秒级时间戳是否是同一周
        /// 默认周一作为一周的开始
        /// </summary>
        public static bool IsSameWeekBySeconds(
            long timestampSeconds1,
            long timestampSeconds2,
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday,
            TimeZoneInfo timeZone = null)
        {
            var date1 = TimestampSecondsToDateTime(timestampSeconds1, timeZone);
            var date2 = TimestampSecondsToDateTime(timestampSeconds2, timeZone);

            return IsSameWeek(date1, date2, firstDayOfWeek);
        }

        /// <summary>
        /// 判断两个毫秒级时间戳是否是同一周
        /// 默认周一作为一周的开始
        /// </summary>
        public static bool IsSameWeekByMilliseconds(
            long timestampMilliseconds1,
            long timestampMilliseconds2,
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday,
            TimeZoneInfo timeZone = null)
        {
            var date1 = TimestampMillisecondsToDateTime(timestampMilliseconds1, timeZone);
            var date2 = TimestampMillisecondsToDateTime(timestampMilliseconds2, timeZone);

            return IsSameWeek(date1, date2, firstDayOfWeek);
        }

        /// <summary>
        /// 判断两个 DateTime 是否是同一周
        /// 默认周一作为一周的开始
        /// </summary>
        public static bool IsSameWeek(
            DateTime date1,
            DateTime date2,
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
        {
            var startOfWeek1 = GetStartOfWeek(date1, firstDayOfWeek);
            var startOfWeek2 = GetStartOfWeek(date2, firstDayOfWeek);

            return startOfWeek1 == startOfWeek2;
        }

        /// <summary>
        /// 获取某个日期所在周的开始日期
        /// </summary>
        public static DateTime GetStartOfWeek(
            DateTime dateTime,
            DayOfWeek firstDayOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + dateTime.DayOfWeek - firstDayOfWeek) % 7;
            return dateTime.Date.AddDays(-diff);
        }

        /// <summary>
        /// 判断两个秒级时间戳是否是同一月
        /// </summary>
        public static bool IsSameMonthBySeconds(
            long timestampSeconds1,
            long timestampSeconds2,
            TimeZoneInfo timeZone = null)
        {
            var date1 = TimestampSecondsToDateTime(timestampSeconds1, timeZone);
            var date2 = TimestampSecondsToDateTime(timestampSeconds2, timeZone);

            return IsSameMonth(date1, date2);
        }

        /// <summary>
        /// 判断两个毫秒级时间戳是否是同一月
        /// </summary>
        public static bool IsSameMonthByMilliseconds(
            long timestampMilliseconds1,
            long timestampMilliseconds2,
            TimeZoneInfo timeZone = null)
        {
            var date1 = TimestampMillisecondsToDateTime(timestampMilliseconds1, timeZone);
            var date2 = TimestampMillisecondsToDateTime(timestampMilliseconds2, timeZone);

            return IsSameMonth(date1, date2);
        }

        /// <summary>
        /// 判断两个 DateTime 是否是同一月
        /// </summary>
        public static bool IsSameMonth(DateTime date1, DateTime date2)
        {
            return date1.Year == date2.Year &&
                   date1.Month == date2.Month;
        }

        #endregion

        #region 常用时间边界

        /// <summary>
        /// 获取某天开始时间 00:00:00
        /// </summary>
        public static DateTime GetStartOfDay(DateTime dateTime)
        {
            return dateTime.Date;
        }

        /// <summary>
        /// 获取某天结束时间 23:59:59.999
        /// </summary>
        public static DateTime GetEndOfDay(DateTime dateTime)
        {
            return dateTime.Date.AddDays(1).AddMilliseconds(-1);
        }

        /// <summary>
        /// 获取某月开始时间
        /// </summary>
        public static DateTime GetStartOfMonth(DateTime dateTime)
        {
            return new DateTime(dateTime.Year, dateTime.Month, 1);
        }

        /// <summary>
        /// 获取某月结束时间
        /// </summary>
        public static DateTime GetEndOfMonth(DateTime dateTime)
        {
            return GetStartOfMonth(dateTime).AddMonths(1).AddMilliseconds(-1);
        }

        #endregion
    }
}
