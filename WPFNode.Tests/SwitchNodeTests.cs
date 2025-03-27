using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Primitives; // ConstantNode가 이 네임스페이스에 있습니다
using Xunit;

namespace WPFNode.Tests
{
    public class SwitchNodeTests
    {
        private readonly ILogger _logger;

        public SwitchNodeTests()
        {
            // 로깅 설정
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<SwitchNodeTests>();
        }

        [Fact]
        public async Task SwitchNode_WithStringValue_ActivatesCorrectCase()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var switchNode = canvas.AddNode<SwitchNode>(100, 50);
            var case1Node = canvas.AddNode<TrackingNode>(200, 0);
            var case2Node = canvas.AddNode<TrackingNode>(200, 100);
            var defaultNode = canvas.AddNode<TrackingNode>(200, 200);
            
            // 입력값 노드 (문자열 상수)
            var stringValue = canvas.AddNode<ConstantNode<string>>(50, 100);

            // 3. 노드 설정
            switchNode.ValueType.Value = typeof(string);
            switchNode.CaseCount.Value = 2; // 두 개의 케이스 설정
            
            // 케이스 값 설정
            switchNode[0].Value = "A";
            switchNode[1].Value = "B";
            
            // 입력 상수 값 설정 (Case "A" 선택)
            stringValue.Value.Value = "A";
            
            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(switchNode.FlowIn);
            switchNode.CaseFlowOut(0).Connect(case1Node.FlowIn);
            switchNode.CaseFlowOut(1).Connect(case2Node.FlowIn);
            switchNode.DefaultPort.Connect(defaultNode.FlowIn);
            
            // 데이터 연결
            stringValue.Result.Connect(switchNode.InputValue);
            
            // 트래킹 노드에 값 입력 (경로 구분용)
            var value1 = canvas.AddNode<ConstantNode<int>>(150, 0);
            var value2 = canvas.AddNode<ConstantNode<int>>(150, 100);
            var valueDefault = canvas.AddNode<ConstantNode<int>>(150, 200);
            
            value1.Value.Value = 1;
            value2.Value.Value = 2;
            valueDefault.Value.Value = 999;
            
            value1.Result.Connect(case1Node.InputValue);
            value2.Result.Connect(case2Node.InputValue);
            valueDefault.Result.Connect(defaultNode.InputValue);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - Case A가 선택되어야 함
            Assert.Single(case1Node.ReceivedValues);
            Assert.Equal(1, case1Node.ReceivedValues[0]);
            Assert.Empty(case2Node.ReceivedValues);
            Assert.Empty(defaultNode.ReceivedValues);
        }

        [Fact]
        public async Task SwitchNode_WithNonMatchingValue_ActivatesDefaultCase()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var switchNode = canvas.AddNode<SwitchNode>(100, 50);
            var case1Node = canvas.AddNode<TrackingNode>(200, 0);
            var case2Node = canvas.AddNode<TrackingNode>(200, 100);
            var defaultNode = canvas.AddNode<TrackingNode>(200, 200);
            
            // 입력값 노드 (문자열 상수)
            var stringValue = canvas.AddNode<ConstantNode<string>>(50, 100);

            // 3. 노드 설정
            switchNode.ValueType.Value = typeof(string);
            switchNode.CaseCount.Value = 2; // 두 개의 케이스 설정
            
            // 케이스 값 설정
            switchNode[0].Value = "A";
            switchNode[1].Value = "B";
            
            // 입력 상수 값 설정 (어떤 케이스와도 일치하지 않는 값)
            stringValue.Value.Value = "C";
            
            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(switchNode.FlowIn);
            switchNode.CaseFlowOut(0).Connect(case1Node.FlowIn);
            switchNode.CaseFlowOut(1).Connect(case2Node.FlowIn);
            switchNode.DefaultPort.Connect(defaultNode.FlowIn);
            
            // 데이터 연결
            stringValue.Result.Connect(switchNode.InputValue);
            
            // 트래킹 노드에 값 입력 (경로 구분용)
            var value1 = canvas.AddNode<ConstantNode<int>>(150, 0);
            var value2 = canvas.AddNode<ConstantNode<int>>(150, 100);
            var valueDefault = canvas.AddNode<ConstantNode<int>>(150, 200);
            
            value1.Value.Value = 1;
            value2.Value.Value = 2;
            valueDefault.Value.Value = 999;
            
            value1.Result.Connect(case1Node.InputValue);
            value2.Result.Connect(case2Node.InputValue);
            valueDefault.Result.Connect(defaultNode.InputValue);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - Default 케이스가 선택되어야 함
            Assert.Empty(case1Node.ReceivedValues);
            Assert.Empty(case2Node.ReceivedValues);
            Assert.Single(defaultNode.ReceivedValues);
            Assert.Equal(999, defaultNode.ReceivedValues[0]);
        }
    }
}
