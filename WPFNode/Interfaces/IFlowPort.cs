using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IFlowInPort : IInputPort
{
    // Flow In 포트는 하나의 연결만 가능
    IConnection? Connection { get; }
}

public interface IFlowOutPort : IOutputPort
{
    // Flow Out 포트는 여러 Flow In 포트에 연결 가능
    IEnumerable<IFlowInPort> ConnectedFlowPorts { get; }
}
