using System;
using YoteiLib.Core;

namespace YoteiTasks.Models;




public class TaskFilter
{
    public TaskStatus? Status { get; set; }
    public int? Priority { get; set; }
    public Actor? Performer { get; set; }
    public DateTimeOffset? DateFrom { get; set; }
    public DateTimeOffset? DateTo { get; set; }
    public string? SearchText { get; set; }
    
    public bool IsEmpty => 
        Status == null && 
        Priority == null && 
        Performer == null && 
        DateFrom == null && 
        DateTo == null && 
        string.IsNullOrWhiteSpace(SearchText);
}




public enum TaskSortBy
{
    Name,
    Status,
    Priority,
    Deadline,
    CreatedDate
}




public enum SortDirection
{
    Ascending,
    Descending
}
