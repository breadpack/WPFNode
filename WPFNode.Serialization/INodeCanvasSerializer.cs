using System.Threading.Tasks;
using WPFNode.Core.Models;

namespace WPFNode.Serialization;

public interface INodeCanvasSerializer
{
    Task<string> SerializeAsync(NodeCanvas canvas);
    Task<NodeCanvas?> DeserializeAsync(string json);
} 