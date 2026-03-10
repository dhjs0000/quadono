using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Quadono
{
    /// <summary>
    /// 农历数据库模型
    /// </summary>
    public class LunarDayEntry
    {
        public string Day { get; set; } = "";
        public GregorianDate Gregorian { get; set; } = new();
        public LunarDateInfo Lunar { get; set; } = new();
        public string Zodiac { get; set; } = "";
        public string? SolarTerm { get; set; }
    }

    public class GregorianDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Date { get; set; }
    }

    public class LunarDateInfo
    {
        public string Year { get; set; } = "";
        public string Month { get; set; } = "";
        public string Date { get; set; } = "";
        public bool LeapMonth { get; set; }
    }

    /// <summary>
    /// 农历数据库服务
    /// 从JSON文件读取精确的农历数据（1901-2100年）
    /// </summary>
    public static class LunarCalendarDb
    {
        private static readonly string DbPath = GetDbPath();
        private static Dictionary<int, List<LunarDayEntry>>? _cache;
        
        /// <summary>
        /// 获取数据库路径
        /// 优先从项目根目录查找，如果不存在则从执行目录查找
        /// </summary>
        private static string GetDbPath()
        {
            // 尝试从当前工作目录查找（dotnet run 时）
            string cwdDbPath = Path.Combine(Directory.GetCurrentDirectory(), "lunardb");
            if (Directory.Exists(cwdDbPath))
                return cwdDbPath;
            
            // 尝试从执行目录向上查找（发布后运行时）
            string execDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                string testPath = Path.Combine(execDir, "lunardb");
                if (Directory.Exists(testPath))
                    return testPath;
                
                var parent = Directory.GetParent(execDir);
                if (parent == null) break;
                execDir = parent.FullName;
            }
            
            // 默认返回当前工作目录下的路径
            return Path.Combine(Directory.GetCurrentDirectory(), "lunardb");
        }

        /// <summary>
        /// 获取指定年份的所有农历数据
        /// </summary>
        /// <param name="year">公历年</param>
        /// <returns>该年的农历数据列表</returns>
        public static List<LunarDayEntry> GetYearData(int year)
        {
            if (_cache?.ContainsKey(year) == true)
                return _cache[year];

            string filePath = Path.Combine(DbPath, $"{year}.json");
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"找不到农历数据文件: {filePath}");

            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true  // 忽略属性名大小写
            };
            var data = JsonSerializer.Deserialize<List<LunarDayEntry>>(json, options);
            
            if (data == null)
                throw new InvalidDataException($"无法解析农历数据: {filePath}");

            // 缓存数据
            _cache ??= new Dictionary<int, List<LunarDayEntry>>();
            _cache[year] = data;

            return data;
        }

        /// <summary>
        /// 将公历日期转换为农历日期
        /// </summary>
        /// <param name="gregorianDate">公历日期</param>
        public static LunarDate ToLunarDate(DateTime gregorianDate)
        {
            var yearData = GetYearData(gregorianDate.Year);
            var entry = yearData.FirstOrDefault(d => 
                d.Gregorian.Month == gregorianDate.Month && 
                d.Gregorian.Date == gregorianDate.Day);

            if (entry == null)
                throw new InvalidOperationException($"找不到日期 {gregorianDate:yyyy-MM-dd} 的农历数据");

            // 解析农历年份数字
            int lunarYear = ParseLunarYear(entry.Lunar.Year);
            int lunarMonth = ParseLunarMonth(entry.Lunar.Month);
            int lunarDay = ParseLunarDay(entry.Lunar.Date);

            return new LunarDate
            {
                Year = lunarYear,
                Month = lunarMonth,
                Day = lunarDay,
                IsLeapMonth = entry.Lunar.LeapMonth,
                GregorianDate = gregorianDate
            };
        }

        /// <summary>
        /// 将农历日期转换为公历日期
        /// </summary>
        /// <param name="lunarYear">农历年</param>
        /// <param name="lunarMonth">农历月</param>
        /// <param name="lunarDay">农历日</param>
        /// <param name="isLeapMonth">是否为闰月</param>
        public static DateTime ToGregorianDate(int lunarYear, int lunarMonth, int lunarDay, bool isLeapMonth = false)
        {
            // 尝试在当前年份和前后年份查找
            for (int year = lunarYear - 1; year <= lunarYear + 1; year++)
            {
                try
                {
                    var yearData = GetYearData(year);
                    var entry = yearData.FirstOrDefault(d =>
                    {
                        int entryMonth = ParseLunarMonth(d.Lunar.Month);
                        int entryDay = ParseLunarDay(d.Lunar.Date);
                        int entryYear = ParseLunarYear(d.Lunar.Year);
                        
                        return entryYear == lunarYear &&
                               entryMonth == lunarMonth &&
                               entryDay == lunarDay &&
                               d.Lunar.LeapMonth == isLeapMonth;
                    });

                    if (entry != null)
                        return new DateTime(entry.Gregorian.Year, entry.Gregorian.Month, entry.Gregorian.Date);
                }
                catch
                {
                    continue;
                }
            }

            throw new InvalidOperationException($"找不到农历 {lunarYear}年{lunarMonth}月{lunarDay}日 对应的公历日期");
        }

        /// <summary>
        /// 获取指定年份的节气日期
        /// </summary>
        /// <param name="year">公历年</param>
        /// <param name="termName">节气名称（支持简体中文和繁体中文）</param>
        /// <returns>节气日期</returns>
        public static DateTime GetSolarTermDate(int year, string termName)
        {
            var yearData = GetYearData(year);
            
            // 将简体中文转换为繁体中文进行查询
            string traditionalName = ToTraditionalChinese(termName);
            
            var entry = yearData.FirstOrDefault(d => 
                d.SolarTerm == traditionalName);

            if (entry == null)
                throw new InvalidOperationException($"找不到 {year} 年的 {termName} 节气数据");

            return new DateTime(entry.Gregorian.Year, entry.Gregorian.Month, entry.Gregorian.Date);
        }
        
        /// <summary>
        /// 将简体中文节气名称转换为繁体中文
        /// </summary>
        private static string ToTraditionalChinese(string simplifiedName)
        {
            return simplifiedName switch
            {
                "惊蛰" => "驚蟄",
                "谷雨" => "穀雨",
                "小满" => "小滿",
                "芒种" => "芒種",
                "处暑" => "處暑",
                _ => simplifiedName  // 其他名称繁简相同
            };
        }

        /// <summary>
        /// 获取指定年份的所有节气
        /// </summary>
        /// <param name="year">公历年</param>
        /// <returns>节气列表</returns>
        public static List<(string Name, DateTime Date)> GetAllSolarTerms(int year)
        {
            var yearData = GetYearData(year);
            return yearData
                .Where(d => !string.IsNullOrEmpty(d.SolarTerm))
                .Select(d => (d.SolarTerm!, new DateTime(d.Gregorian.Year, d.Gregorian.Month, d.Gregorian.Date)))
                .ToList();
        }

        /// <summary>
        /// 获取指定日期的节气（如果当天是节气）
        /// </summary>
        /// <param name="date">日期</param>
        /// <returns>节气名称，如果不是节气则返回null</returns>
        public static string? GetSolarTermOfDate(DateTime date)
        {
            var yearData = GetYearData(date.Year);
            var entry = yearData.FirstOrDefault(d =>
                d.Gregorian.Month == date.Month &&
                d.Gregorian.Date == date.Day &&
                !string.IsNullOrEmpty(d.SolarTerm));

            return entry?.SolarTerm;
        }

        #region 辅助方法

        /// <summary>
        /// 解析农历年份（如"丙午" -> 2026）
        /// </summary>
        private static int ParseLunarYear(string lunarYear)
        {
            // 干支纪年到公历年份的转换
            // 1900年是庚子年
            string[] stems = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };
            string[] branches = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

            if (lunarYear.Length < 2) return 1900;

            string stem = lunarYear[0].ToString();
            string branch = lunarYear[1].ToString();

            int stemIndex = Array.IndexOf(stems, stem);
            int branchIndex = Array.IndexOf(branches, branch);

            if (stemIndex < 0 || branchIndex < 0) return 1900;

            // 计算年份
            for (int year = 1900; year <= 2100; year++)
            {
                int s = (year - 1900 + 6) % 10; // 1900年天干索引为6（庚）
                int b = (year - 1900) % 12;      // 1900年地支索引为0（子）
                
                if (s == stemIndex && b == branchIndex)
                    return year;
            }

            return 1900;
        }

        /// <summary>
        /// 解析农历月份（如"二月" -> 2）
        /// </summary>
        private static int ParseLunarMonth(string lunarMonth)
        {
            string[] months = { "正", "二", "三", "四", "五", "六", "七", "八", "九", "十", "冬", "腊" };
            
            for (int i = 0; i < months.Length; i++)
            {
                if (lunarMonth.Contains(months[i]))
                    return i + 1;
            }

            // 尝试直接解析数字
            if (int.TryParse(lunarMonth.Replace("月", ""), out int month))
                return month;

            return 1;
        }

        /// <summary>
        /// 解析农历日期（如"十八" -> 18）
        /// </summary>
        private static int ParseLunarDay(string lunarDay)
        {
            string[] days = { "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
                             "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
                             "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十" };

            for (int i = 0; i < days.Length; i++)
            {
                if (lunarDay == days[i])
                    return i + 1;
            }

            // 尝试直接解析数字
            if (int.TryParse(lunarDay.Replace("日", ""), out int day))
                return day;

            return 1;
        }

        #endregion
    }
}
