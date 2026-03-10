using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Quadono
{
    /// <summary>
    /// 节日服务类
    /// </summary>
    public static class HolidayService
    {
        private static readonly List<Holiday> _holidays = InitializeHolidays();
        private static readonly string _customHolidaysFile = "custom_holidays.json";
        
        /// <summary>
        /// 获取所有节日
        /// </summary>
        public static List<Holiday> GetAllHolidays()
        {
            var customHolidays = LoadCustomHolidays();
            return _holidays.Concat(customHolidays).ToList();
        }
        
        /// <summary>
        /// 按类型获取节日
        /// </summary>
        public static List<Holiday> GetHolidaysByType(HolidayType type)
        {
            return GetAllHolidays().Where(h => h.Type == type).ToList();
        }
        
        /// <summary>
        /// 获取即将到来的节日
        /// </summary>
        public static List<Holiday> GetUpcomingHolidays(int daysAhead = 30)
        {
            var today = TimeZoneService.BeijingToday;
            var upcoming = new List<Holiday>();
            
            foreach (var holiday in GetAllHolidays())
            {
                var daysUntil = holiday.DaysUntil(today.Year);
                if (daysUntil <= daysAhead)
                {
                    upcoming.Add(holiday);
                }
            }
            
            return upcoming.OrderBy(h => h.GetDate(today.Year)).ToList();
        }
        
        /// <summary>
        /// 获取指定月份的节日
        /// </summary>
        public static List<Holiday> GetHolidaysByMonth(int month)
        {
            return GetAllHolidays()
                .Where(h => h.Month == month || (h.IsLunar && h.LunarMonth == month))
                .ToList();
        }
        
        /// <summary>
        /// 显示节日列表
        /// </summary>
        public static void ListHolidays(HolidayType? type = null)
        {
            var holidays = type.HasValue ? GetHolidaysByType(type.Value) : GetAllHolidays();
            var currentYear = TimeZoneService.CurrentYear;
            
            if (!holidays.Any())
            {
                Console.WriteLine("没有找到节日。");
                return;
            }
            
            var grouped = holidays.GroupBy(h => h.Type).OrderBy(g => g.Key);
            
            foreach (var group in grouped)
            {
                Console.WriteLine($"【{GetTypeDisplayName(group.Key)}】");
                foreach (var holiday in group.OrderBy(h => h.Month).ThenBy(h => h.Day))
                {
                    var date = holiday.GetDate(currentYear);
                    var daysUntil = holiday.DaysUntil(currentYear);
                    
                    Console.Write($"  {holiday.Name}");
                    if (!string.IsNullOrEmpty(holiday.EnglishName))
                        Console.Write($" ({holiday.EnglishName})");
                    
                    // 使用格式化日期显示，支持时区敏感节日
                    Console.Write($" - {holiday.GetFormattedDateDisplay(currentYear)}");
                    
                    if (holiday.IsLunar)
                        Console.Write($" (农历{holiday.LunarMonth}月{holiday.LunarDay}日)");
                    
                    if (daysUntil == 0)
                        Console.Write(" 【今天】");
                    else if (daysUntil == 1)
                        Console.Write(" 【明天】");
                    else if (daysUntil > 0 && daysUntil <= 7)
                        Console.Write($" 【还有{daysUntil}天】");
                    
                    if (holiday.IsPublicHoliday)
                        Console.Write(" ★法定假日");
                    
                    Console.WriteLine();
                    
                    if (!string.IsNullOrEmpty(holiday.Description))
                        Console.WriteLine($"    {holiday.Description}");
                }
                Console.WriteLine();
            }
        }

        /// <summary>
        /// 显示即将到来的节日（优先显示农历日期）
        /// </summary>
        public static void ShowUpcomingHolidays(int daysAhead = 30)
        {
            var upcoming = GetUpcomingHolidays(daysAhead);
            var today = TimeZoneService.BeijingToday;

            if (!upcoming.Any())
            {
                Console.WriteLine($"未来{daysAhead}天内没有节日。");
                return;
            }

            Console.WriteLine($"【未来{daysAhead}天内的节日】");
            foreach (var holiday in upcoming.Take(10))
            {
                /* 1. 计算距今天数 */
                var daysUntil = holiday.DaysUntil(today.Year);

                /* 2. 决定「显示用」月-日字符串 */
                string displayMonthDay = holiday.IsLunar
                    ? $"农历{holiday.LunarMonth}月{holiday.LunarDay}日"
                    : $"{holiday.Month}月{holiday.Day}日";

                /* 3. 输出一行信息 */
                Console.Write($"  {displayMonthDay} {holiday.Name}");

                if (daysUntil == 0) Console.Write(" 【今天】");
                else if (daysUntil == 1) Console.Write(" 【明天】");
                else Console.Write($" 【还有{daysUntil}天】");

                Console.Write($" - {GetTypeDisplayName(holiday.Type)}");

                if (holiday.IsPublicHoliday) Console.Write(" ★");

                Console.WriteLine();
            }
        }

        /// <summary>
        /// 添加自定义节日
        /// </summary>
        public static void AddCustomHoliday(string name, HolidayType type, int month, int day, 
            string? description = null, bool isLunar = false, int? lunarMonth = null, int? lunarDay = null)
        {
            var customHolidays = LoadCustomHolidays();
            
            var holiday = new Holiday
            {
                Name = name,
                Type = type,
                Month = month,
                Day = day,
                Description = description ?? "",
                IsLunar = isLunar,
                LunarMonth = lunarMonth,
                LunarDay = lunarDay
            };
            
            customHolidays.Add(holiday);
            SaveCustomHolidays(customHolidays);
            
            Console.WriteLine($"已添加自定义节日：{name}");
        }
        
        /// <summary>
        /// 删除自定义节日
        /// </summary>
        public static void RemoveCustomHoliday(string name)
        {
            var customHolidays = LoadCustomHolidays();
            var removed = customHolidays.RemoveAll(h => h.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            
            if (removed > 0)
            {
                SaveCustomHolidays(customHolidays);
                Console.WriteLine($"已删除 {removed} 个自定义节日。");
            }
            else
            {
                Console.WriteLine("未找到匹配的自定义节日。");
            }
        }
        
        /// <summary>
        /// 获取类型显示名称
        /// </summary>
        private static string GetTypeDisplayName(HolidayType type)
        {
            return type switch
            {
                HolidayType.International => "国际节日",
                HolidayType.Chinese => "中国节日",
                HolidayType.Folk => "民间节日",
                HolidayType.Community => "社区节日",
                _ => "未知类型"
            };
        }
        
        /// <summary>
        /// 加载自定义节日
        /// </summary>
        private static List<Holiday> LoadCustomHolidays()
        {
            if (!File.Exists(_customHolidaysFile))
                return new List<Holiday>();
            
            try
            {
                var json = File.ReadAllText(_customHolidaysFile);
                return JsonSerializer.Deserialize<List<Holiday>>(json) ?? new List<Holiday>();
            }
            catch
            {
                return new List<Holiday>();
            }
        }
        
        /// <summary>
        /// 保存自定义节日
        /// </summary>
        private static void SaveCustomHolidays(List<Holiday> holidays)
        {
            var json = JsonSerializer.Serialize(holidays, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_customHolidaysFile, json);
        }
        
        /// <summary>
        /// 初始化内置节日数据
        /// </summary>
        private static List<Holiday> InitializeHolidays()
        {
            return new List<Holiday>
            {
                // 国际节日
                new Holiday 
                { 
                    Name = "元旦", 
                    EnglishName = "New Year's Day",
                    Type = HolidayType.International, 
                    Month = 1, Day = 1, 
                    Description = "新年的第一天",
                    IsPublicHoliday = true
                },
                new Holiday 
                { 
                    Name = "情人节", 
                    EnglishName = "Valentine's Day",
                    Type = HolidayType.International, 
                    Month = 2, Day = 14, 
                    Description = "情侣们的浪漫节日"
                },
                new Holiday 
                { 
                    Name = "妇女节", 
                    EnglishName = "International Women's Day",
                    Type = HolidayType.International, 
                    Month = 3, Day = 8, 
                    Description = "国际劳动妇女节",
                    IsPublicHoliday = true
                },
                new Holiday 
                { 
                    Name = "劳动节", 
                    EnglishName = "International Labor Day",
                    Type = HolidayType.International, 
                    Month = 5, Day = 1, 
                    Description = "国际劳动节",
                    IsPublicHoliday = true,
                    HolidayDays = 3
                },
                new Holiday 
                { 
                    Name = "儿童节", 
                    EnglishName = "Children's Day",
                    Type = HolidayType.International, 
                    Month = 6, Day = 1, 
                    Description = "国际儿童节"
                },
                new Holiday 
                { 
                    Name = "圣诞节", 
                    EnglishName = "Christmas Day",
                    Type = HolidayType.International, 
                    Month = 12, Day = 25, 
                    Description = "基督教传统节日"
                },

                // 中国节日
                new Holiday 
                { 
                    Name = "春节", 
                    EnglishName = "Spring Festival",
                    Type = HolidayType.Chinese, 
                    Month = 1, Day = 1, 
                    IsLunar = true, LunarMonth = 1, LunarDay = 1,
                    Description = "农历新年，中国最重要的传统节日",
                    IsPublicHoliday = true,
                    HolidayDays = 7
                },
                new Holiday 
                { 
                    Name = "元宵节", 
                    EnglishName = "Lantern Festival",
                    Type = HolidayType.Chinese, 
                    Month = 1, Day = 15, 
                    IsLunar = true, LunarMonth = 1, LunarDay = 15,
                    Description = "正月十五，赏灯吃汤圆"
                },
                new Holiday 
                { 
                    Name = "清明节", 
                    EnglishName = "Qingming Festival",
                    Type = HolidayType.Chinese, 
                    Month = 4, Day = 4,  // 默认值，实际日期由节气计算
                    Description = "祭祖扫墓的传统节日",
                    IsPublicHoliday = true,
                    IsTimeZoneSensitive = true,  // 清明节基于太阳黄经，受时区影响
                    IsSolarTerm = true,  // 基于节气计算日期
                    SolarTermName = "清明"
                },
                new Holiday 
                { 
                    Name = "端午节", 
                    EnglishName = "Dragon Boat Festival",
                    Type = HolidayType.Chinese, 
                    Month = 5, Day = 5, 
                    IsLunar = true, LunarMonth = 5, LunarDay = 5,
                    Description = "纪念屈原，吃粽子赛龙舟",
                    IsPublicHoliday = true
                },
                new Holiday 
                { 
                    Name = "七夕节", 
                    EnglishName = "Qixi Festival",
                    Type = HolidayType.Chinese, 
                    Month = 7, Day = 7, 
                    IsLunar = true, LunarMonth = 7, LunarDay = 7,
                    Description = "中国情人节，牛郎织女相会"
                },
                new Holiday 
                { 
                    Name = "中秋节", 
                    EnglishName = "Mid-Autumn Festival",
                    Type = HolidayType.Chinese, 
                    Month = 8, Day = 15, 
                    IsLunar = true, LunarMonth = 8, LunarDay = 15,
                    Description = "团圆节，赏月吃月饼",
                    IsPublicHoliday = true
                },
                new Holiday 
                { 
                    Name = "重阳节", 
                    EnglishName = "Double Ninth Festival",
                    Type = HolidayType.Chinese, 
                    Month = 9, Day = 9, 
                    IsLunar = true, LunarMonth = 9, LunarDay = 9,
                    Description = "登高望远，敬老节"
                },
                new Holiday 
                { 
                    Name = "国庆节", 
                    EnglishName = "National Day",
                    Type = HolidayType.Chinese, 
                    Month = 10, Day = 1, 
                    Description = "中华人民共和国成立纪念日",
                    IsPublicHoliday = true,
                    HolidayDays = 7
                },

                // 民间节日
                new Holiday 
                { 
                    Name = "腊八节", 
                    EnglishName = "Laba Festival",
                    Type = HolidayType.Folk, 
                    Month = 12, Day = 8, 
                    IsLunar = true, LunarMonth = 12, LunarDay = 8,
                    Description = "喝腊八粥，祈求丰收"
                },
                new Holiday 
                { 
                    Name = "小年", 
                    EnglishName = "Little New Year",
                    Type = HolidayType.Folk, 
                    Month = 12, Day = 23, 
                    IsLunar = true, LunarMonth = 12, LunarDay = 23,
                    Description = "祭灶神，准备过年"
                },
                new Holiday 
                { 
                    Name = "泼水节", 
                    EnglishName = "Water Splashing Festival",
                    Type = HolidayType.Folk, 
                    Month = 4, Day = 13, 
                    Description = "傣族新年，互相泼水祈福"
                },
                new Holiday 
                { 
                    Name = "火把节", 
                    EnglishName = "Torch Festival",
                    Type = HolidayType.Folk, 
                    Month = 6, Day = 24, 
                    Description = "彝族传统节日，点燃火把"
                },
                new Holiday 
                { 
                    Name = "那达慕大会", 
                    EnglishName = "Naadam Festival",
                    Type = HolidayType.Folk, 
                    Month = 7, Day = 15, 
                    Description = "蒙古族传统节日，赛马摔跤射箭"
                },

                // 社区节日
                new Holiday 
                { 
                    Name = "程序员节", 
                    EnglishName = "Programmer's Day",
                    Type = HolidayType.Community, 
                    Month = 10, Day = 24, 
                    Description = "2的10次方！",
                    IsPublicHoliday = false
                },
                new Holiday 
                { 
                    Name = "双十一", 
                    EnglishName = "Singles' Day Shopping",
                    Type = HolidayType.Community, 
                    Month = 11, Day = 11, 
                    Description = "光棍节购物狂欢节"
                },
                new Holiday 
                { 
                    Name = "双十二", 
                    EnglishName = "Double 12 Shopping",
                    Type = HolidayType.Community, 
                    Month = 12, Day = 12, 
                    Description = "年末购物节"
                },
                new Holiday 
                { 
                    Name = "618购物节", 
                    EnglishName = "618 Shopping Festival",
                    Type = HolidayType.Community, 
                    Month = 6, Day = 18, 
                    Description = "年中购物狂欢节"
                }
            };
        }
    }
}