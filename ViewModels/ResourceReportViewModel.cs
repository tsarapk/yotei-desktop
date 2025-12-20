using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using YoteiLib.Core;
using YoteiTasks.Models;

namespace YoteiTasks.ViewModels;




public class ResourceReportViewModel : INotifyPropertyChanged
{
    private readonly ResourceUsageReport _report;
    private readonly Action? _closeCallback;

    public ResourceReportViewModel(ResourceUsageReport report, Action? closeCallback = null)
    {
        _report = report;
        _closeCallback = closeCallback;
        
        TaskAllocations = new ObservableCollection<TaskAllocationViewModel>(
            report.TaskAllocations.Select(ta => new TaskAllocationViewModel(ta))
        );
        
        CloseCommand = new RelayCommand(_ => _closeCallback?.Invoke());
    }

    public string ResourceName => _report.Resource.Name;
    public double TotalAvailable => _report.TotalAvailable;
    public double TotalAllocated => _report.TotalAllocated;
    public double UsagePercentage => _report.UsagePercentage;
    public double RemainingAmount => TotalAvailable - TotalAllocated;
    public string UsageStatus => UsagePercentage > 100 ? "⚠️ Перерасход" : 
                                 UsagePercentage > 80 ? "⚡ Высокая загрузка" : 
                                 "✓ Нормальная загрузка";
    
    public ObservableCollection<TaskAllocationViewModel> TaskAllocations { get; }
    
    public ICommand CloseCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}




public class TaskAllocationViewModel : INotifyPropertyChanged
{
    private readonly TaskResourceAllocation _allocation;

    public TaskAllocationViewModel(TaskResourceAllocation allocation)
    {
        _allocation = allocation;
    }

    public string TaskName => _allocation.TaskName;
    public double Amount => _allocation.Amount;
    public double Percentage => _allocation.Percentage;
    public string PercentageText => $"{Percentage:F1}%";

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
