using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using YoteiTasks.Models;
using YoteiTasks.Services;
using YoteiTasks.ViewModels;

namespace YoteiTasks.Views;

public partial class GraphCanvas : UserControl
{
    public static readonly StyledProperty<Graph?> GraphProperty =
        AvaloniaProperty.Register<GraphCanvas, Graph?>(nameof(Graph));

    private NodeRenderer? _nodeRenderer;
    private EdgeRenderer? _edgeRenderer;
    private SelectionManager? _selectionManager;
    private NodeInteractionHandler? _interactionHandler;
    private NodeFactory? _nodeFactory;
    private GraphVisualizationService? _visualizationService;

    public Graph? Graph
    {
        get => GetValue(GraphProperty);
        set => SetValue(GraphProperty, value);
    }

    public GraphCanvas()
    {
        InitializeComponent();
        GraphProperty.Changed.AddClassHandler<GraphCanvas>((x, e) => x.OnGraphChanged(e.NewValue as Graph));
        
        MainCanvas.PointerPressed += OnCanvasPointerPressed;
        
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        InitializeServices();
    }

 

    private void InitializeServices()
    {
        if (_nodeRenderer != null)
            return; 

        var mainViewModel = DataContext as MainViewModel;
        
        _nodeRenderer = new NodeRenderer(MainCanvas);
        _edgeRenderer = new EdgeRenderer(MainCanvas);
        _selectionManager = new SelectionManager(_nodeRenderer);
        _nodeFactory = new NodeFactory(mainViewModel);
        _interactionHandler = new NodeInteractionHandler(
            MainCanvas,
            _nodeRenderer,
            _edgeRenderer,
            _selectionManager,
            _nodeFactory,
            mainViewModel);
        
        _visualizationService = new GraphVisualizationService(
            _nodeRenderer,
            _edgeRenderer,
            _interactionHandler);

        _selectionManager.SetMainViewModel(mainViewModel);
        _interactionHandler.SetOnNodeRightClick(ShowNodeOperationsMenu);
        
        
        if (Graph != null)
        {
            _visualizationService.SetGraph(Graph);
        }
    }

    private void OnGraphChanged(Graph? newGraph)
    {
        _visualizationService?.SetGraph(newGraph);
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var hitTestResult = e.GetCurrentPoint(MainCanvas);
        var clickedElement = MainCanvas.InputHitTest(hitTestResult.Position);
        

        if (clickedElement is Border)
        {
            return;
        }
        
        
       
        
        var mainViewModel = DataContext as MainViewModel;

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            
            if (mainViewModel?.CanCreateTasks == true)
            {
                var pos = e.GetPosition(MainCanvas);
                ShowAddNodeMenu(pos);
                e.Handled = true;
            }
            else if (mainViewModel != null)
            {
                Services.NotificationService.Instance.ShowWarning("У вас недостаточно привилегий для создания задач");
                e.Handled = true;
            }
        }
        
        
        if (e.ClickCount == 2 && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            
            if (mainViewModel?.CanCreateTasks == true)
            {
                var pos = e.GetPosition(MainCanvas);
                AddNodeAtPosition(pos);
                e.Handled = true;
            }
            else if (mainViewModel != null)
            {
                Services.NotificationService.Instance.ShowWarning("У вас недостаточно привилегий для создания задач");
                e.Handled = true;
            }
        }
    }

    private GraphNode? AddNodeAtPosition(Avalonia.Point position)
    {
        var mainViewModel = DataContext as MainViewModel;
        if (mainViewModel?.CanCreateTasks != true)
            return null;
            
        return _nodeFactory?.CreateNodeAtPosition(position, Graph);
    }
}
