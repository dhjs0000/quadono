using System;
using System.Collections.Generic;
using System.Linq;

namespace Quadono
{
    /// <summary>
    /// 重要日类型枚举
    /// </summary>
    public enum ImportantDateType
    {
        /// <summary>
        /// 中学考试
        /// </summary>
        MiddleSchoolExam,
        
        /// <summary>
        /// 大学考试
        /// </summary>
        UniversityExam,
        
        /// <summary>
        /// 编程竞赛
        /// </summary>
        ProgrammingContest,
        
        /// <summary>
        /// 学科竞赛
        /// </summary>
        SubjectContest,
        
        /// <summary>
        /// 语言考试
        /// </summary>
        LanguageExam,
        
        /// <summary>
        /// 职业考试
        /// </summary>
        ProfessionalExam,
        
        /// <summary>
        /// 招生考试
        /// </summary>
        AdmissionExam,
        
        /// <summary>
        /// 其他重要日期
        /// </summary>
        Other
    }

    /// <summary>
    /// 重要级别枚举
    /// </summary>
    public enum ImportanceLevel
    {
        /// <summary>
        /// 普通
        /// </summary>
        Normal,
        
        /// <summary>
        /// 重要
        /// </summary>
        Important,
        
        /// <summary>
        /// 非常重要
        /// </summary>
        VeryImportant,
        
        /// <summary>
        /// 极其重要
        /// </summary>
        Critical
    }

    /// <summary>
    /// 重要日数据模型
    /// </summary>
    public class ImportantDate
    {
        /// <summary>
        /// 重要日名称
        /// </summary>
        public string Name { get; set; } = "";
        
        /// <summary>
        /// 重要日英文名称
        /// </summary>
        public string EnglishName { get; set; } = "";
        
        /// <summary>
        /// 重要日类型
        /// </summary>
        public ImportantDateType Type { get; set; }
        
        /// <summary>
        /// 重要级别
        /// </summary>
        public ImportanceLevel Level { get; set; }
        
        /// <summary>
        /// 公历月份（1-12）
        /// </summary>
        public int Month { get; set; }
        
        /// <summary>
        /// 公历日期（1-31）
        /// </summary>
        public int Day { get; set; }
        
        /// <summary>
        /// 是否每年日期相对固定（如高考）
        /// </summary>
        public bool IsAnnualFixed { get; set; } = true;
        
        /// <summary>
        /// 是否为日期范围（如中考持续几天）
        /// </summary>
        public bool IsDateRange { get; set; } = false;
        
        /// <summary>
        /// 持续天数（如果IsDateRange为true）
        /// </summary>
        public int DurationDays { get; set; } = 1;
        
        /// <summary>
        /// 年份（如果为特定年份的重要日）
        /// </summary>
        public int? Year { get; set; }
        
        /// <summary>
        /// 描述信息
        /// </summary>
        public string Description { get; set; } = "";
        
        /// <summary>
        /// 相关网址
        /// </summary>
        public string Website { get; set; } = "";
        
        /// <summary>
        /// 报名网址
        /// </summary>
        public string RegistrationUrl { get; set; } = "";
        
        /// <summary>
        /// 考试费用（如果适用）
        /// </summary>
        public string Fee { get; set; } = "";
        
        /// <summary>
        /// 是否需要提前报名
        /// </summary>
        public bool RequiresRegistration { get; set; } = true;
        
        /// <summary>
        /// 提前报名天数
        /// </summary>
        public int RegistrationAdvanceDays { get; set; } = 30;
        
        /// <summary>
        /// 标签（用于搜索和分类）
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public ImportantDate()
        {
        }

        /// <summary>
        /// 使用基本参数创建重要日
        /// </summary>
        /// <param name="name">重要日名称</param>
        /// <param name="type">重要日类型</param>
        /// <param name="level">重要级别</param>
        /// <param name="year">年份</param>
        /// <param name="month">月份</param>
        /// <param name="day">日期</param>
        /// <param name="description">描述信息</param>
        public ImportantDate(
            string name,
            ImportantDateType type,
            ImportanceLevel level,
            int year,
            int month,
            int day,
            string description)
        {
            Name = name;
            Type = type;
            Level = level;
            Year = year;
            Month = month;
            Day = day;
            Description = description;
        }

