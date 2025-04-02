using System.Collections.Generic;
using WPFNode.Models;

namespace WPFNode.Interfaces;

public interface IFlowInPort : IPort {
    // Flow In 포트는 하나의 연결만 가능
    IConnection? Connection { get; }
    IConnection  Connect(IFlowOutPort source);
}

public interface IFlowOutPort : IPort {
    // Flow Out 포트는 여러 Flow In 포트에 연결 가능
    IEnumerable<IFlowInPort> ConnectedFlowPorts { get; }
    IConnection              Connect(IFlowInPort target);
}