using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using YoteiTasks.Models;

namespace YoteiTasks.Services;


public class EdgeRenderer
{
    private readonly Canvas _canvas;
    private readonly Dictionary<string, Line> _edgeLines = new();
    private readonly Dictionary<string, Path> _edgeArrows = new();
    private const double NodeWidth = 60;
    private const double NodeHeight = 60;

    public EdgeRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void AddEdge(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode)
    {
        var edgeKey = GetEdgeKey(edge);
        if (_edgeLines.ContainsKey(edgeKey))
            return;

        var line = CreateLine(sourceNode, targetNode);
        var arrow = CreateArrow(line.StartPoint, line.EndPoint, sourceNode, targetNode);

        _edgeLines[edgeKey] = line;
        _edgeArrows[edgeKey] = arrow;

        _canvas.Children.Add(line);
        _canvas.Children.Add(arrow);
    }

    public void RemoveEdge(string edgeKey)
    {
        if (_edgeLines.TryGetValue(edgeKey, out var line))
        {
            _canvas.Children.Remove(line);
            _edgeLines.Remove(edgeKey);
        }

        if (_edgeArrows.TryGetValue(edgeKey, out var arrow))
        {
            _canvas.Children.Remove(arrow);
            _edgeArrows.Remove(edgeKey);
        }
    }

    public void UpdateEdgePosition(GraphEdge edge, GraphNode sourceNode, GraphNode targetNode)
    {
        var edgeKey = GetEdgeKey(edge);
        
        if (_edgeLines.TryGetValue(edgeKey, out var line))
        {
            UpdateLinePosition(line, sourceNode, targetNode);
        }

        if (_edgeArrows.TryGetValue(edgeKey, out var arrow))
        {
            UpdateArrowPosition(arrow, sourceNode, targetNode, line);
        }
    }

