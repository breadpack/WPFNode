using System.Text.Json;
using WPFNode.Interfaces;
using WPFNode.Models.Serialization;

namespace WPFNode.Models;

/// <summary>
/// NodeCanvas의 JSON 처리 확장 기능 구현 
/// </summary>
public partial class NodeCanvas
{
    /// <summary>
    /// JSON 문자열로부터 단일 노드를 생성합니다.
    /// </summary>
    /// <param name="json">노드 JSON 문자열</param>
    /// <param name="offsetX">X 좌표 오프셋</param>
    /// <param name="offsetY">Y 좌표 오프셋</param>
    /// <returns>생성된 노드</returns>
    public INode? CreateNodeFromJson(string json, double offsetX = 20, double offsetY = 20)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var element = document.RootElement;
            
            // 1. 타입 정보 추출
            if (!element.TryGetProperty("Type", out var typeElement))
                return null;
                
            var typeName = typeElement.GetString();
            if (string.IsNullOrEmpty(typeName))
                return null;
                
            var nodeType = Type.GetType(typeName);
            if (nodeType == null || !typeof(NodeBase).IsAssignableFrom(nodeType))
                return null;
            
            // 2. 위치 정보 추출 (원래 위치에서 오프셋 적용)
            double x = 0, y = 0;
            if (element.TryGetProperty("X", out var xElement))
                x = xElement.GetDouble() + offsetX;
            if (element.TryGetProperty("Y", out var yElement))
                y = yElement.GetDouble() + offsetY;
            
            // 3. 새 노드 생성 (Guid는 새로 생성됨)
            var newNode = CreateNode(nodeType, x, y);
            
            // 4. 프로퍼티와 기타 정보 복원
            if (newNode is IJsonSerializable serializableNode)
            {
                serializableNode.ReadJson(element, NodeCanvasJsonConverter.SerializerOptions);
            }
            
            return newNode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"노드 생성 중 오류: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// JSON 배열로부터 여러 노드를 생성합니다.
    /// </summary>
    /// <param name="jsonArray">노드 JSON 배열</param>
    /// <param name="offsetX">X 좌표 오프셋</param>
    /// <param name="offsetY">Y 좌표 오프셋</param>
    /// <returns>생성된 노드 목록</returns>
    public IEnumerable<INode> CreateNodesFromJson(string jsonArray, double offsetX = 20, double offsetY = 20)
    {
        var result = new List<INode>();
        
        try
        {
            using var document = JsonDocument.Parse(jsonArray);
            var rootElement = document.RootElement;
            
            if (rootElement.ValueKind != JsonValueKind.Array)
                return result;
                
            foreach (var element in rootElement.EnumerateArray())
            {
                var nodeJson = element.ToString();
                var newNode = CreateNodeFromJson(nodeJson, offsetX, offsetY);
                if (newNode != null)
                {
                    result.Add(newNode);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"노드 일괄 생성 중 오류: {ex.Message}");
        }
        
        return result;
    }
}
