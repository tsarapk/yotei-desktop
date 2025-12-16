using Avalonia.Media;

namespace YoteiTasks.ValueObjects;

public class LocalColors
{
    public static readonly IBrush NodeBackDefault = new SolidColorBrush(Color.Parse("#28262E"));
    public static readonly IBrush NodeBackSelected = new SolidColorBrush(Color.Parse("#383540"));
    
    public static readonly IBrush NodeOutlineCompleted = Brushes.Green;
    public static readonly IBrush NodeOutlineInProgress = Brushes.Orange;
    public static readonly IBrush NodeOutlineQueued = Brushes.Yellow;
    public static readonly IBrush NodeOutlineCanceled = Brushes.Red;
}