        /// <summary>
        /// 获取当前年份的重要日日期
        /// </summary>
        public DateTime GetDate(int currentYear)
        {
            if (Year.HasValue && Year.Value != currentYear)
            {
                // 如果指定了特定年份且不是当前年份，返回一个无效日期
                return DateTime.MinValue;
            }
            
            try
            {
                return new DateTime(Year ?? currentYear, Month, Day);
            }
            catch
            {
                // 处理无效日期（如2月30日）
                var daysInMonth = DateTime.DaysInMonth(Year ?? currentYear, Month);
                return new DateTime(Year ?? currentYear, Month, Math.Min(Day, daysInMonth));
            }
        }
        
        /// <summary>
        /// 获取距离重要日的天数
        /// </summary>
        public int DaysUntil(int currentYear)
        {
            var importantDate = GetDate(currentYear);
            if (importantDate == DateTime.MinValue)
                return int.MaxValue; // 无效日期返回最大值
                
            var today = DateTime.Today;
            
            if (importantDate < today)
            {
                if (IsAnnualFixed && !Year.HasValue)
                {
                    // 对于年度固定的重要日，计算下一年的日期
                    importantDate = GetDate(currentYear + 1);
                }
                else
                {
                    return int.MaxValue; // 非年度固定且已过期的返回最大值
                }
            }
            
            return (importantDate - today).Days;
        }
        
        /// <summary>
        /// 获取重要日类型显示名称
        /// </summary>
        public string GetTypeDisplayName()
        {
            return Type switch
            {
                ImportantDateType.MiddleSchoolExam => "中学考试",
                ImportantDateType.UniversityExam => "大学考试",
                ImportantDateType.ProgrammingContest => "编程竞赛",
                ImportantDateType.SubjectContest => "学科竞赛",
                ImportantDateType.LanguageExam => "语言考试",
                ImportantDateType.ProfessionalExam => "职业考试",
                ImportantDateType.AdmissionExam => "招生考试",
                ImportantDateType.Other => "其他重要日",
                _ => "未知类型"
            };
        }
        
        /// <summary>
        /// 获取重要级别显示名称
        /// </summary>
        public string GetLevelDisplayName()
        {
            return Level switch
            {
                ImportanceLevel.Normal => "普通",
                ImportanceLevel.Important => "重要",
                ImportanceLevel.VeryImportant => "非常重要",
                ImportanceLevel.Critical => "极其重要",
                _ => "未知级别"
            };
        }
        
        /// <summary>
        /// 获取级别对应的符号
        /// </summary>
        public string GetLevelSymbol()
        {
            return Level switch
            {
                ImportanceLevel.Normal => "●",
                ImportanceLevel.Important => "★",
                ImportanceLevel.VeryImportant => "✦",
                ImportanceLevel.Critical => "⚡",
                _ => "●"
            };
        }
        
        /// <summary>
        /// 是否显示报名提醒
        /// </summary>
        public bool ShouldShowRegistrationReminder(int currentYear)
        {
            if (!RequiresRegistration)
                return false;
                
            var daysUntil = DaysUntil(currentYear);
            // 只有当考试还没到，且报名尚未截止时才显示提醒
            // 报名截止日 = 考试日 - 提前报名天数
            // 所以 daysUntil > RegistrationAdvanceDays 表示报名还未截止
            return daysUntil > RegistrationAdvanceDays && daysUntil > 0;
        }
    }

    /// <summary>
    /// 重要日类型扩展方法
    /// </summary>
    public static class ImportantDateTypeExtensions
    {
        /// <summary>
        /// 获取重要日类型显示名称
        /// </summary>
        public static string GetTypeDisplayName(this ImportantDateType type)
        {
            return type switch
            {
                ImportantDateType.MiddleSchoolExam => "中学考试",
                ImportantDateType.UniversityExam => "大学考试",
                ImportantDateType.ProgrammingContest => "编程竞赛",
                ImportantDateType.SubjectContest => "学科竞赛",
                ImportantDateType.LanguageExam => "语言考试",
                ImportantDateType.ProfessionalExam => "职业考试",
                ImportantDateType.AdmissionExam => "招生考试",
                ImportantDateType.Other => "其他重要日",
                _ => "未知类型"
            };
        }
    }
}