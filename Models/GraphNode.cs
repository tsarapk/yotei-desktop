using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using YoteiLib.Core;

namespace YoteiTasks.Models;




public class GraphNode : INotifyPropertyChanged
{
    private string _id;
    private string _label;
    private double _x;
    private double _y;
    

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    
    public TaskNode? TaskNode { get; set; }
    
    public List<TaskResourceUsage> ResourceUsages { get; set; } = new();

    public GraphNode(string id, string label, double x = 0, double y = 0)
    {
        _id = id;
        _label = label;
        _x = x;
        _y = y;
    }

    
    public void SyncToTaskNode()
    {
        if (TaskNode != null)
        {
            TaskNode.SetTitle(Label);
        }
    }

    public void SyncFromTaskNode()
    {
        if (TaskNode != null)
        {
            Label = TaskNode.Title;
        }
    }

    
    
    
    
    public void RaiseVisualChanged()
    {
        OnPropertyChanged(nameof(Label));
    }


    public virtual void OnSelect()
    {
        
    }

 
    public virtual void OnDeselect()
    {
        Console.WriteLine("OnDeselect");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

