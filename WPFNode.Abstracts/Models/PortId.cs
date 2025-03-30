using System;

namespace WPFNode.Models;

public readonly record struct PortId(Guid NodeId, bool IsInput, string Name) {
    public override string ToString() {
        return $"{NodeId}:{(IsInput ? "in" : "out")}[{Name}]";
    }
}