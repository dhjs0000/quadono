using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Quadono
{
    public class TaskItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public int Quadrant { get; set; }
        public int EstimateMinutes { get; set; }
        public bool Done { get; set; }
        public TaskStatus Status { get; set; } = TaskStatus.Pending;
        public DateTime? StartTime { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public enum TaskStatus
    {
        Pending,
        InProgress,
        Paused,
        Completed
    }

    public static class TaskStore
    {
        static readonly string FileName = "quadono.json";
        static List<TaskItem> _cache = new();
        static readonly object _lock = new();

        static TaskStore()
        {
            if (File.Exists(FileName))
            {
                var json = File.ReadAllText(FileName);
                _cache = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new();
            }
        }

        static void Save()
        {
            lock (_lock)
            {
                File.WriteAllText(FileName, JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public static void Add(string title, int quadrant, int minutes, string? description = null, DateTime? dueDate = null)
        {
            lock (_lock) _cache.Add(new TaskItem { 
                Title = title, 
                Quadrant = quadrant, 
                EstimateMinutes = minutes,
                Description = description,
                DueDate = dueDate
            });
            Save();
        }

        public static void Edit(string id, string? title = null, int? quadrant = null, int? minutes = null, string? description = null)
        {
            lock (_lock)
            {
                var task = _cache.FirstOrDefault(x =>
                    x.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Title, id, StringComparison.OrdinalIgnoreCase));
                
                if (task != null)
                {
                    if (title != null) task.Title = title;
                    if (quadrant.HasValue) task.Quadrant = quadrant.Value;
                    if (minutes.HasValue) task.EstimateMinutes = minutes.Value;
                    if (description != null) task.Description = description;
                }
            }
            Save();
        }

        public static void Start(string id)
        {
            lock (_lock)
            {
                var task = _cache.FirstOrDefault(x =>
                    x.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Title, id, StringComparison.OrdinalIgnoreCase));
                
                if (task != null && task.Status != TaskStatus.InProgress)
                {
                    task.Status = TaskStatus.InProgress;
                    task.StartTime = DateTime.Now;
                }
            }
            Save();
        }

        public static void Pause(string id)
        {
            lock (_lock)
            {
                var task = _cache.FirstOrDefault(x =>
                    x.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Title, id, StringComparison.OrdinalIgnoreCase));
                
                if (task != null && task.Status == TaskStatus.InProgress)
                {
                    task.Status = TaskStatus.Paused;
                }
            }
            Save();
        }

        public static void List(int? quadrant = null, TaskStatus? status = null, bool? today = null, bool showAll = false)
        {
            // 重新加载，保证与磁盘一致
            lock (_lock)
            {
                if (File.Exists(FileName))
                    _cache = JsonSerializer.Deserialize<List<TaskItem>>(File.ReadAllText(FileName)) ?? new();
                else
                    _cache = new();

                var query = _cache.AsQueryable();
                
                if (!showAll)
                    query = query.Where(t => !t.Done);
                
                if (quadrant.HasValue)
                    query = query.Where(t => t.Quadrant == quadrant.Value);
                
                if (status.HasValue)
                    query = query.Where(t => t.Status == status.Value);
                
                if (today.HasValue && today.Value)
                {
                    var todayStart = DateTime.Today;
                    var tomorrowStart = todayStart.AddDays(1);
                    query = query.Where(t => t.DueDate.HasValue && t.DueDate.Value >= todayStart && t.DueDate.Value < tomorrowStart);
                }

                var tasks = query.ToList();
                if (!tasks.Any())
                {
                    Console.WriteLine("没有找到符合条件的任务。");
                    return;
                }

                var groups = tasks.GroupBy(t => t.Quadrant).OrderBy(g => g.Key);
                foreach (var g in groups)
                {
                    Console.WriteLine($"【第 {g.Key} 象限】");
                    foreach (var t in g)
                    {
                        var statusText = GetStatusText(t.Status);
                        var dueText = t.DueDate.HasValue ? $" 截止:{t.DueDate.Value:MM-dd}" : "";
                        var descText = !string.IsNullOrEmpty(t.Description) ? $" ({t.Description})" : "";
                        
                        Console.WriteLine($"  {t.Id[..8]}  {t.Title}{descText}  估{t.EstimateMinutes}分钟{statusText}{dueText}");
                    }
                }
            }
        }

        private static string GetStatusText(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.InProgress => " [进行中]",
                TaskStatus.Paused => " [已暂停]",
                TaskStatus.Completed => " [已完成]",
                _ => ""
            };
        }

        public static void Done(string id)
        {
            lock (_lock)
            {
                var t = _cache.FirstOrDefault(x =>
                    x.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Title, id, StringComparison.OrdinalIgnoreCase));
                if (t != null) t.Done = true;
            }
            Save();
        }

        public static void Del(string id)
        {
            lock (_lock)
            {
                _cache.RemoveAll(x =>
                    x.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Title, id, StringComparison.OrdinalIgnoreCase));
            }
            Save();
        }

        public static TaskItem? Find(string id)
        {
            lock (_lock)
                return _cache.FirstOrDefault(x =>
                         x.Id.StartsWith(id, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(x.Title, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}