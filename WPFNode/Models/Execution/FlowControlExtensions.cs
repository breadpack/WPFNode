using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using WPFNode.Interfaces;

namespace WPFNode.Models.Execution
{
    /// <summary>
    /// Flow 제어를 위한 확장 메서드들을 제공합니다.
    /// </summary>
    public static class FlowControlExtensions
    {
    // 특수 루프백 마커 클래스
    private class LoopBackMarker : IFlowOutPort, INotifyPropertyChanged
    {
        public INode Node { get; }
        public string Name => "LoopBack";
        public PortId Id => new(Node.Guid, false, Name);
        public bool IsInput => false;
        public bool IsConnected => false;
        public bool IsVisible { get; set; } = false;
        public Type DataType => typeof(void);
        public IReadOnlyList<IConnection> Connections => Array.Empty<IConnection>();
        public IEnumerable<IFlowInPort> ConnectedFlowPorts => Enumerable.Empty<IFlowInPort>();
        public object? Value { get; set; } = null;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void Disconnect() { }
        public void AddConnection(IConnection connection) { }
        public void RemoveConnection(IConnection connection) { }
        public bool CanConnectTo(IInputPort targetPort) => false;
        public IConnection Connect(IInputPort target) => throw new NotSupportedException("루프백 마커는 연결할 수 없습니다.");
        public void Disconnect(IInputPort target) { }
        public int GetPortIndex() => -1;
        
        public void WriteJson(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", Name);
            writer.WriteString("Type", "LoopBack");
            writer.WriteEndObject();
        }
        
        public void ReadJson(JsonElement element, JsonSerializerOptions options)
        {
            // 읽기는 지원하지 않음
        }
        
        public LoopBackMarker(INode node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }
    }
        
        /// <summary>
        /// 현재 노드를 다시 실행하도록 요청하는 특수 제어 신호를 생성합니다.
        /// </summary>
        public static IFlowOutPort LoopBack(this NodeBase node)
        {
            return new LoopBackMarker(node);
        }
        
        /// <summary>
        /// 주어진 포트가 루프백 신호인지 확인합니다.
        /// </summary>
        public static bool IsLoopBackSignal(this IFlowOutPort port)
        {
            return port is LoopBackMarker;
        }
    }
}
