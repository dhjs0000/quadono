using System;

namespace Quadono
{
    /// <summary>
    /// 时区服务类
    /// 提供UTC和北京时间(UTC+8)的显示支持
    /// </summary>
    public static class TimeZoneService
    {
        /// <summary>
        /// 中国标准时间(UTC+8)时区
        /// </summary>
        private static readonly TimeZoneInfo ChinaStandardTime = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time");
        
        /// <summary>
        /// 获取当前UTC时间
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;
        
        /// <summary>
        /// 获取当前北京时间(UTC+8)
        /// </summary>
        public static DateTime BeijingNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaStandardTime);
        
        /// <summary>
        /// 获取当前UTC日期
        /// </summary>
        public static DateTime UtcToday => DateTime.UtcNow.Date;
        
        /// <summary>
        /// 获取当前北京日期(UTC+8)
        /// </summary>
        public static DateTime BeijingToday => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaStandardTime).Date;
        
        /// <summary>
        /// 获取当前年份(UTC+8)
        /// </summary>
        public static int CurrentYear => BeijingNow.Year;
        
        /// <summary>
        /// 将UTC时间转换为北京时间
        /// </summary>
        /// <param name="utcTime">UTC时间</param>
        /// <returns>北京时间</returns>
        public static DateTime ToBeijingTime(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Local)
                utcTime = utcTime.ToUniversalTime();
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, ChinaStandardTime);
        }
        
        /// <summary>
        /// 将北京时间转换为UTC时间
        /// </summary>
        /// <param name="beijingTime">北京时间</param>
        /// <returns>UTC时间</returns>
        public static DateTime ToUtcTime(DateTime beijingTime)
        {
            return TimeZoneInfo.ConvertTimeToUtc(beijingTime, ChinaStandardTime);
        }
        
        /// <summary>
        /// 获取格式化的当前时间字符串
        /// 当UTC和北京时间日期不同时，同时显示两个时间
        /// </summary>
        /// <returns>格式化的时间字符串</returns>
        public static string GetCurrentTimeDisplay()
        {
            var utc = UtcNow;
            var beijing = BeijingNow;
            
            // 如果日期相同，只显示北京时间
            if (utc.Date == beijing.Date)
            {
                return $"北京时间: {beijing:yyyy-MM-dd HH:mm:ss} (UTC+8)";
            }
            
            // 如果日期不同，同时显示两个时间
            return $"UTC: {utc:yyyy-MM-dd HH:mm:ss} | 北京时间: {beijing:yyyy-MM-dd HH:mm:ss} (UTC+8)";
        }
        
        /// <summary>
        /// 获取格式化的当前日期字符串
        /// 当UTC和北京时间日期不同时，同时显示两个日期
        /// </summary>
        /// <returns>格式化的日期字符串</returns>
        public static string GetCurrentDateDisplay()
        {
            var utc = UtcToday;
            var beijing = BeijingToday;
            
            // 如果日期相同，只显示北京时间
            if (utc == beijing)
            {
                return $"今天: {beijing:yyyy-MM-dd} (UTC+8)";
            }
            
            // 如果日期不同，同时显示两个日期
            return $"UTC日期: {utc:yyyy-MM-dd} | 北京日期: {beijing:yyyy-MM-dd} (UTC+8)";
        }
        
        /// <summary>
        /// 获取当前时间信息（包含农历）
        /// </summary>
        /// <returns>完整的时间信息字符串</returns>
        public static string GetFullTimeInfo()
        {
            var beijing = BeijingNow;
            var lunarDate = LunarCalendar.ToLunarDate(beijing);
            var solarTerm = LunarCalendar.GetSolarTermOfDate(beijing);
            
            var result = $"当前时间: {beijing:yyyy-MM-dd HH:mm:ss} (UTC+8)\n";
            result += $"农历: {lunarDate}\n";
            
            if (!string.IsNullOrEmpty(solarTerm))
            {
                result += $"节气: {solarTerm}\n";
            }
            
            // 如果UTC日期不同，添加UTC信息
            var utc = UtcNow;
            if (utc.Date != beijing.Date)
            {
                result += $"UTC时间: {utc:yyyy-MM-dd HH:mm:ss}\n";
            }
            
            return result.TrimEnd();
        }
    }
}
