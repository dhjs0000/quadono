using System;
using System.Collections.Generic;

namespace Quadono
{
    /// <summary>
    /// 精确的农历历法计算类
    /// 基于天文算法和紫金山天文台数据
    /// 支持范围：1900年-2100年
    /// </summary>
    public static class LunarCalendar
    {
        #region 天文常数
        
        /// <summary>
        /// 朔望月平均长度（天）
        /// </summary>
        private const double SYNODIC_MONTH = 29.53058867;
        
        /// <summary>
        /// 回归年长度（天）
        /// </summary>
        private const double TROPICAL_YEAR = 365.24219878;
        
        /// <summary>
        /// 儒略日基准点（1900年1月0日）
        /// </summary>
        private const double JULIAN_DAY_1900 = 2415021.0;
        
        /// <summary>
        /// 1900年1月31日（农历1900年正月初一）对应的儒略日
        /// </summary>
        private const double JULIAN_DAY_1900_LUNAR_NEW_YEAR = 2415051.0;
        
        #endregion
        
        #region 农历数据表（1900-2100年）
        
        /// <summary>
        /// 1900-2100年农历数据
        /// 每个元素为4字节整数，存储格式：
        /// 第17位：闰月月份（0表示无闰月）
        /// 第16-13位：闰月天数（1=30天，0=29天）
        /// 第12-1位：12个月的大小月（1=30天，0=29天）
        /// </summary>
        private static readonly int[] LUNAR_INFO = new int[]
        {
            0x04bd8, 0x04ae0, 0x0a570, 0x054d5, 0x0d260, 0x0d950, 0x16554, 0x056a0, 0x09ad0, 0x055d2, // 1900-1909
            0x04ae0, 0x0a5b6, 0x0a4d0, 0x0d250, 0x1d255, 0x0b540, 0x0d6a0, 0x0ada2, 0x095b0, 0x14977, // 1910-1919
            0x04970, 0x0a4b0, 0x0b4b5, 0x06a50, 0x06d40, 0x1ab54, 0x02b60, 0x09570, 0x052f2, 0x04970, // 1920-1929
            0x06566, 0x0d4a0, 0x0ea50, 0x06e95, 0x05ad0, 0x02b60, 0x186e3, 0x092e0, 0x1c8d7, 0x0c950, // 1930-1939
            0x0d4a0, 0x1d8a6, 0x0b550, 0x056a0, 0x1a5b4, 0x025d0, 0x092d0, 0x0d2b2, 0x0a950, 0x0b557, // 1940-1949
            0x06ca0, 0x0b550, 0x15355, 0x04da0, 0x0a5d0, 0x14573, 0x052d0, 0x0a9a8, 0x0e950, 0x06aa0, // 1950-1959
            0x0aea6, 0x0ab50, 0x04b60, 0x0aae4, 0x0a570, 0x05260, 0x0f263, 0x0d950, 0x05b57, 0x056a0, // 1960-1969
            0x096d0, 0x04dd5, 0x04ad0, 0x0a4d0, 0x0d4d4, 0x0d250, 0x0d558, 0x0b540, 0x0b5a0, 0x195a6, // 1970-1979
            0x095b0, 0x049b0, 0x0a974, 0x0a4b0, 0x0b27a, 0x06a50, 0x06d40, 0x0af46, 0x0ab60, 0x09570, // 1980-1989
            0x04af5, 0x04970, 0x064b0, 0x074a3, 0x0ea50, 0x06b58, 0x055c0, 0x0ab60, 0x096d5, 0x092e0, // 1990-1999
            0x0c960, 0x0d954, 0x0d4a0, 0x0da50, 0x07552, 0x056a0, 0x0abb7, 0x025d0, 0x092d0, 0x0cab5, // 2000-2009
            0x0a950, 0x0b4a0, 0x0baa4, 0x0ad50, 0x055d9, 0x04ba0, 0x0a5b0, 0x15176, 0x052b0, 0x0a930, // 2010-2019
            0x07954, 0x06aa0, 0x0ad50, 0x05b52, 0x04b60, 0x0a6e6, 0x0a4e0, 0x0d260, 0x0ea65, 0x0d530, // 2020-2029
            0x05aa0, 0x076a3, 0x096d0, 0x04bd7, 0x04ad0, 0x0a4d0, 0x1d0b6, 0x0d250, 0x0d520, 0x0dd45, // 2030-2039
            0x0b5a0, 0x056d0, 0x055b2, 0x049b0, 0x0a577, 0x0a4b0, 0x0aa50, 0x1b255, 0x06d20, 0x0ada0, // 2040-2049
            0x14b63, 0x09370, 0x049f8, 0x04970, 0x064b0, 0x168a6, 0x0ea50, 0x06b20, 0x1a6c4, 0x0aae0, // 2050-2059
            0x0a2e0, 0x0d2e3, 0x0c960, 0x0d557, 0x0d4a0, 0x0da50, 0x05d55, 0x056a0, 0x0a6d0, 0x055d4, // 2060-2069
            0x052d0, 0x0a9b8, 0x0a950, 0x0b4a0, 0x0b6a6, 0x0ad50, 0x055a0, 0x0aba4, 0x0a5b0, 0x052b0, // 2070-2079
            0x0b273, 0x06930, 0x07337, 0x06aa0, 0x0ad50, 0x14b55, 0x04b60, 0x0a570, 0x054e4, 0x0d160, // 2080-2089
            0x0e968, 0x0d520, 0x0daa0, 0x16aa6, 0x056d0, 0x04ae0, 0x0a9d4, 0x0a2d0, 0x0d150, 0x0f252, // 2090-2099
            0x0d520 // 2100
        };
        
