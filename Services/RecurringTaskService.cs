using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.Services;

/// <summary>
/// Service for managing recurring tasks and notifications
/// </summary>
public class RecurringTaskService : IDisposable
{
    private readonly Dictionary<string, RecurringTaskConfig> _recurringTasks = new();
    private readonly Dictionary<string, Timer> _resetTimers = new();
    private readonly NotificationService _notificationService;
    private Timer? _checkTimer;
    private readonly object _lock = new();
    private bool _disposed = false;

    public RecurringTaskService(NotificationService notificationService)
    {
        _notificationService = notificationService;
        
        _checkTimer = new Timer(CheckTasks, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }
    
    /// <summary>
    /// Делегат для выполнения сброса на главном UI-потоке (назначается из ViewModel)
    /// </summary>
    public Action<string>? MainThreadReset { get; set; }
    public void ConfigureRecurringTask(string nodeId, RecurringTaskConfig config)
    {
        lock (_lock)
        {
            Console.WriteLine($"[RecurringTaskService] Настройка повторяющейся задачи: NodeId={nodeId}");
            Console.WriteLine($"  - Тип повторения: {config.RecurrenceType}");
            Console.WriteLine($"  - Интервал: {config.Interval}");
            Console.WriteLine($"  - Автосброс: {config.AutoReset}");
            Console.WriteLine($"  - Задержка автосброса: {config.AutoResetDelay?.TotalMinutes ?? 0} мин");
            Console.WriteLine($"  - Уведомления: {config.NotificationsEnabled}");
            
            _recurringTasks[nodeId] = config;
            
            // Вычисляем первую дату выполнения если её нет
            if (config.NextDueDate == null && config.RecurrenceType != RecurrenceType.None)
            {
                config.NextDueDate = config.CalculateNextDueDate(DateTimeOffset.Now);
                Console.WriteLine($"  - Следующая дата выполнения: {config.NextDueDate}");
            }
        }
    }

    /// <summary>
    /// Remove recurring task configuration
    /// </summary>
    public void RemoveRecurringTask(string nodeId)
    {
        lock (_lock)
        {
            Console.WriteLine($"[RecurringTaskService] Удаление настроек повторения: NodeId={nodeId}");
            _recurringTasks.Remove(nodeId);
        }
    }

    /// <summary>
    /// Get recurring task configuration
    /// </summary>
    public RecurringTaskConfig? GetRecurringTask(string nodeId)
    {
        lock (_lock)
        {
            return _recurringTasks.TryGetValue(nodeId, out var config) ? config : null;
        }
    }

    /// <summary>
    /// Get all recurring tasks
    /// </summary>
    public List<RecurringTaskConfig> GetAllRecurringTasks()
    {
        lock (_lock)
        {
            return _recurringTasks.Values.ToList();
        }
    }

    /// <summary>
    /// Mark task as completed and schedule reset if needed
    /// </summary>
    public void OnTaskCompleted(string nodeId, GraphNode node, TaskRepository repository)
    {
        lock (_lock)
        {
            Console.WriteLine($"[RecurringTaskService] Задача выполнена: NodeId={nodeId}, Label='{node.Label}'");
            
            if (!_recurringTasks.TryGetValue(nodeId, out var config))
            {
                Console.WriteLine($"  - Задача не является повторяющейся");
                return;
            }

            var now = DateTimeOffset.Now;

            // Если задача повторяющаяся, вычисляем следующую дату
            if (config.RecurrenceType != RecurrenceType.None)
            {
                config.NextDueDate = config.CalculateNextDueDate(now);
                Console.WriteLine($"  - Следующая дата выполнения: {config.NextDueDate}");
            }

            // Если включен автосброс, запоминаем время выполнения
            if (config.AutoReset && config.AutoResetDelay.HasValue)
            {
                config.LastReset = now;
                var resetTime = now + config.AutoResetDelay.Value;
                Console.WriteLine($"  - Автосброс включен, задача будет сброшена в: {resetTime}");

                // Планируем одноразовый сброс состояния
                if (_resetTimers.TryGetValue(nodeId, out var existingTimer))
                {
                    existingTimer.Dispose();
                }

                _resetTimers[nodeId] = new Timer(_ =>
                {
                    try
                    {
                        ResetTask(nodeId, node, repository, DateTimeOffset.Now);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[RecurringTaskService] Ошибка при автосбросе задачи {nodeId}: {ex.Message}");
                    }
                }, null, config.AutoResetDelay.Value, Timeout.InfiniteTimeSpan);
            }
        }
    }

    /// <summary>
    /// Check if task should be reset and reset it
    /// </summary>
    public bool CheckAndResetTask(string nodeId, GraphNode node, TaskRepository repository)
    {
        lock (_lock)
        {
            if (!_recurringTasks.TryGetValue(nodeId, out var config))
                return false;

            var now = DateTimeOffset.Now;
            var isCompleted = node.TaskNode?.IsCompleted ?? false;
            
            // Детальное логирование для отладки
            if (config.AutoReset && isCompleted)
            {
                Console.WriteLine($"[RecurringTaskService] 🔍 Проверка сброса: NodeId={nodeId}");
                Console.WriteLine($"  - Задача выполнена: {isCompleted}");
                Console.WriteLine($"  - Автосброс включен: {config.AutoReset}");
                Console.WriteLine($"  - Задержка автосброса: {config.AutoResetDelay?.TotalMinutes ?? 0} мин");
                Console.WriteLine($"  - LastReset установлен: {config.LastReset.HasValue}");
                if (config.LastReset.HasValue)
                {
                    var resetTime = config.LastReset.Value + (config.AutoResetDelay ?? TimeSpan.Zero);
                    Console.WriteLine($"  - Время последнего выполнения: {config.LastReset}");
                    Console.WriteLine($"  - Время сброса должно быть: {resetTime}");
                    Console.WriteLine($"  - Текущее время: {now}");
                    Console.WriteLine($"  - Прошло времени: {(now - config.LastReset.Value).TotalMinutes:F2} мин");
                }
            }
            
            if (config.ShouldReset(now, isCompleted))
            {
                // Сбрасываем задачу (устанавливаем статус InProgress)
                if (node.TaskNode != null && node.TaskNode.IsCompleted)
                {
                    Console.WriteLine($"[RecurringTaskService] ⏰ СБРОС ЗАДАЧИ: NodeId={nodeId}, Label='{node.Label}'");
                    Console.WriteLine($"  - Время сброса: {now}");
                    Console.WriteLine($"  - Задержка была: {config.AutoResetDelay?.TotalMinutes ?? 0} мин");
                    
                    return ResetTask(nodeId, node, repository, now);
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Periodic check for notifications and resets
    /// </summary>
    private void CheckTasks(object? state)
    {
        if (_disposed)
            return;

        try
        {
            var now = DateTimeOffset.Now;
            List<(string nodeId, RecurringTaskConfig config)> tasksToProcess;

            lock (_lock)
            {
                tasksToProcess = _recurringTasks
                    .Select(kvp => (kvp.Key, kvp.Value))
                    .ToList();
            }

            if (tasksToProcess.Count > 0)
            {
                Console.WriteLine($"[RecurringTaskService] 🔄 Периодическая проверка задач: {now:HH:mm:ss}");
                Console.WriteLine($"  - Всего повторяющихся задач: {tasksToProcess.Count}");
            }

            foreach (var (nodeId, config) in tasksToProcess)
            {
                // Проверяем нужно ли отправить уведомление
                if (config.ShouldNotify(now))
                {
                    Console.WriteLine($"  - 🔔 Отправка уведомления для задачи: NodeId={nodeId}");
                    SendNotification(nodeId, config);
                    config.LastNotification = now;
                }

                // Также проверяем автосбросы, если приложение открыто
                if (config.AutoReset && config.AutoResetDelay.HasValue && config.LastReset.HasValue)
                {
                    var shouldReset = now >= config.LastReset.Value + config.AutoResetDelay.Value;
                    if (shouldReset && MainThreadReset != null)
                    {
                        // Запрашиваем сброс на UI-потоке через делегат
                        MainThreadReset(nodeId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RecurringTaskService] ❌ Ошибка при проверке задач: {ex.Message}");
            Console.WriteLine($"  Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Send notification for task
    /// </summary>
    private void SendNotification(string nodeId, RecurringTaskConfig config)
    {
        var message = config.RecurrenceType switch
        {
            RecurrenceType.Minutes => $"Напоминание: задача повторяется каждые {config.Interval} мин",
            RecurrenceType.Hours => $"Напоминание: задача повторяется каждые {config.Interval} ч",
            RecurrenceType.Daily => $"Напоминание: ежедневная задача",
            RecurrenceType.Weekly => $"Напоминание: еженедельная задача",
            RecurrenceType.Monthly => $"Напоминание: ежемесячная задача",
            _ => "Напоминание о задаче"
        };

        Console.WriteLine($"[RecurringTaskService] 📢 Уведомление: {message}");
        _notificationService.ShowWarning(message);
    }

    private bool ResetTask(string nodeId, GraphNode node, TaskRepository repository, DateTimeOffset resetTime)
    {
        lock (_lock)
        {
            if (!_recurringTasks.TryGetValue(nodeId, out var config))
                return false;

            if (node.TaskNode == null || !node.TaskNode.IsCompleted)
                return false;

            void DoReset()
            {
                // Пытаемся снять завершение через репозиторий (сброс IsCompleted + статус)
                var uncompleted = repository.Uncomplete(node.TaskNode.Id);
                if (uncompleted == null)
                {
                    // Фолбек на прямую установку статуса, если репозиторий не смог
                    node.TaskNode.SetStatusSecure(YoteiLib.Core.TaskStatus.InProgress);
                }

                _notificationService.ShowInfo($"Задача '{node.Label}' сброшена");

                // Обновляем время последнего сброса
                config.LastReset = resetTime;

                // Обновляем визуализацию узла
                node.SyncFromTaskNode();
                node.RaiseVisualChanged();

                // Чистим таймер, если он был
                if (_resetTimers.TryGetValue(nodeId, out var timer))
                {
                    timer.Dispose();
                    _resetTimers.Remove(nodeId);
                }
            }

            // Обновляем через UI-поток, чтобы биндинги перерисовались корректно
            if (Dispatcher.UIThread.CheckAccess())
            {
                DoReset();
            }
            else
            {
                Dispatcher.UIThread.Post(DoReset);
            }

            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _checkTimer?.Dispose();
        _checkTimer = null;
    }
}
