using System;
using System.Collections.Generic;
using YoteiLib.Core;

namespace YoteiTasks.Models;

/// <summary>
/// Report on resource usage across tasks
/// </summary>
public class ResourceUsageReport
{
    public Resource Resource { get; set; }
    public double TotalAllocated { get; set; }
    public double TotalAvailable { get; set; }
    public double UsagePercentage => TotalAvailable > 0 ? (TotalAllocated / TotalAvailable) * 100 : 0;
    public List<TaskResourceAllocation> TaskAllocations { get; set; } = new();

    public ResourceUsageReport(Resource resource)
    {
        Resource = resource;
        TotalAvailable = resource.Value;
    }
}

/// <summary>
/// Represents allocation of a resource to a specific task
/// </summary>
public class TaskResourceAllocation
{
    public string TaskName { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double Percentage { get; set; }
}