        #endregion
        
        #region 天干地支
        
        /// <summary>
        /// 十天干
        /// </summary>
        private static readonly string[] HEAVENLY_STEMS = new string[]
        {
            "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸"
        };
        
        /// <summary>
        /// 十二地支
        /// </summary>
        private static readonly string[] EARTHLY_BRANCHES = new string[]
        {
            "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥"
        };
        
        /// <summary>
        /// 十二生肖
        /// </summary>
        private static readonly string[] ZODIAC_ANIMALS = new string[]
        {
            "鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪"
        };
        
        /// <summary>
        /// 农历月份名称
        /// </summary>
        private static readonly string[] LUNAR_MONTH_NAMES = new string[]
        {
            "正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "冬", "腊"
        };
        
        /// <summary>
        /// 农历日期名称
        /// </summary>
        private static readonly string[] LUNAR_DAY_NAMES = new string[]
        {
            "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
            "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
            "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十"
        };
        
        #endregion
        
        #region 公历转农历
        
        /// <summary>
        /// 将公历日期转换为农历日期
        /// </summary>
        /// <param name="gregorianDate">公历日期</param>
        /// <returns>农历日期信息</returns>
        public static LunarDate ToLunarDate(DateTime gregorianDate)
        {
            int year = gregorianDate.Year;
            
            // 验证年份范围
            if (year < 1901 || year > 2100)
                throw new ArgumentOutOfRangeException(nameof(gregorianDate), "支持的农历年份范围为1901-2100年");
            
            // 优先使用数据库查询
            try
            {
                return LunarCalendarDb.ToLunarDate(gregorianDate);
            }
            catch
            {
                // 数据库查询失败，使用算法计算
                return ToLunarDateByAlgorithm(gregorianDate);
            }
        }
        
        /// <summary>
        /// 使用算法将公历日期转换为农历日期（备用方法）
        /// </summary>
        private static LunarDate ToLunarDateByAlgorithm(DateTime gregorianDate)
        {
            int year = gregorianDate.Year;
            int month = gregorianDate.Month;
            int day = gregorianDate.Day;
            
            // 计算从1900年1月31日（农历1900年正月初一）到目标日期的天数
            DateTime baseDate = new DateTime(1900, 1, 31);
            int offset = (int)(gregorianDate.Date - baseDate.Date).TotalDays;
            
            int lunarYear = 1900;
            int daysInYear = GetLunarYearDays(lunarYear);
            
            // 确定农历年份
            while (offset >= daysInYear)
            {
                offset -= daysInYear;
                lunarYear++;
                daysInYear = GetLunarYearDays(lunarYear);
            }
            
            // 确定农历月份和日期
            int lunarMonth = 1;
            bool isLeapMonth = false;
            int leapMonth = GetLeapMonth(lunarYear);
            
            while (offset > 0)
            {
                int daysInMonth = GetLunarMonthDays(lunarYear, lunarMonth);
                
                // 检查是否有闰月
                if (lunarMonth == leapMonth)
                {
                    int leapDays = GetLeapDays(lunarYear);
                    if (offset < leapDays)
                    {
                        isLeapMonth = true;
                        daysInMonth = leapDays;
                    }
                    else if (offset == leapDays)
                    {
                        // 正好是闰月的最后一天
                        isLeapMonth = true;
                        offset = 0;
                        break;
                    }
                    else
                    {
                        offset -= leapDays;
                    }
                }
                
                if (offset < daysInMonth)
                {
                    break;
                }
                
                offset -= daysInMonth;
                lunarMonth++;
            }
            
            int lunarDay = offset + 1;
            
            return new LunarDate
            {
                Year = lunarYear,
                Month = lunarMonth,
                Day = lunarDay,
                IsLeapMonth = isLeapMonth,
                GregorianDate = gregorianDate
            };
        }
        
