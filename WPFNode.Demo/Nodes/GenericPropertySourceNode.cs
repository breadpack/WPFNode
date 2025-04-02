using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties; // Required for GenericNodeProperty

namespace WPFNode.Demo.Nodes;

[NodeCategory("Demo")]
[NodeName("Generic Property Source")]
[NodeDescription("Provides a value from a GenericNodeProperty.")]
public class GenericPropertySourceNode : NodeBase {
    [NodeProperty("Source Value", CanConnectToPort = false, ConnectionStateChangedCallback = nameof(SourceValueConnectionStateChanged))]                      // Not connectable
    public GenericNodeProperty SourceValueProperty { get; private set; } = null!; // Initialized in InitializeFromAttributes

    private IOutputPort _outputPort;

    public GenericPropertySourceNode(INodeCanvas canvas, Guid guid)
        : base(canvas, guid) { }

    public void SourceValueConnectionStateChanged(IInputPort port) {
        ReconfigurePorts();
    }

    protected override void Configure(NodeBuilder builder) {
        var type = SourceValueProperty.ConnectedType ?? typeof(object);
        _outputPort = builder.Output("Output", type);
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken  cancellationToken = default
    ) {
        // Set the output port value from the property's value
        _outputPort.Value = SourceValueProperty.Value;

        // No flow ports in this simple example
        await Task.CompletedTask;
        yield break;
    }
}