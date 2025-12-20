using System;
using System.Collections.Generic;
using YoteiLib.Core;

namespace YoteiTasks.Models;




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




public class TaskResourceAllocation
{
    public string TaskName { get; set; } = string.Empty;
    public string TaskId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public double Percentage { get; set; }
}
