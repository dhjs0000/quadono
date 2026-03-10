using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Quadono
{
    /// <summary>
    /// 重要日服务类
    /// </summary>
    public static class ImportantDateService
    {
        private static readonly List<ImportantDate> _importantDates = InitializeImportantDates();
        private static readonly string _customImportantDatesFile = "custom_important_dates.json";
        
        /// <summary>
        /// 获取所有重要日
        /// </summary>
        public static List<ImportantDate> GetAllImportantDates()
        {
            var customDates = LoadCustomImportantDates();
            return _importantDates.Concat(customDates).ToList();
        }
        
        /// <summary>
        /// 按类型获取重要日
        /// </summary>
        public static List<ImportantDate> GetImportantDatesByType(ImportantDateType type)
        {
            return GetAllImportantDates().Where(d => d.Type == type).ToList();
        }
        
        /// <summary>
        /// 按重要级别获取重要日
        /// </summary>
        public static List<ImportantDate> GetImportantDatesByLevel(ImportanceLevel level)
        {
            return GetAllImportantDates().Where(d => d.Level == level).ToList();
        }
        
        /// <summary>
        /// 获取即将到来的重要日
        /// </summary>
        public static List<ImportantDate> GetUpcomingImportantDates(int daysAhead = 90)
        {
            var today = TimeZoneService.BeijingToday;
            var upcoming = new List<ImportantDate>();
            
            foreach (var date in GetAllImportantDates())
            {
                var daysUntil = date.DaysUntil(today.Year);
                if (daysUntil <= daysAhead && daysUntil >= 0)
                {
                    upcoming.Add(date);
                }
            }
            
            return upcoming.OrderBy(d => d.GetDate(today.Year)).ToList();
        }
        
        /// <summary>
        /// 获取需要报名提醒的重要日
        /// </summary>
        public static List<ImportantDate> GetRegistrationReminders(int currentYear)
        {
            return GetAllImportantDates()
                .Where(d => d.ShouldShowRegistrationReminder(currentYear))
                .OrderBy(d => d.DaysUntil(currentYear))
                .ToList();
        }
        
        /// <summary>
        /// 显示重要日列表
        /// </summary>
        public static void ListImportantDates(ImportantDateType? type = null, ImportanceLevel? level = null)
        {
            var dates = GetAllImportantDates();
            
            if (type.HasValue)
                dates = dates.Where(d => d.Type == type.Value).ToList();
                
            if (level.HasValue)
                dates = dates.Where(d => d.Level == level.Value).ToList();
            
            var currentYear = TimeZoneService.CurrentYear;
            
            if (!dates.Any())
            {
                Console.WriteLine("没有找到重要日。");
                return;
            }
            
            var grouped = dates.GroupBy(d => d.Type).OrderBy(g => g.Key);
            
            foreach (var group in grouped)
            {
                Console.WriteLine($"【{group.Key.GetTypeDisplayName()}】");
                foreach (var date in group.OrderBy(d => d.Month).ThenBy(d => d.Day))
                {
                    var dateValue = date.GetDate(currentYear);
                    var daysUntil = date.DaysUntil(currentYear);
                    
                    Console.Write($"  {date.GetLevelSymbol()} {date.Name}");
                    
                    if (!string.IsNullOrEmpty(date.EnglishName))
                        Console.Write($" ({date.EnglishName})");
                    
                    Console.Write($" - {date.Month}月{date.Day}日");
                    
                    if (date.IsDateRange && date.DurationDays > 1)
                        Console.Write($"({date.DurationDays}天)");
                    
                    if (daysUntil == 0)
                        Console.Write(" 【今天】");
                    else if (daysUntil == 1)
                        Console.Write(" 【明天】");
                    else if (daysUntil > 0 && daysUntil <= 7)
                        Console.Write($" 【还有{daysUntil}天】");
                    
                    Console.Write($" - {date.GetLevelDisplayName()}");
                    
                    if (date.RequiresRegistration && date.RegistrationAdvanceDays > 0)
                        Console.Write($" (需提前{date.RegistrationAdvanceDays}天报名)");
                    
                    Console.WriteLine();
                    
                    if (!string.IsNullOrEmpty(date.Description))
                        Console.WriteLine($"    {date.Description}");
                    
                    if (!string.IsNullOrEmpty(date.Fee))
                        Console.WriteLine($"    费用: {date.Fee}");
                    
                    if (date.ShouldShowRegistrationReminder(currentYear))
                    {
                        var daysUntilRegistrationDeadline = daysUntil - date.RegistrationAdvanceDays;
                        Console.WriteLine($"    ⚠️  报名提醒: 还有{daysUntilRegistrationDeadline}天截止报名！");
                        if (!string.IsNullOrEmpty(date.RegistrationUrl))
                            Console.WriteLine($"    报名网址: {date.RegistrationUrl}");
                    }
                    else
                        Console.WriteLine($"    ⚠️  报名提醒: 报名已截止！");
                }
                Console.WriteLine();
            }
        }
        
        /// <summary>
        /// 显示即将到来的重要日
        /// </summary>
        public static void ShowUpcomingImportantDates(int daysAhead = 90)
        {
            var upcoming = GetUpcomingImportantDates(daysAhead);
            var today = TimeZoneService.BeijingToday;
            
            var registrationReminders = GetRegistrationReminders(today.Year);
            
            if (!upcoming.Any() && !registrationReminders.Any())
            {
                Console.WriteLine($"未来{daysAhead}天内没有重要日。");
                return;
            }
            
            // 显示报名提醒
            if (registrationReminders.Any())
            {
                Console.WriteLine($"【报名提醒】");
                foreach (var date in registrationReminders.Take(5))
                {
                    var daysUntil = date.DaysUntil(today.Year);
                    Console.WriteLine($"  ⚠️  {date.Name} - 还有{daysUntil}天考试，报名即将截止！");
                    if (!string.IsNullOrEmpty(date.RegistrationUrl))
                        Console.WriteLine($"     报名: {date.RegistrationUrl}");
                }
                Console.WriteLine();
            }
            
            // 显示即将到来的重要日
            if (upcoming.Any())
            {
                Console.WriteLine($"【未来{daysAhead}天内的重要日】");
                foreach (var date in upcoming.Take(15))
                {
                    var daysUntil = date.DaysUntil(today.Year);
                    
                    Console.Write($"  {date.GetLevelSymbol()} {date.Month}月{date.Day}日 {date.Name}");
                    
                    if (daysUntil == 0)
                        Console.Write(" 【今天】");
                    else if (daysUntil == 1)
                        Console.Write(" 【明天】");
                    else
                        Console.Write($" 【还有{daysUntil}天】");
                    
                    Console.Write($" - {date.GetTypeDisplayName()}");
                    Console.Write($"({date.GetLevelDisplayName()})");
                    
                    if (date.RequiresRegistration && !date.ShouldShowRegistrationReminder(today.Year))
                        Console.Write(" ✅报名截止");
                    else if (date.ShouldShowRegistrationReminder(today.Year))
                        Console.Write(" ⏰报名中");
                    
                    Console.WriteLine();
                    
                    if (!string.IsNullOrEmpty(date.Fee))
                        Console.WriteLine($"     费用: {date.Fee}");
                }
            }
        }
        
        /// <summary>
        /// 添加自定义重要日
        /// </summary>
        public static void AddCustomImportantDate(string name, ImportantDateType type, ImportanceLevel level, 
            int month, int day, string? description = null, string? fee = null, 
            bool requiresRegistration = true, int registrationAdvanceDays = 30,
            string? registrationUrl = null, string? website = null, 
            bool isDateRange = false, int durationDays = 1, int? year = null)
        {
            var customDates = LoadCustomImportantDates();
            
            var importantDate = new ImportantDate
            {
                Name = name,
                Type = type,
                Level = level,
                Month = month,
                Day = day,
                Description = description ?? "",
                Fee = fee ?? "",
                RequiresRegistration = requiresRegistration,
                RegistrationAdvanceDays = registrationAdvanceDays,
                RegistrationUrl = registrationUrl ?? "",
                Website = website ?? "",
                IsDateRange = isDateRange,
                DurationDays = durationDays,
                Year = year
            };
            
            customDates.Add(importantDate);
            SaveCustomImportantDates(customDates);
            
            Console.WriteLine($"已添加自定义重要日：{name}");
            
            if (requiresRegistration && registrationAdvanceDays > 0)
            {
                Console.WriteLine($"提醒：需要提前{registrationAdvanceDays}天报名");
            }
        }
        
        /// <summary>
        /// 删除自定义重要日
        /// </summary>
        public static void RemoveCustomImportantDate(string name)
        {
            var customDates = LoadCustomImportantDates();
            var removed = customDates.RemoveAll(d => d.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            
            if (removed > 0)
            {
                SaveCustomImportantDates(customDates);
                Console.WriteLine($"已删除 {removed} 个自定义重要日。");
            }
            else
            {
                Console.WriteLine("未找到匹配的自定义重要日。");
            }
        }
        
        /// <summary>
        /// 搜索重要日
        /// </summary>
        public static void SearchImportantDates(string keyword)
        {
            var dates = GetAllImportantDates()
                .Where(d => d.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                           d.EnglishName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                           d.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                           d.Tags.Any(t => t.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            if (!dates.Any())
            {
                Console.WriteLine($"未找到包含\"{keyword}\"的重要日。");
                return;
            }
            
            Console.WriteLine($"【搜索结果：\"{keyword}\"】");
            var currentYear = TimeZoneService.CurrentYear;
            
            foreach (var date in dates.OrderBy(d => d.Month).ThenBy(d => d.Day))
            {
                var daysUntil = date.DaysUntil(currentYear);
                Console.Write($"  {date.GetLevelSymbol()} {date.Name} - {date.Month}月{date.Day}日");
                
                if (daysUntil >= 0 && daysUntil <= 30)
                {
                    if (daysUntil == 0)
                        Console.Write(" 【今天】");
                    else if (daysUntil == 1)
                        Console.Write(" 【明天】");
                    else
                        Console.Write($" 【还有{daysUntil}天】");
                }
                
                Console.WriteLine($" ({date.GetTypeDisplayName()} - {date.GetLevelDisplayName()})");
            }
        }
        
        /// <summary>
        /// 加载自定义重要日
        /// </summary>
        public static List<ImportantDate> LoadCustomImportantDates()
        {
            if (!File.Exists(_customImportantDatesFile))
                return new List<ImportantDate>();
            
            try
            {
                var json = File.ReadAllText(_customImportantDatesFile);
                return JsonSerializer.Deserialize<List<ImportantDate>>(json) ?? new List<ImportantDate>();
            }
            catch
            {
                return new List<ImportantDate>();
            }
        }
        
        /// <summary>
        /// 保存自定义重要日
        /// </summary>
        public static void SaveCustomImportantDates(List<ImportantDate> dates)
        {
            var json = JsonSerializer.Serialize(dates, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_customImportantDatesFile, json);
        }
        
        /// <summary>
        /// 初始化内置重要日数据
        /// </summary>
        private static List<ImportantDate> InitializeImportantDates()
        {
            return new List<ImportantDate>
            {
                new ImportantDate
                {
                    Name = "高考",
                    EnglishName = "National College Entrance Examination (Gaokao)",
                    Type = ImportantDateType.MiddleSchoolExam,
                    Level = ImportanceLevel.Critical,
                    Month = 6, Day = 7,
                    Description = "普通高等学校招生全国统一考试",
                    IsDateRange = true, DurationDays = 3,
                    RequiresRegistration = true, RegistrationAdvanceDays = 90,
                    Fee = "约150元",
                    Website = "https://gaokao.chsi.com.cn/",
                    Tags = new List<string> { "高考", "大学", "升学" }
                },
                
            };
        }
    }
}