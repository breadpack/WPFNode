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
    }

    public override Task ProcessAsync()
    {
        // 연결된 입력 포트의 값을 출력 포트로 전달
        if (DoubleInput.IsConnected)
        {
            DoubleOutput.Value = DoubleInput.Value;
        }
        return Task.CompletedTask;
    }

    protected override void InitializePorts() {
        RegisterOutputPort(DoubleOutput);
        RegisterInputPort(DoubleInput);
        RegisterInputPort(StringInput);
    }
} 