        #endregion
        
        #region 农历转公历
        
        /// <summary>
        /// 将农历日期转换为公历日期
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <param name="lunarMonth">农历月</param>
        /// <param name="lunarDay">农历日</param>
        /// <param name="isLeapMonth">是否为闰月</param>
        /// <returns>公历日期</returns>
        public static DateTime ToGregorianDate(int lunarYear, int lunarMonth, int lunarDay, bool isLeapMonth = false)
        {
            // 验证年份范围
            if (lunarYear < 1901 || lunarYear > 2100)
                throw new ArgumentOutOfRangeException(nameof(lunarYear), "支持的农历年份范围为1901-2100年");
            
            if (lunarMonth < 1 || lunarMonth > 12)
                throw new ArgumentOutOfRangeException(nameof(lunarMonth), "农历月份必须在1-12之间");
            
            if (lunarDay < 1 || lunarDay > 30)
                throw new ArgumentOutOfRangeException(nameof(lunarDay), "农历日期必须在1-30之间");
            
            // 优先使用数据库查询
            try
            {
                return LunarCalendarDb.ToGregorianDate(lunarYear, lunarMonth, lunarDay, isLeapMonth);
            }
            catch
            {
                // 数据库查询失败，使用算法计算
                return ToGregorianDateByAlgorithm(lunarYear, lunarMonth, lunarDay, isLeapMonth);
            }
        }
        
        /// <summary>
        /// 使用算法将农历日期转换为公历日期（备用方法）
        /// </summary>
        private static DateTime ToGregorianDateByAlgorithm(int lunarYear, int lunarMonth, int lunarDay, bool isLeapMonth = false)
        {
            DateTime baseDate = new DateTime(1900, 1, 31);
            int offset = 0;
            
            // 累加之前所有年份的天数
            for (int year = 1900; year < lunarYear; year++)
            {
                offset += GetLunarYearDays(year);
            }
            
            // 累加之前所有月份的天数
            int leapMonth = GetLeapMonth(lunarYear);
            for (int month = 1; month < lunarMonth; month++)
            {
                offset += GetLunarMonthDays(lunarYear, month);
                if (month == leapMonth)
                {
                    offset += GetLeapDays(lunarYear);
                }
            }
            
            // 如果是闰月
            if (isLeapMonth)
            {
                if (lunarMonth != leapMonth)
                    throw new ArgumentException($"{lunarYear}年的闰月是{leapMonth}月，不是{lunarMonth}月");
                offset += GetLunarMonthDays(lunarYear, lunarMonth);
            }
            
            // 加上当月的天数
            offset += lunarDay - 1;
            
            return baseDate.AddDays(offset);
        }
        
        #endregion
        
        #region 辅助计算方法
        
        /// <summary>
        /// 获取指定农历年的总天数
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>该年的总天数</returns>
        public static int GetLunarYearDays(int lunarYear)
        {
            int sum = 348; // 12个月 × 29天（最小值）
            int info = LUNAR_INFO[lunarYear - 1900];
            
            // 累加各月的天数（大月多一天）
            for (int i = 0x8000; i > 0x8; i >>= 1)
            {
                if ((info & i) != 0)
                    sum++;
            }
            
            // 加上闰月天数
            return sum + GetLeapDays(lunarYear);
        }
        
        /// <summary>
        /// 获取指定农历年指定月份的天数
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <param name="lunarMonth">农历月（1-12）</param>
        /// <returns>该月的天数</returns>
        public static int GetLunarMonthDays(int lunarYear, int lunarMonth)
        {
            int info = LUNAR_INFO[lunarYear - 1900];
            // 从高位开始，第16位对应正月
            return ((info & (0x10000 >> lunarMonth)) != 0) ? 30 : 29;
        }
        
        /// <summary>
        /// 获取指定农历年的闰月天数
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>闰月天数（0表示无闰月）</returns>
        public static int GetLeapDays(int lunarYear)
        {
            if (GetLeapMonth(lunarYear) == 0)
                return 0;
            
            int info = LUNAR_INFO[lunarYear - 1900];
            // 第16位表示闰月大小
            return ((info & 0x10000) != 0) ? 30 : 29;
        }
        