    public void UpdateConnectedEdges(GraphNode node, Graph graph)
    {
        foreach (var edge in graph.Edges)
        {
            if (edge.SourceId == node.Id || edge.TargetId == node.Id)
            {
                var sourceNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.SourceId);
                var targetNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.TargetId);

                if (sourceNode != null && targetNode != null)
                {
                    UpdateEdgePosition(edge, sourceNode, targetNode);
                }
            }
        }
    }

    public void RemoveEdgesForNode(string nodeId)
    {
        var edgesToRemove = _edgeLines
            .Where(kvp => kvp.Key.StartsWith(nodeId + "-") || kvp.Key.EndsWith("-" + nodeId))
            .ToList();

        foreach (var kvp in edgesToRemove)
        {
            RemoveEdge(kvp.Key);
        }
    }

    public void Clear()
    {
        foreach (var line in _edgeLines.Values)
        {
            _canvas.Children.Remove(line);
        }

        foreach (var arrow in _edgeArrows.Values)
        {
            _canvas.Children.Remove(arrow);
        }

        _edgeLines.Clear();
        _edgeArrows.Clear();
    }

    private Line CreateLine(GraphNode source, GraphNode target)
    {
        var line = new Line
        {
            Stroke = Brushes.Gray,
            StrokeThickness = 2,
            ZIndex = 0
        };

        UpdateLinePosition(line, source, target);
        return line;
    }

    private void UpdateLinePosition(Line line, GraphNode source, GraphNode target)
    {
        var sourceToTarget = new Avalonia.Point(target.X - source.X, target.Y - source.Y);
        var targetToSource = new Avalonia.Point(source.X - target.X, source.Y - target.Y);

        line.StartPoint = GetIntersectionWithRectangle(
            new Avalonia.Point(source.X, source.Y),
            sourceToTarget,
            NodeWidth,
            NodeHeight
        );

        line.EndPoint = GetIntersectionWithRectangle(
            new Avalonia.Point(target.X, target.Y),
            targetToSource,
            NodeWidth,
            NodeHeight
        );
    }

    private Path CreateArrow(Avalonia.Point lineStart, Avalonia.Point lineEnd, GraphNode source, GraphNode target)
    {
        var midPoint = new Avalonia.Point(
            (lineStart.X + lineEnd.X) / 2,
            (lineStart.Y + lineEnd.Y) / 2
        );

        var dx = lineEnd.X - lineStart.X;
        var dy = lineEnd.Y - lineStart.Y;
        var angle = Math.Atan2(dy, dx);

        var arrowLength = 15;
        var arrowWidth = 10;

        var tip = new Avalonia.Point(
            midPoint.X + arrowLength * 0.5 * Math.Cos(angle),
            midPoint.Y + arrowLength * 0.5 * Math.Sin(angle)
        );

        var leftPoint = new Avalonia.Point(
            tip.X - arrowLength * Math.Cos(angle) + arrowWidth * Math.Cos(angle + Math.PI / 2),
            tip.Y - arrowLength * Math.Sin(angle) + arrowWidth * Math.Sin(angle + Math.PI / 2)
        );

        var rightPoint = new Avalonia.Point(
            tip.X - arrowLength * Math.Cos(angle) + arrowWidth * Math.Cos(angle - Math.PI / 2),
            tip.Y - arrowLength * Math.Sin(angle) + arrowWidth * Math.Sin(angle - Math.PI / 2)
        );

        var pathGeometry = new PathGeometry();
        var pathFigure = new PathFigure
        {
            StartPoint = tip,
            IsClosed = true
        };

        pathFigure.Segments.Add(new LineSegment { Point = leftPoint });
        pathFigure.Segments.Add(new LineSegment { Point = rightPoint });
        pathGeometry.Figures.Add(pathFigure);

        return new Path
        {
            Data = pathGeometry,
            Fill = Brushes.Gray,
            Stroke = Brushes.Gray,
            StrokeThickness = 1,
            ZIndex = 2
        };
    }

    private void UpdateArrowPosition(Path arrow, GraphNode source, GraphNode target, Line line)
    {
        var midPoint = new Avalonia.Point(
            (line.StartPoint.X + line.EndPoint.X) / 2,
            (line.StartPoint.Y + line.EndPoint.Y) / 2
        );

        var dx = line.EndPoint.X - line.StartPoint.X + 5;
        var dy = line.EndPoint.Y - line.StartPoint.Y;
        var angle = Math.Atan2(dy, dx);

        var arrowLength = 15;
        var arrowWidth = 10;

        var tip = new Avalonia.Point(
            midPoint.X + arrowLength * 0.5 * Math.Cos(angle),
            midPoint.Y + arrowLength * 0.5 * Math.Sin(angle)
        );

        var leftPoint = new Avalonia.Point(
            tip.X - arrowLength * Math.Cos(angle) + arrowWidth * Math.Cos(angle + Math.PI / 2),
            tip.Y - arrowLength * Math.Sin(angle) + arrowWidth * Math.Sin(angle + Math.PI / 2)
        );

        var rightPoint = new Avalonia.Point(
            tip.X - arrowLength * Math.Cos(angle) + arrowWidth * Math.Cos(angle - Math.PI / 2),
            tip.Y - arrowLength * Math.Sin(angle) + arrowWidth * Math.Sin(angle - Math.PI / 2)
        );

        if (arrow.Data is PathGeometry pathGeometry && pathGeometry.Figures.Count > 0)
        {
            var pathFigure = pathGeometry.Figures[0];
            if (pathFigure != null)
            {
                pathFigure.StartPoint = tip;

                if (pathFigure.Segments.Count >= 2)
                {
                    if (pathFigure.Segments[0] is LineSegment leftSegment)
                        leftSegment.Point = leftPoint;
                    if (pathFigure.Segments[1] is LineSegment rightSegment)
                        rightSegment.Point = rightPoint;
                }
            }
        }
    }

    private Avalonia.Point GetIntersectionWithRectangle(Avalonia.Point center, Avalonia.Point direction, double width, double height)
    {
        var halfWidth = width / 2;
        var halfHeight = height / 2;

        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        if (length == 0) return center;

        var dx = direction.X / length;
        var dy = direction.Y / length;

        double t1 = double.MaxValue;
        double t2 = double.MaxValue;

        if (Math.Abs(dx) > 0.0001)
        {
            t1 = halfWidth / Math.Abs(dx);
        }

        if (Math.Abs(dy) > 0.0001)
        {
            t2 = halfHeight / Math.Abs(dy);
        }

        var t = Math.Min(t1, t2);

        return new Avalonia.Point(
            center.X + dx * t,
            center.Y + dy * t
        );
    }

    private string GetEdgeKey(GraphEdge edge)
    {
        return edge.SourceId + "-" + edge.TargetId;
    }
}









