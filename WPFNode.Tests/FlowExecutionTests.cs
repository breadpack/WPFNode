using System.Threading.Tasks;
using WPFNode.Interfaces.Flow;
using WPFNode.Models;
using WPFNode.Models.Flow;
using WPFNode.Plugins.Basic;
using WPFNode.Extensions;
using Xunit;

namespace WPFNode.Tests
{
    public class FlowExecutionTests
    {
        [Fact]
        public async Task SimpleFlowExecution_ExecutesInOrder()
        {
            // 캔버스 생성
            var canvas = new NodeCanvas();
            
            // 노드 생성
            var startNode = canvas.CreateNode<StartNode>();
            var addNode = canvas.CreateNode<AdditionNode>();
            var switchNode = canvas.CreateNode<SwitchNode>();
            
            // 데이터 연결 설정
            addNode.InputA.SetValue(5.0);
            addNode.InputB.SetValue(10.0);
            
            // 흐름 연결
            startNode.StartOutput.Connect(addNode.FlowIn);
            
            // switchNode 연결 및 설정
            addNode.FlowOut.Connect(switchNode.FlowIn);
            switchNode.ValueInput.SetValue(1);  // Case 1에 해당하는 값
            switchNode.Case1Input.SetValue(1);
            
            // 실행
            await canvas.ExecuteAsync();
            
            // 검증
            Assert.Equal(15.0, addNode.Result.Value);
        }
        
        [Fact]
        public async Task ForLoopExecution_IteratesCorrectTimes()
        {
            // 캔버스 생성
            var canvas = new NodeCanvas();
            
            // 노드 생성
            var startNode = canvas.CreateNode<StartNode>();
            var forNode = canvas.CreateNode<ForNode>();
            var addNode = canvas.CreateNode<AdditionNode>();
            
            // 데이터 설정
            forNode.StartInput.SetValue(0);
            forNode.EndInput.SetValue(5);
            forNode.StepInput.SetValue(1);
            
            addNode.InputA.SetValue(1.0);
            addNode.InputB.SetValue(0.0);
            
            // forNode 루프 본문에서는 addNode.InputB 값을 forNode.CurrentOutput으로 업데이트하도록 연결
            forNode.CurrentOutput.Connect(addNode.InputB);
            
            // 흐름 연결
            startNode.StartOutput.Connect(forNode.FlowIn);
            forNode.LoopBody.Connect(addNode.FlowIn);
            addNode.FlowOut.Connect(forNode.FlowIn);
            
            // 실행
            await canvas.ExecuteAsync();
            
            // 검증: 0부터 4까지 5번 반복하면 InputA(1) + (0+1+2+3+4) = 11 이어야 함
            Assert.Equal(11.0, addNode.Result.Value);
        }
    }
}