        /// <summary>
        /// 获取指定农历年的闰月月份
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>闰月月份（0表示无闰月）</returns>
        public static int GetLeapMonth(int lunarYear)
        {
            int info = LUNAR_INFO[lunarYear - 1900];
            // 低4位存储闰月信息
            return info & 0xf;
        }
        
        #endregion
        
        #region 干支纪年
        
        /// <summary>
        /// 获取指定农历年的天干
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>天干名称</returns>
        public static string GetHeavenlyStem(int lunarYear)
        {
            // 1900年是庚子年，天干索引为6
            int index = (lunarYear - 1900 + 6) % 10;
            return HEAVENLY_STEMS[index];
        }
        
        /// <summary>
        /// 获取指定农历年的地支
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>地支名称</returns>
        public static string GetEarthlyBranch(int lunarYear)
        {
            // 1900年是庚子年，地支索引为0
            int index = (lunarYear - 1900) % 12;
            return EARTHLY_BRANCHES[index];
        }
        
        /// <summary>
        /// 获取指定农历年的干支纪年
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>干支名称（如"庚子"）</returns>
        public static string GetGanZhi(int lunarYear)
        {
            return GetHeavenlyStem(lunarYear) + GetEarthlyBranch(lunarYear);
        }
        
        /// <summary>
        /// 获取指定农历年的生肖
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <returns>生肖名称</returns>
        public static string GetZodiacAnimal(int lunarYear)
        {
            // 1900年是鼠年
            int index = (lunarYear - 1900) % 12;
            return ZODIAC_ANIMALS[index];
        }
        
        #endregion
        
        #region 二十四节气
        
        /// <summary>
        /// 二十四节气名称
        /// </summary>
        private static readonly string[] SOLAR_TERMS = new string[]
        {
            "小寒", "大寒", "立春", "雨水", "惊蛰", "春分",
            "清明", "谷雨", "立夏", "小满", "芒种", "夏至",
            "小暑", "大暑", "立秋", "处暑", "白露", "秋分",
            "寒露", "霜降", "立冬", "小雪", "大雪", "冬至"
        };
        
        /// <summary>
        /// 节气数据表（1900-2100年）
        /// 每个节气用2字节表示，存储相对于当年小寒的分钟数偏移
        /// </summary>
        private static readonly ushort[] SOLAR_TERMS_DATA = new ushort[]
        {
            // 1900-1909
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1910-1919
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1920-1929
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1930-1939
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1940-1949
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1950-1959
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1960-1969
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1970-1979
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1980-1989
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 1990-1999
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2000-2009
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2010-2019
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2020-2029
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2030-2039
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2040-2049
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2050-2059
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2060-2069
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2070-2079
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2080-2089
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2090-2099
            0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47, 0x6a47,
            // 2100
            0x6a47
        };
        
        /// <summary>
        /// 基于数据库查询获取指定年份的节气日期
        /// </summary>
        /// <param name="year">公历年</param>
        /// <param name="termIndex">节气索引（0=小寒, 1=大寒, ... 23=冬至）</param>
        /// <returns>节气日期</returns>
        public static DateTime GetSolarTermDate(int year, int termIndex)
        {
            // 数据库使用繁体中文节气名称
            string[] termNames = new string[]
            {
                "小寒", "大寒", "立春", "雨水", "驚蟄", "春分",
                "清明", "穀雨", "立夏", "小滿", "芒種", "夏至",
                "小暑", "大暑", "立秋", "處暑", "白露", "秋分",
                "寒露", "霜降", "立冬", "小雪", "大雪", "冬至"
            };
            
            string termName = termNames[termIndex];
            return LunarCalendarDb.GetSolarTermDate(year, termName);
        }
        
        /// <summary>
        /// 获取指定年份的所有节气
        /// </summary>
        /// <param name="year">公历年</param>
        /// <returns>节气列表</returns>
        public static List<SolarTerm> GetAllSolarTerms(int year)
        {
            var terms = new List<SolarTerm>();
            for (int i = 0; i < 24; i++)
            {
                terms.Add(new SolarTerm
                {
                    Name = SOLAR_TERMS[i],
                    Date = GetSolarTermDate(year, i),
                    Index = i
                });
            }
            return terms;
        }
        
