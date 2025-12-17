using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using YoteiLib.Core;
using YoteiTasks.Models;
using YoteiTasks.ValueObjects;

namespace YoteiTasks.Services;


public class NodeRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<string, Border> _nodeShapes = new();
    private readonly Dictionary<string, TextBlock> _nodeLabels = new();

    public NodeRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void AddNode(GraphNode node)
    {
        if (_nodeShapes.ContainsKey(node.Id))
            return;

        var border = CreateNodeBorder(node);
        var textBlock = CreateNodeLabel(node);

        _nodeShapes[node.Id] = border;
        _nodeLabels[node.Id] = textBlock;
        border.Child = textBlock;

        SetNodePosition(border, node);
        _canvas.Children.Add(border);
    }

    public void RemoveNode(string nodeId)
    {
        if (_nodeShapes.TryGetValue(nodeId, out var border))
        {
            _canvas.Children.Remove(border);
            _nodeShapes.Remove(nodeId);
        }

        if (_nodeLabels.TryGetValue(nodeId, out var textBlock))
        {
            _nodeLabels.Remove(nodeId);
        }
    }

    public void UpdateNodePosition(GraphNode node)
    {
        if (_nodeShapes.TryGetValue(node.Id, out var border))
        {
            SetNodePosition(border, node);
        }
    }

    public void RefreshNodeVisualization(GraphNode node)
    {
        if (_nodeShapes.TryGetValue(node.Id, out var border))
        {
            UpdateNodeBorder(border, node);
        }

        if (_nodeLabels.TryGetValue(node.Id, out var textBlock))
        {
            UpdateNodeLabel(textBlock, node);
        }
    }

    public Border? GetNodeShape(string nodeId)
    {
        return _nodeShapes.TryGetValue(nodeId, out var shape) ? shape : null;
    }

    public void Clear()
    {
        foreach (var border in _nodeShapes.Values)
        {
            _canvas.Children.Remove(border);
        }
        _nodeShapes.Clear();
        _nodeLabels.Clear();
    }

    private Border CreateNodeBorder(GraphNode node)
    {
        var borderBrush = GetNodeBorderBrush(node);
        var background = GetNodeBackground(node);
        var opacity = GetNodeOpacity(node);
        
        return new Border
        {
            Background = background,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            Opacity = opacity,
            CornerRadius = new CornerRadius(4)
        };
    }

    private TextBlock CreateNodeLabel(GraphNode node)
    {
        var labelText = GetNodeLabelText(node);
        
        return new TextBlock
        {
            Text = labelText,
            Foreground = Brushes.White,
            FontSize = 12,
            TextAlignment = TextAlignment.Left,
            ZIndex = 2,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.WrapWithOverflow,
            Padding = new Thickness(15),
            MaxWidth = 200,
            MaxHeight = 160
        };
    }

    private void UpdateNodeBorder(Border border, GraphNode node)
    {
        if (node.TaskNode != null)
        {
            border.BorderBrush = GetNodeBorderBrush(node);
            border.Background = GetNodeBackground(node);
            border.Opacity = GetNodeOpacity(node);
            border.InvalidateMeasure();
        }
    }

    private void UpdateNodeLabel(TextBlock textBlock, GraphNode node)
    {
        textBlock.Text = GetNodeLabelText(node);
        textBlock.InvalidateMeasure();
    }

    private IBrush GetNodeBorderBrush(GraphNode node)
    {
        if (node.TaskNode == null)
            return Brushes.DarkBlue;

        return node.TaskNode.Status switch
        {
            TaskStatus.Completed => LocalColors.NodeOutlineCompleted,
            TaskStatus.InProgress => LocalColors.NodeOutlineInProgress,
            TaskStatus.Queued => LocalColors.NodeOutlineQueued,
            TaskStatus.Canceled => LocalColors.NodeOutlineCanceled,
            _ => Brushes.DarkBlue
        };
    }

    private string GetNodeLabelText(GraphNode node)
    {
        var labelText = node.Label;
        if (node.TaskNode != null && node.TaskNode.Priority > 0)
        {
            labelText = $"[{node.TaskNode.Priority}] {labelText}";
        }
        
        // Добавляем индикатор завершенной задачи
        if (node.TaskNode != null && node.TaskNode.IsCompleted)
        {
            labelText = $"✓ {labelText}";
        }
        
        return labelText;
    }

    private IBrush GetNodeBackground(GraphNode node)
    {
        if (node.TaskNode == null)
            return LocalColors.NodeBackDefault;

        // Для завершенных задач используем более темный фон
        if (node.TaskNode.IsCompleted)
            return new SolidColorBrush(Color.FromRgb(40, 40, 40));

        return LocalColors.NodeBackDefault;
    }

    private double GetNodeOpacity(GraphNode node)
    {
        if (node.TaskNode == null)
            return 1.0;

        // Завершенные задачи отображаются полупрозрачными (неактивными)
        if (node.TaskNode.IsCompleted)
            return 0.6;

        return 1.0;
    }

    private void SetNodePosition(Border border, GraphNode node)
    {
        Canvas.SetLeft(border, node.X - 30);
        Canvas.SetTop(border, node.Y - 30);
    }
}









