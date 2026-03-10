using System;
using System.Collections.Generic;
using System.Linq;

namespace Quadono
{
    /// <summary>
    /// 节日类型枚举
    /// </summary>
    public enum HolidayType
    {
        /// <summary>
        /// 国际节日，如劳动节
        /// </summary>
        International,
        
        /// <summary>
        /// 中国节日，如国庆节
        /// </summary>
        Chinese,
        
        /// <summary>
        /// 民间节日，如泼水节
        /// </summary>
        Folk,
        
        /// <summary>
        /// 社区节日，如程序员节
        /// </summary>
        Community
    }

    /// <summary>
    /// 节日数据模型
    /// </summary>
    public class Holiday
    {
        /// <summary>
        /// 节日名称
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 节日英文名称
        /// </summary>
        public string EnglishName { get; set; } = "";
        
        /// <summary>
        /// 节日类型
        /// </summary>
        public HolidayType Type { get; set; }
        
        /// <summary>
        /// 公历月份（1-12）
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// 公历日期（1-31）
        /// </summary>
        public int Day { get; set; }
        
        /// <summary>
        /// 是否为中国农历节日
        /// </summary>
        public bool IsLunar { get; set; }
        
        /// <summary>
        /// 农历月份（如果IsLunar为true）
        /// </summary>
        public int? LunarMonth { get; set; }
        
        /// <summary>
        /// 农历日期（如果IsLunar为true）
        /// </summary>
        public int? LunarDay { get; set; }
        
        /// <summary>
        /// 节日描述
        /// </summary>
        public string Description { get; set; } = "";
        
        /// <summary>
        /// 是否为国家法定节假日
        /// </summary>
        public bool IsPublicHoliday { get; set; }
        
        /// <summary>
        /// 假期天数
        /// </summary>
        public int HolidayDays { get; set; } = 1;
        
        /// <summary>
        /// 是否受时区影响（基于天文计算的节日，如清明节）
        /// 当为true时，如果UTC和北京时间日期不同，会显示两个日期
        /// </summary>
        public bool IsTimeZoneSensitive { get; set; }

        /// <summary>
        /// 获取当前年份的节日日期（北京时间）
        /// </summary>
        public DateTime GetDate(int year)
        {
            // 如果是基于节气的节日（如清明节），使用节气计算
            if (IsSolarTerm)
            {
                return GetSolarTermDate(year);
            }
            
            if (IsLunar)
            {
                // 使用精确的农历算法进行转换
                return LunarCalendar.ToGregorianDate(year, LunarMonth ?? Month, LunarDay ?? Day);
            }
            return new DateTime(year, Month, Day);
        }
        
        /// <summary>
        /// 是否基于二十四节气（如清明节）
        /// </summary>
        public bool IsSolarTerm { get; set; }
        
        /// <summary>
        /// 节气名称（如果IsSolarTerm为true）
        /// </summary>
        public string? SolarTermName { get; set; }
        
        /// <summary>
        /// 获取节气日期
        /// </summary>
        private DateTime GetSolarTermDate(int year)
        {
            if (string.IsNullOrEmpty(SolarTermName))
                return new DateTime(year, Month, Day);
            
            // 查找对应的节气索引
            int termIndex = SolarTermName switch
            {
                "小寒" => 0, "大寒" => 1, "立春" => 2, "雨水" => 3,
                "驚蟄" => 4, "春分" => 5, "清明" => 6, "穀雨" => 7,
                "立夏" => 8, "小滿" => 9, "芒種" => 10, "夏至" => 11,
                "小暑" => 12, "大暑" => 13, "立秋" => 14, "處暑" => 15,
                "白露" => 16, "秋分" => 17, "寒露" => 18, "霜降" => 19,
                "立冬" => 20, "小雪" => 21, "大雪" => 22, "冬至" => 23,
                // 兼容简体中文
                "惊蛰" => 4, "谷雨" => 7, "小满" => 9, "芒种" => 10,
                "处暑" => 15,
                _ => -1
            };
            
            if (termIndex < 0)
                return new DateTime(year, Month, Day);
            
            return LunarCalendar.GetSolarTermDate(year, termIndex);
        }
        
        /// <summary>
        /// 获取当前年份的节日日期（UTC时间）
        /// 仅对受时区影响的节日有意义
        /// </summary>
        public DateTime GetDateUtc(int year)
        {
            // 对于受时区影响的节日，UTC日期可能与北京时间不同
            // 这里简化处理：减去8小时
            var beijingDate = GetDate(year);
            return beijingDate.AddHours(-8);
        }
        
        /// <summary>
        /// 获取格式化的节日日期显示
        /// 对于受时区影响的节日，如果UTC和北京时间日期不同，会同时显示
        /// </summary>
        public string GetFormattedDateDisplay(int year)
        {
            var beijingDate = GetDate(year);
            
            if (!IsTimeZoneSensitive)
            {
                return $"{beijingDate:MM月dd日}";
            }
            
            // 对于受时区影响的节日，检查UTC和北京时间是否日期不同
            var utcDate = GetDateUtc(year);
            
            if (utcDate.Date != beijingDate.Date)
            {
                return $"{beijingDate:MM月dd日}(UTC+8) / {utcDate:MM月dd日}(UTC)";
            }
            
            return $"{beijingDate:MM月dd日}";
        }

        /// <summary>
        /// 获取距离节日的天数
        /// </summary>
        public int DaysUntil(int year)
        {
            var holidayDate = GetDate(year);
            var today = TimeZoneService.BeijingToday;
            
            if (holidayDate < today)
            {
                holidayDate = GetDate(year + 1);
            }
            
            return (holidayDate - today).Days;
        }
        
        /// <summary>
        /// 获取节日类型显示名称
        /// </summary>
        public string GetTypeDisplayName()
        {
            return Type switch
            {
                HolidayType.International => "国际节日",
                HolidayType.Chinese => "中国节日",
                HolidayType.Folk => "民间节日",
                HolidayType.Community => "社区节日",
                _ => "未知类型"
            };
        }
    }

}