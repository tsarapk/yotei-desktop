namespace YoteiTasks.Models;




public class GraphEdge
{
    public string SourceId { get; set; }
    public string TargetId { get; set; }

    public GraphEdge(string sourceId, string targetId)
    {
        SourceId = sourceId;
        TargetId = targetId;
    }
}