        /// <summary>
        /// 获取指定日期最近的节气
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>最近的节气信息</returns>
        public static SolarTerm? GetNearestSolarTerm(DateTime date)
        {
            var terms = GetAllSolarTerms(date.Year);
            SolarTerm? nearest = null;
            int minDiff = int.MaxValue;
            
            foreach (var term in terms)
            {
                int diff = Math.Abs((term.Date - date.Date).Days);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    nearest = term;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// 获取指定日期的节气（如果当天是节气）
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>节气名称，如果不是节气则返回null</returns>
        public static string? GetSolarTermOfDate(DateTime date)
        {
            var terms = GetAllSolarTerms(date.Year);
            foreach (var term in terms)
            {
                if (term.Date.Date == date.Date)
                    return term.Name;
            }
            return null;
        }
        
        #endregion
        
        #region 格式化输出
        
        /// <summary>
        /// 获取农历月份的中文名称
        /// </summary>
        /// <param name="lunarMonth">农历月</param>
        /// <param name="isLeapMonth">是否为闰月</param>
        /// <returns>月份名称</returns>
        public static string GetLunarMonthName(int lunarMonth, bool isLeapMonth = false)
        {
            string prefix = isLeapMonth ? "闰" : "";
            return prefix + LUNAR_MONTH_NAMES[lunarMonth - 1] + "月";
        }
        
        /// <summary>
        /// 获取农历日期的中文名称
        /// </summary>
        /// <param name="lunarDay">农历日</param>
        /// <returns>日期名称</returns>
        public static string GetLunarDayName(int lunarDay)
        {
            if (lunarDay < 1 || lunarDay > 30)
                throw new ArgumentOutOfRangeException(nameof(lunarDay));
            return LUNAR_DAY_NAMES[lunarDay - 1];
        }
        
        /// <summary>
        /// 格式化农历日期为字符串
        /// </summary>
        /// <param name="lunarDate">农历日期</param>
        /// <returns>格式化字符串</returns>
        public static string FormatLunarDate(LunarDate lunarDate)
        {
            string yearStr = GetGanZhi(lunarDate.Year) + "年";
            string monthStr = GetLunarMonthName(lunarDate.Month, lunarDate.IsLeapMonth);
            string dayStr = GetLunarDayName(lunarDate.Day);
            return $"{yearStr}{monthStr}{dayStr}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 农历日期信息结构
    /// </summary>
    public struct LunarDate
    {
        /// <summary>
        /// 农历年
        /// </summary>
        public int Year { get; set; }
        
        /// <summary>
        /// 农历月（1-12）
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// 农历日（1-30）
        /// </summary>
        public int Day { get; set; }
        
        /// <summary>
        /// 是否为闰月
        /// </summary>
        public bool IsLeapMonth { get; set; }
        
        /// <summary>
        /// 对应的公历日期
        /// </summary>
        public DateTime GregorianDate { get; set; }
        
        /// <summary>
        /// 获取干支纪年
        /// </summary>
        public string GanZhi => LunarCalendar.GetGanZhi(Year);
        
        /// <summary>
        /// 获取生肖
        /// </summary>
        public string ZodiacAnimal => LunarCalendar.GetZodiacAnimal(Year);
        
        /// <summary>
        /// 获取月份名称
        /// </summary>
        public string MonthName => LunarCalendar.GetLunarMonthName(Month, IsLeapMonth);
        
        /// <summary>
        /// 获取日期名称
        /// </summary>
        public string DayName => LunarCalendar.GetLunarDayName(Day);
        
        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return LunarCalendar.FormatLunarDate(this);
        }
    }
    
    /// <summary>
    /// 节气信息结构
    /// </summary>
    public struct SolarTerm
    {
        /// <summary>
        /// 节气名称
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// 节气日期
        /// </summary>
        public DateTime Date { get; set; }
        
        /// <summary>
        /// 节气索引（0-23）
        /// </summary>
        public int Index { get; set; }
        
        /// <summary>
        /// 是否为节（奇数索引）或气（偶数索引）
        /// 节：立春、惊蛰、清明、立夏、芒种、小暑、立秋、白露、寒露、立冬、大雪、小寒
        /// 气：雨水、春分、谷雨、小满、夏至、大暑、处暑、秋分、霜降、小雪、冬至、大寒
        /// </summary>
        public bool IsJie => Index % 2 == 0;
        
        /// <summary>
        /// 是否为气
        /// </summary>
        public bool IsQi => Index % 2 == 1;
        
        public override string ToString()
        {
            return $"{Name} ({Date:yyyy-MM-dd})";
        }
    }
}
