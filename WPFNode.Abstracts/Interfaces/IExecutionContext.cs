namespace WPFNode.Interfaces;

public interface IExecutionContext {
    /// <summary>
    /// 현재 활성화된 FlowInPort
    /// </summary>
    IFlowInPort? ActiveFlowInPort { get; }
}