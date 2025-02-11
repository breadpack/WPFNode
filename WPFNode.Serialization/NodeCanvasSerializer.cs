using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WPFNode.Core.Models;

namespace WPFNode.Serialization;

public class NodeCanvasSerializer : INodeCanvasSerializer
{
    private readonly JsonSerializerOptions _options;

    public NodeCanvasSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            ReferenceHandler = ReferenceHandler.Preserve,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<string> SerializeAsync(NodeCanvas canvas)
    {
        return await Task.Run(() => JsonSerializer.Serialize(canvas, _options));
    }

    public async Task<NodeCanvas?> DeserializeAsync(string json)
    {
        return await Task.Run(() => JsonSerializer.Deserialize<NodeCanvas>(json, _options));
    }
} 