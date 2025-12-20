using System;

namespace YoteiTasks.Models;




public enum RecurrenceType
{
    None,           
    Minutes,        
    Hours,          
    Daily,          
    Weekly,         
    Monthly,        
    Custom          
}




public class RecurringTaskConfig
{
    public string NodeId { get; set; } = string.Empty;
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public int Interval { get; set; } = 1; 
    public bool AutoReset { get; set; } = false; 
    public TimeSpan? AutoResetDelay { get; set; } 
    public bool NotificationsEnabled { get; set; } = true;
    public TimeSpan? NotificationAdvance { get; set; } 
    public DateTimeOffset? LastNotification { get; set; }
    public DateTimeOffset? LastReset { get; set; }
    public DateTimeOffset? NextDueDate { get; set; }
    
    public RecurringTaskConfig() { }
    
    public RecurringTaskConfig(string nodeId)
    {
        NodeId = nodeId;
    }

    
    
    
    public DateTimeOffset CalculateNextDueDate(DateTimeOffset from)
    {
        return RecurrenceType switch
        {
            RecurrenceType.Minutes => from.AddMinutes(Interval),
            RecurrenceType.Hours => from.AddHours(Interval),
            RecurrenceType.Daily => from.AddDays(Interval),
            RecurrenceType.Weekly => from.AddDays(Interval * 7),
            RecurrenceType.Monthly => from.AddMonths(Interval),
            _ => from
        };
    }

    
    
    
    public bool ShouldNotify(DateTimeOffset now)
    {
        if (!NotificationsEnabled || NextDueDate == null)
            return false;

        
        if (LastNotification.HasValue && (now - LastNotification.Value).TotalMinutes < 1)
            return false;

        
        if (NotificationAdvance.HasValue)
        {
            var notifyTime = NextDueDate.Value - NotificationAdvance.Value;
            return now >= notifyTime && now < NextDueDate.Value;
        }

        
        return now >= NextDueDate.Value;
    }

    
    
    
    public bool ShouldReset(DateTimeOffset now, bool isCompleted)
    {
        if (!AutoReset || !isCompleted || !AutoResetDelay.HasValue)
            return false;

        if (!LastReset.HasValue)
            return false;

        var resetTime = LastReset.Value + AutoResetDelay.Value;
        return now >= resetTime;
    }
}
