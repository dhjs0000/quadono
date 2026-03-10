using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Quadono
{
    public static class PomodoroService
    {
        private static PomodoroState? _currentState;
        private static readonly string HistoryFile = "history.log";
        private static readonly string StatsFile = "pomodoro_stats.json";

        public static async Task Start(string? taskId)
        {
            if (_currentState != null && _currentState.IsRunning)
            {
                Console.WriteLine("当前已有正在运行的番茄钟，请先停止");
                return;
            }

            var task = taskId == null ? null : TaskStore.Find(taskId);
            if (taskId != null && task == null)
            {
                Console.WriteLine("未找到指定任务");
                return;
            }

            _currentState = new PomodoroState
            {
                TaskId = taskId,
                TaskTitle = task?.Title,
                StartTime = DateTime.Now,
                Phase = PomodoroPhase.Work,
                IsRunning = true
            };

            int work = 25 * 60, brk = 5 * 60;
            Console.WriteLine($"【番茄钟】工作 25 分钟，任务：{task?.Title ?? "无"}");
            await CountDown(work, "工作");

            if (_currentState?.IsRunning == true)
            {
                Console.WriteLine("【番茄钟】休息 5 分钟");
                _currentState.Phase = PomodoroPhase.Break;
                await CountDown(brk, "休息");

                if (task != null)
                {
                    TaskStore.Done(task.Id);
                    Console.WriteLine("任务已自动标记完成");
                }

                // 记录完成统计
                RecordCompletion(task?.Title, work / 60);
            }

            _currentState = null;
        }

        public static async Task Stop()
        {
            if (_currentState == null || !_currentState.IsRunning)
            {
                Console.WriteLine("当前没有运行的番茄钟");
                return;
            }

            _currentState.IsRunning = false;
            Console.WriteLine("番茄钟已停止");
            
            // 记录中断的番茄钟
            var duration = (int)(DateTime.Now - _currentState.StartTime).TotalMinutes;
            File.AppendAllText(HistoryFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm} 中断番茄钟 {_currentState.TaskTitle ?? "无任务"} {duration} 分钟\n");
            
            _currentState = null;
        }

        public static void ShowStatus()
        {
            if (_currentState == null || !_currentState.IsRunning)
            {
                Console.WriteLine("当前没有运行的番茄钟");
                return;
            }

            var elapsed = DateTime.Now - _currentState.StartTime;
            var remaining = _currentState.Phase == PomodoroPhase.Work 
                ? TimeSpan.FromMinutes(25) - elapsed 
                : TimeSpan.FromMinutes(5) - elapsed;

            Console.WriteLine($"【番茄钟状态】");
            Console.WriteLine($"阶段: {(_currentState.Phase == PomodoroPhase.Work ? "工作" : "休息")}");
            Console.WriteLine($"任务: {_currentState.TaskTitle ?? "无"}");
            Console.WriteLine($"开始时间: {_currentState.StartTime:HH:mm}");
            Console.WriteLine($"剩余时间: {remaining:mm\\:ss}");
        }

        public static void ShowStats()
        {
            try
            {
                if (!File.Exists(HistoryFile))
                {
                    Console.WriteLine("暂无番茄钟记录");
                    return;
                }

                var lines = File.ReadAllLines(HistoryFile);
                var today = DateTime.Now.Date;
                var todayCount = lines.Count(line => line.StartsWith(today.ToString("yyyy-MM-dd")) && line.Contains("完成任务"));
                var totalCount = lines.Count(line => line.Contains("完成任务"));
                var weekCount = lines.Count(line => 
                {
                    if (!line.Contains("完成任务")) return false;
                    var dateStr = line.Substring(0, 10);
                    if (DateTime.TryParse(dateStr, out var date))
                    {
                        return date >= today.AddDays(-7);
                    }
                    return false;
                });

                Console.WriteLine("【番茄钟统计】");
                Console.WriteLine($"今日完成: {todayCount} 个番茄");
                Console.WriteLine($"本周完成: {weekCount} 个番茄");
                Console.WriteLine($"总计完成: {totalCount} 个番茄");
                
                if (todayCount > 0)
                {
                    Console.WriteLine($"今日专注时间: {todayCount * 25} 分钟");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取统计数据失败: {ex.Message}");
            }
        }

        private static void RecordCompletion(string? taskTitle, int minutes)
        {
            // 写历史日志
            File.AppendAllText(HistoryFile,
                $"{DateTime.Now:yyyy-MM-dd HH:mm} 完成任务 {taskTitle ?? "无"} {minutes} 分钟\n");
            
            // 更新统计数据
            try
            {
                var stats = LoadStats();
                stats.TotalCompleted++;
                stats.LastCompleted = DateTime.Now;
                SaveStats(stats);
            }
            catch
            {
                // 忽略统计错误
            }
        }

        private static PomodoroStats LoadStats()
        {
            try
            {
                if (File.Exists(StatsFile))
                {
                    var json = File.ReadAllText(StatsFile);
                    return System.Text.Json.JsonSerializer.Deserialize<PomodoroStats>(json) ?? new PomodoroStats();
                }
            }
            catch { }
            return new PomodoroStats();
        }

        private static void SaveStats(PomodoroStats stats)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(stats, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StatsFile, json);
            }
            catch { }
        }

        static async Task CountDown(int seconds, string phase)
        {
            var end = DateTime.Now.AddSeconds(seconds);
            while (DateTime.Now < end && (_currentState?.IsRunning == true))
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Console.WriteLine("\n用户提前终止");
                        await Stop();
                        return;
                    }
                }
                var left = end - DateTime.Now;
                Console.Write($"\r剩余 {left:mm\\:ss}  ");
                await Task.Delay(1000);
            }
            
            if (_currentState?.IsRunning == true)
            {
                Console.WriteLine();
                Beep();
            }
        }

        static void Beep()
        {
            try { Console.Beep(800, 300); Console.Beep(1000, 300); } catch { }
        }
    }

    public class PomodoroState
    {
        public string? TaskId { get; set; }
        public string? TaskTitle { get; set; }
        public DateTime StartTime { get; set; }
        public PomodoroPhase Phase { get; set; }
        public bool IsRunning { get; set; }
    }

    public enum PomodoroPhase
    {
        Work,
        Break
    }

    public class PomodoroStats
    {
        public int TotalCompleted { get; set; }
        public DateTime LastCompleted { get; set; }
        public int TodayCompleted => LoadTodayCount();
        public int WeekCompleted => LoadWeekCount();

        private int LoadTodayCount()
        {
            try
            {
                if (!File.Exists("history.log")) return 0;
                var today = DateTime.Now.Date;
                return File.ReadAllLines("history.log")
                    .Count(line => line.StartsWith(today.ToString("yyyy-MM-dd")) && line.Contains("完成任务"));
            }
            catch { return 0; }
        }

        private int LoadWeekCount()
        {
            try
            {
                if (!File.Exists("history.log")) return 0;
                var today = DateTime.Now.Date;
                var weekAgo = today.AddDays(-7);
                return File.ReadAllLines("history.log")
                    .Count(line => 
                    {
                        if (!line.Contains("完成任务")) return false;
                        var dateStr = line.Substring(0, 10);
                        if (DateTime.TryParse(dateStr, out var date))
                        {
                            return date >= weekAgo;
                        }
                        return false;
                    });
            }
            catch { return 0; }
        }
    }
}