using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties; // Required for GenericNodeProperty

namespace WPFNode.Demo.Nodes;

[NodeCategory("Demo")]
[NodeName("Generic Property Display")]
[NodeDescription("Displays the value from a connected port or its internal value using GenericNodeProperty.")]
public class GenericPropertyDisplayNode : NodeBase
{
    // This property can be connected to an output port
    [NodeProperty("Display Value", CanConnectToPort = true)]
    public GenericNodeProperty DisplayValueProperty { get; private set; } = null!; // Initialized in InitializeFromAttributes

    // Output port to provide the resolved type name
    [NodeOutput("Resolved Type Name")]
    public OutputPort<string> ResolvedTypeName { get; private set; } = null!; // Initialized in InitializeFromAttributes

    public GenericPropertyDisplayNode(INodeCanvas canvas, Guid guid)
        : base(canvas, guid)
    {
        // Optional: Add a callback for when the connection state changes
        // DisplayValueProperty.PropertyChanged += HandleDisplayValueConnectionChanged;
    }

    // Example callback (if needed, requires uncommenting above and potentially adjustments in GenericNodeProperty/NodeBase)
    // private void HandleDisplayValueConnectionChanged(object? sender, PropertyChangedEventArgs e)
    // {
    //     if (e.PropertyName == nameof(GenericNodeProperty.IsConnected) || e.PropertyName == nameof(GenericNodeProperty.ConnectedType))
    //     {
    //         UpdateResolvedTypeDisplay();
    //     }
    // }

    // private void UpdateResolvedTypeNameOutput() // Renamed for clarity
    // {
    //     ResolvedTypeName.Value = DisplayValueProperty.DataType?.Name ?? "object"; // Default to "object" if null
    // }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context, CancellationToken cancellationToken = default)
    {
        // Get the value (either from connected port or internal value)
        var value = DisplayValueProperty.Value;
        var resolvedType = DisplayValueProperty.DataType; // Get the dynamically resolved type

        // Log or display the value and type
        Logger?.LogInformation($"GenericPropertyDisplay: Value = {value ?? "null"}, Resolved Type = {resolvedType.Name}");

        // Set the output port value with the resolved type name
        ResolvedTypeName.Value = resolvedType.Name ?? "object"; // Use "object" if type is somehow null

        // No flow ports in this simple example
        await Task.CompletedTask;
        yield break;
    }
}
