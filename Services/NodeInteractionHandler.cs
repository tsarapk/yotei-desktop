using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using YoteiTasks.Adapters;
using YoteiTasks.Models;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Services;




public class NodeInteractionHandler
{
    private readonly Canvas _canvas;
    private readonly NodeRenderer _nodeRenderer;
    private readonly EdgeRenderer _edgeRenderer;
    private readonly SelectionManager _selectionManager;
    private readonly NodeFactory _nodeFactory;
    private readonly MainViewModel? _mainViewModel;
    private System.Action<GraphNode>? _onNodeRightClick;

    private GraphNode? _draggedNode;
    private bool _isDragging;
    private Avalonia.Point _dragStart;
    private Avalonia.Point _nodeStartPosition;

    public NodeInteractionHandler(
        Canvas canvas,
        NodeRenderer nodeRenderer,
        EdgeRenderer edgeRenderer,
        SelectionManager selectionManager,
        NodeFactory nodeFactory,
        MainViewModel? mainViewModel)
    {
        _canvas = canvas;
        _nodeRenderer = nodeRenderer;
        _edgeRenderer = edgeRenderer;
        _selectionManager = selectionManager;
        _nodeFactory = nodeFactory;
        _mainViewModel = mainViewModel;
    }

    public void SetOnNodeRightClick(System.Action<GraphNode> handler)
    {
        _onNodeRightClick = handler;
    }
    public void AttachToNode(GraphNode node, Border border)
    {
        border.PointerPressed += (s, e) => OnNodePressed(node, e);
        border.PointerMoved += (s, e) => OnNodeMoved(node, e);
        border.PointerReleased += (s, e) => OnNodeReleased(node, e);
    }
    private void OnNodePressed(GraphNode node, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed)
        {
            _selectionManager.SelectNode(node);
            
            _isDragging = true;
            _draggedNode = node;
            _dragStart = e.GetPosition(_canvas);
            _nodeStartPosition = new Avalonia.Point(node.X, node.Y);
            e.Handled = true;
        }
        else if (e.GetCurrentPoint(_canvas).Properties.IsRightButtonPressed)
        {
            _onNodeRightClick?.Invoke(node);
            e.Handled = true;
        }
    }

    private void OnNodeMoved(GraphNode node, PointerEventArgs e)
    {
        if (_isDragging && _draggedNode == node)
        {
            var currentPos = e.GetPosition(_canvas);
            var deltaX = currentPos.X - _dragStart.X;
            var deltaY = currentPos.Y - _dragStart.Y;

            var newX = _nodeStartPosition.X + deltaX;
            var newY = _nodeStartPosition.Y + deltaY;

            node.X = newX;
            node.Y = newY;

            YoteiAdapter.SaveNodePosition(node.Id, newX, newY);

            _nodeRenderer.UpdateNodePosition(node);
            
            if (_mainViewModel?.SelectedGraph != null)
            {
                _edgeRenderer.UpdateConnectedEdges(node, _mainViewModel.SelectedGraph);
            }
        }
    }

    private void OnNodeReleased(GraphNode node, PointerReleasedEventArgs e)
    {
        if (_isDragging && _draggedNode == node)
        {
            YoteiAdapter.SaveNodePosition(node.Id, node.X, node.Y);
            
            _isDragging = false;
            _draggedNode = null;
        }
    }
}

