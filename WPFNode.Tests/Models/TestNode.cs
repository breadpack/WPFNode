using System.Threading.Tasks;
using WPFNode.Core.Models;

namespace WPFNode.Tests.Models;

public class TestNode : NodeBase
{
    public OutputPort<double> DoubleOutput { get; }
    public InputPort<double> DoubleInput { get; }
    public InputPort<string> StringInput { get; }

    public TestNode()
    {
        DoubleOutput = new OutputPort<double>("DoubleOutput", this);
        DoubleInput = new InputPort<double>("DoubleInput", this);
        StringInput = new InputPort<string>("StringInput", this);

        RegisterOutputPort(DoubleOutput);
        RegisterInputPort(DoubleInput);
        RegisterInputPort(StringInput);
    }

    public override Task ProcessAsync()
    {
        return Task.CompletedTask;
    }
} 