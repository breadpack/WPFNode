namespace WPFNode.Models.Serialization;

public class DeserializationResult
{
    public NodeCanvas                 Canvas    { get; }
    public List<DeserializationError> Errors    { get; } = new();
    public bool                       HasErrors => Errors.Any();

    public DeserializationResult(NodeCanvas canvas)
    {
        Canvas = canvas;
    }

    public void AddError(string elementType, string elementId, string message, string details, Exception exception)
    {
        Errors.Add(new DeserializationError(elementType, elementId, message, details, exception));
    }

    public string GetErrorSummary()
    {
        if (!HasErrors) return "성공적으로 로드되었습니다.";

        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"{Errors.Count}개 항목 로드 실패:");
        
        var nodeErrors       = Errors.Where(e => e.ElementType == "Node").ToList();
        var connectionErrors = Errors.Where(e => e.ElementType == "Connection").ToList();
        var groupErrors      = Errors.Where(e => e.ElementType == "Group").ToList();

        if (nodeErrors.Any())
            summary.AppendLine($"- {nodeErrors.Count}개 노드 실패");
        
        if (connectionErrors.Any())
            summary.AppendLine($"- {connectionErrors.Count}개 연결 실패");
        
        if (groupErrors.Any())
            summary.AppendLine($"- {groupErrors.Count}개 그룹 실패");

        return summary.ToString();
    }
}