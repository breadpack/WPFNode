using System;
using WPFNode.Abstractions;

namespace WPFNode.Core.Models;

public class OutputPort<T> : PortBase, IOutputPort
{
    public OutputPort(string name, INode node) : base(name, typeof(T), false, node)
    {
    }

    public bool CanConnectTo(IInputPort targetPort)
    {
        // 같은 노드의 포트와는 연결 불가
        if (targetPort.Node == Node) return false;

        if (IsInput == targetPort.IsInput) return false;

        return targetPort.CanAcceptType(DataType);
    }

    public void SetValue(object? value) {
        Value = (T?)value;
    }
    public object? GetValue() {
        return Value;
    }
} 