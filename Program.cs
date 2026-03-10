using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Quadono
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            // 如果没有任何参数，直接打印用法
            if (args.Length == 0)
            {
                PrintUsage();
                return 0;
            }

            // 子命令需要后台服务时，统一走 Host；纯查询可立即返回
            switch (args[0].ToLowerInvariant())
            {
                case "add":
                    return HandleAddCommand(args);

                case "list":
                    return HandleListCommand(args);

                case "edit":
                    return HandleEditCommand(args);

                case "start":
                    return HandleStartCommand(args);

                case "pause":
                    return HandlePauseCommand(args);

                case "done":
                    if (args.Length < 2) goto default;
                    TaskStore.Done(args[1]);
                    Console.WriteLine("任务已标记完成");
                    return 0;

                case "del":
                    if (args.Length < 2) goto default;
                    TaskStore.Del(args[1]);
                    Console.WriteLine("任务已删除");
                    return 0;

                case "matrix":
                    return HandleMatrixCommand(args);

                case "pom":
                case "pomodoro":
                case "25":
                    return await HandlePomodoroCommand(args);

                case "alarm":
                    if (args.Length < 3) goto default;
                    return await RunHostAsync(() => AlarmService.Set(args[1], string.Join(' ', args[2..])));

                case "holidays":
                case "holiday":
                case "节日":
                    return HandleHolidayCommand(args);

                case "important":
                case "dates":
                case "重要日":
                case "考试":
                    return HandleImportantDateCommand(args);

                default:
                    Console.WriteLine($"未知命令：{args[0]}");
                    PrintUsage();
                    return 1;
            }
        }

        static async Task<int> RunHostAsync(Action backgroundWork)
        {
            using var host = Host.CreateDefaultBuilder()
                .ConfigureServices(svc =>
                {
                    svc.AddSingleton<IHostedService>(_ => new QuadonoHost(backgroundWork));
                })
                .Build();
            await host.RunAsync();
            return 0;
        }

        static void PrintUsage()
        {
            Console.WriteLine("quadono β0.4.0 — Ethernos Studio");
            Console.WriteLine(TimeZoneService.GetCurrentTimeDisplay());
            Console.WriteLine();
            Console.WriteLine("用法：");
            Console.WriteLine("任务管理：");
            Console.WriteLine("  quadono add <标题> <象限1-4> <预估分钟> [描述] [--due=MM-dd]");
            Console.WriteLine("  quadono list [--quadrant=1-4] [--status=pending|inprogress|paused|completed] [--today] [--all]");
            Console.WriteLine("  quadono edit <guid> [标题] [象限] [分钟] [描述]");
            Console.WriteLine("  quadono start <guid>         开始任务");
            Console.WriteLine("  quadono pause <guid>         暂停任务");
            Console.WriteLine("  quadono done <guid>          标记完成");
            Console.WriteLine("  quadono del <guid>           删除任务");
            Console.WriteLine("  quadono matrix               四象限矩阵视图");
            Console.WriteLine("番茄钟：");
            Console.WriteLine("  quadono pom start [--task=<guid>]  开始番茄钟");
            Console.WriteLine("  quadono pom stop                   停止番茄钟");
            Console.WriteLine("  quadono pom status                 查看状态");
            Console.WriteLine("  quadono pom stats                  统计信息");
            Console.WriteLine("闹钟：");
            Console.WriteLine("  quadono alarm 09:00 \"Stand-up\"  设置闹钟");
            Console.WriteLine("节日：");
            Console.WriteLine("  quadono holidays             显示所有节日");
            Console.WriteLine("  quadono holidays upcoming    显示即将到来的节日");
            Console.WriteLine("  quadono holidays add <名称> <类型> <月> <日> [描述] [--lunar]");
            Console.WriteLine("  quadono holidays del <名称>  删除自定义节日");
            Console.WriteLine("重要日：");
            Console.WriteLine("  quadono important            显示所有重要日");
            Console.WriteLine("  quadono important upcoming   显示即将到来的重要日");
            Console.WriteLine("  quadono important add <名称> <类型> <级别> <年-月-日> [描述] [--remind=30,7,1]");
            Console.WriteLine("  quadono important search <关键词> 搜索重要日");
            Console.WriteLine("  quadono important del <名称> 删除重要日");
            Console.WriteLine("  节日类型: international, chinese, folk, community");
            Console.WriteLine("  重要日类型: exam, contest, language, professional, admission, other");
            Console.WriteLine("  重要级别: normal, important, veryimportant, critical");
        }

        static int HandleAddCommand(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("用法: quadono add <标题> <象限1-4> <预估分钟> [描述] [--due=MM-dd]");
                return 1;
            }

            string title = args[1];
            if (!int.TryParse(args[2], out int quadrant) || quadrant < 1 || quadrant > 4)
            {
                Console.WriteLine("象限必须是1-4之间的数字");
                return 1;
            }

            if (!int.TryParse(args[3], out int minutes) || minutes <= 0)
            {
                Console.WriteLine("预估分钟必须是正数");
                return 1;
            }

            string? description = null;
            DateTime? dueDate = null;

            // 解析可选参数
            for (int i = 4; i < args.Length; i++)
            {
                if (args[i].StartsWith("--due="))
                {
                    var dueStr = args[i][6..];
                    if (DateTime.TryParseExact(dueStr, "MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDue))
                    {
                        dueDate = new DateTime(DateTime.Now.Year, parsedDue.Month, parsedDue.Day);
                    }
                    else
                    {
                        Console.WriteLine("截止日期格式错误，请使用 MM-dd 格式");
                        return 1;
                    }
                }
                else if (description == null && !args[i].StartsWith("--"))
                {
                    description = args[i];
                }
            }

            TaskStore.Add(title, quadrant, minutes, description, dueDate);
            Console.WriteLine("任务已添加");
            return 0;
        }

        static int HandleListCommand(string[] args)
        {
            int? quadrant = null;
            TaskStatus? status = null;
            bool today = false;
            bool showAll = false;

            for (int i = 1; i < args.Length; i++)
            {
                if (args[i].StartsWith("--quadrant="))
                {
                    if (int.TryParse(args[i][11..], out int q) && q >= 1 && q <= 4)
                        quadrant = q;
                }
                else if (args[i].StartsWith("--status="))
                {
                    Enum.TryParse<TaskStatus>(args[i][9..], true, out var s);
                    status = s;
                }
                else if (args[i] == "--today")
                {
                    today = true;
                }
                else if (args[i] == "--all")
                {
                    showAll = true;
                }
            }

            TaskStore.List(quadrant, status, today, showAll);
            return 0;
        }

        static int HandleEditCommand(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("用法: quadono edit <guid> [标题] [象限] [分钟] [描述]");
                return 1;
            }

            string id = args[1];
            string? title = null;
            int? quadrant = null;
            int? minutes = null;
            string? description = null;

            for (int i = 2; i < args.Length; i++)
            {
                if (int.TryParse(args[i], out int q) && q >= 1 && q <= 4)
                {
                    quadrant = q;
                }
                else if (int.TryParse(args[i], out int m) && m > 0)
                {
                    minutes = m;
                }
                else if (title == null && !args[i].StartsWith("--"))
                {
                    title = args[i];
                }
                else if (description == null && !args[i].StartsWith("--"))
                {
                    description = args[i];
                }
            }

            TaskStore.Edit(id, title, quadrant, minutes, description);
            Console.WriteLine("任务已更新");
            return 0;
        }

        static int HandleStartCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: quadono start <guid>");
                return 1;
            }

            TaskStore.Start(args[1]);
            Console.WriteLine("任务已开始");
            return 0;
        }

        static int HandlePauseCommand(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("用法: quadono pause <guid>");
                return 1;
            }

            TaskStore.Pause(args[1]);
            Console.WriteLine("任务已暂停");
            return 0;
        }

        static int HandleMatrixCommand(string[] args)
        {
            // 四象限矩阵视图
            var tasks = new List<TaskItem>();
            
            // 从任务存储文件直接读取，避免锁定问题
            if (File.Exists("quadono.json"))
            {
                try
                {
                    var json = File.ReadAllText("quadono.json");
                    tasks = System.Text.Json.JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new();
                    tasks = tasks.Where(t => !t.Done).ToList();
                }
                catch
                {
                    tasks = new List<TaskItem>();
                }
            }

            Console.WriteLine("\n【四象限任务矩阵】\n");
            
            // 创建四象限矩阵
            var q1 = tasks.Where(t => t.Quadrant == 1).ToList();
            var q2 = tasks.Where(t => t.Quadrant == 2).ToList();
            var q3 = tasks.Where(t => t.Quadrant == 3).ToList();
            var q4 = tasks.Where(t => t.Quadrant == 4).ToList();

            Console.WriteLine("┌─────────────────┬─────────────────┐");
            Console.WriteLine("│  重要且紧急 [1]  │  重要不紧急 [2]  │");
            Console.WriteLine("├─────────────────┼─────────────────┤");
            
            int maxRows = Math.Max(Math.Max(q1.Count, q2.Count), Math.Max(q3.Count, q4.Count));
            maxRows = Math.Max(maxRows, 1); // 至少显示一行
            
            for (int i = 0; i < maxRows; i++)
            {
                string q1Text = i < q1.Count ? $"{q1[i].Title} ({q1[i].EstimateMinutes}min)" : "";
                string q2Text = i < q2.Count ? $"{q2[i].Title} ({q2[i].EstimateMinutes}min)" : "";
                
                Console.WriteLine($"│ {q1Text,-15} │ {q2Text,-15} │");
            }
            
            Console.WriteLine("├─────────────────┼─────────────────┤");
            Console.WriteLine("│ 不重要但紧急 [3] │ 不重要不紧急 [4] │");
            Console.WriteLine("├─────────────────┼─────────────────┤");
            
            for (int i = 0; i < maxRows; i++)
            {
                string q3Text = i < q3.Count ? $"{q3[i].Title} ({q3[i].EstimateMinutes}min)" : "";
                string q4Text = i < q4.Count ? $"{q4[i].Title} ({q4[i].EstimateMinutes}min)" : "";
                
                Console.WriteLine($"│ {q3Text,-15} │ {q4Text,-15} │");
            }
            
            Console.WriteLine("└─────────────────┴─────────────────┘");
            Console.WriteLine($"\n总计: {tasks.Count} 个任务");
            return 0;
        }

        static async Task<int> HandlePomodoroCommand(string[] args)
        {
            if (args.Length < 2)
            {
                return await RunHostAsync(() => PomodoroService.Start(null));
            }

            switch (args[1].ToLowerInvariant())
            {
                case "start":
                    string? taskId = null;
                    if (args.Length >= 3 && args[2].StartsWith("--task="))
                    {
                        taskId = args[2][7..];
                    }
                    return await RunHostAsync(() => PomodoroService.Start(taskId));

                case "stop":
                    return await RunHostAsync(() => PomodoroService.Stop());

                case "status":
                    PomodoroService.ShowStatus();
                    return 0;

                case "stats":
                    PomodoroService.ShowStats();
                    return 0;

                default:
                    Console.WriteLine("未知的番茄钟命令");
                    return 1;
            }
        }

        static int HandleHolidayCommand(string[] args)
        {
            if (args.Length == 1)
            {
                // 显示所有节日
                HolidayService.ListHolidays();
                return 0;
            }

            switch (args[1].ToLowerInvariant())
            {
                case "upcoming":
                case "coming":
                case "近期":
                    int daysAhead = 30;
                    if (args.Length >= 3 && int.TryParse(args[2], out int days))
                        daysAhead = days;
                    HolidayService.ShowUpcomingHolidays(daysAhead);
                    return 0;

                case "add":
                case "添加":
                    if (args.Length < 6)
                    {
                        Console.WriteLine("用法: quadono holidays add <名称> <类型> <月> <日> [描述]");
                        Console.WriteLine("节日类型: international, chinese, folk, community");
                        return 1;
                    }
                    
                    if (!Enum.TryParse<HolidayType>(args[3], true, out var type))
                    {
                        Console.WriteLine($"无效的节日类型: {args[3]}");
                        Console.WriteLine("可用类型: international, chinese, folk, community");
                        return 1;
                    }
                    
                    if (!int.TryParse(args[4], out int month) || month < 1 || month > 12)
                    {
                        Console.WriteLine($"无效的月份: {args[4]}");
                        return 1;
                    }
                    
                    if (!int.TryParse(args[5], out int day) || day < 1 || day > 31)
                    {
                        Console.WriteLine($"无效的日期: {args[5]}");
                        return 1;
                    }
                    
                    string description = args.Length >= 7 ? string.Join(" ", args[6..]) : "";
                    HolidayService.AddCustomHoliday(args[2], type, month, day, description);
                    return 0;

                case "del":
                case "delete":
                case "删除":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("用法: quadono holidays del <名称>");
                        return 1;
                    }
                    HolidayService.RemoveCustomHoliday(args[2]);
                    return 0;

                case "international":
                case "国际":
                    HolidayService.ListHolidays(HolidayType.International);
                    return 0;

                case "chinese":
                case "中国":
                    HolidayService.ListHolidays(HolidayType.Chinese);
                    return 0;

                case "folk":
                case "民间":
                    HolidayService.ListHolidays(HolidayType.Folk);
                    return 0;

                case "community":
                case "社区":
                    HolidayService.ListHolidays(HolidayType.Community);
                    return 0;

                default:
                    Console.WriteLine($"未知的节日命令: {args[1]}");
                    Console.WriteLine("可用命令: upcoming, add, del, international, chinese, folk, community");
                    return 1;
            }
        }

        static int HandleImportantDateCommand(string[] args)
        {
            if (args.Length == 1)
            {
                // 显示所有重要日
                ImportantDateService.ListImportantDates();
                return 0;
            }

            switch (args[1].ToLowerInvariant())
            {
                case "upcoming":
                case "coming":
                case "近期":
                    int daysAhead = 90;
                    if (args.Length >= 3 && int.TryParse(args[2], out int days))
                        daysAhead = days;
                    ImportantDateService.ShowUpcomingImportantDates(daysAhead);
                    return 0;

                case "add":
                case "添加":
                    return HandleImportantDateAddCommand(args);

                case "del":
                case "delete":
                case "删除":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("用法: quadono important del <名称>");
                        return 1;
                    }
                    ImportantDateService.RemoveCustomImportantDate(args[2]);
                    return 0;

                case "search":
                case "搜索":
                case "find":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("用法: quadono important search <关键词>");
                        return 1;
                    }
                    string keyword = string.Join(" ", args[2..]);
                    ImportantDateService.SearchImportantDates(keyword);
                    return 0;

                case "exam":
                case "考试":
                    ImportantDateService.ListImportantDates(type: ImportantDateType.MiddleSchoolExam);
                    return 0;

                case "contest":
                case "竞赛":
                    ImportantDateService.ListImportantDates(type: ImportantDateType.ProgrammingContest);
                    return 0;

                case "language":
                case "语言":
                    ImportantDateService.ListImportantDates(type: ImportantDateType.LanguageExam);
                    return 0;

                case "critical":
                case "极其重要":
                    ImportantDateService.ListImportantDates(level: ImportanceLevel.Critical);
                    return 0;

                case "registration":
                case "报名":
                    ImportantDateService.ShowUpcomingImportantDates(365); // 显示全年需要报名的考试
                    return 0;

                default:
                    Console.WriteLine($"未知的重要日命令: {args[1]}");
                    Console.WriteLine("可用命令: upcoming, add, del, search, exam, contest, language, critical, registration");
                    return 1;
            }
        }

        static int HandleImportantDateAddCommand(string[] args)
        {
            if (args.Length < 7)
            {
                Console.WriteLine("用法: quadono important add <名称> <类型> <级别> <年-月-日> [描述] [--remind=30,7,1]");
                Console.WriteLine("重要日类型: exam, contest, language, professional, admission, other");
                Console.WriteLine("重要级别: normal, important, veryimportant, critical");
                Console.WriteLine("日期格式: YYYY-MM-DD 或 MM-DD (当年)");
                return 1;
            }

            if (!Enum.TryParse<ImportantDateType>(args[3], true, out var dateType))
            {
                Console.WriteLine($"无效的重要日类型: {args[3]}");
                Console.WriteLine("可用类型: exam, contest, language, professional, admission, other");
                return 1;
            }

            if (!Enum.TryParse<ImportanceLevel>(args[4], true, out var level))
            {
                Console.WriteLine($"无效的重要级别: {args[4]}");
                Console.WriteLine("可用级别: normal, important, veryimportant, critical");
                return 1;
            }

            // 解析日期 - 支持 YYYY-MM-DD 和 MM-DD 格式
            DateTime targetDate;
            string dateStr = args[5];

            if (dateStr.Contains('-') && dateStr.Split('-').Length == 3)
            {
                // YYYY-MM-DD 格式
                if (!DateTime.TryParse(dateStr, out targetDate))
                {
                    Console.WriteLine($"无效的日期格式: {dateStr}");
                    Console.WriteLine("请使用 YYYY-MM-DD 或 MM-DD 格式");
                    return 1;
                }
            }
            else if (dateStr.Contains('-') && dateStr.Split('-').Length == 2)
            {
                // MM-DD 格式，默认为当前年
                if (DateTime.TryParseExact(dateStr, "MM-dd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    targetDate = new DateTime(DateTime.Now.Year, parsedDate.Month, parsedDate.Day);
                }
                else
                {
                    Console.WriteLine($"无效的日期格式: {dateStr}");
                    Console.WriteLine("请使用 YYYY-MM-DD 或 MM-DD 格式");
                    return 1;
                }
            }
            else
            {
                Console.WriteLine($"无效的日期格式: {dateStr}");
                Console.WriteLine("请使用 YYYY-MM-DD 或 MM-DD 格式");
                return 1;
            }

            // 解析可选参数
            string description = "";
            List<int> remindDays = new List<int>();

            for (int i = 6; i < args.Length; i++)
            {
                if (args[i].StartsWith("--remind="))
                {
                    var remindStr = args[i][9..];
                    remindDays = remindStr.Split(',')
                        .Select(d => int.TryParse(d.Trim(), out int day) ? day : 0)
                        .Where(d => d > 0)
                        .ToList();
                }
                else if (string.IsNullOrEmpty(description) && !args[i].StartsWith("--"))
                {
                    description = args[i];
                }
                else if (!args[i].StartsWith("--"))
                {
                    description += " " + args[i];
                }
            }

            // 如果有提醒天数，添加到描述中
            if (remindDays.Any())
            {
                description += $" [提醒: {string.Join(",", remindDays)}天前]";
            }

            // 创建重要日
            var importantDate = new ImportantDate(
                args[2], 
                dateType, 
                level, 
                targetDate.Year,
                targetDate.Month, 
                targetDate.Day, 
                description.Trim()
            );

            // 添加到自定义重要日
            var customDates = ImportantDateService.LoadCustomImportantDates();
            customDates.Add(importantDate);
            ImportantDateService.SaveCustomImportantDates(customDates);

            Console.WriteLine($"重要日 '{args[2]}' 已添加 ({targetDate:yyyy-MM-dd})");
            return 0;
        }
    }
}