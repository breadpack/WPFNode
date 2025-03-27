using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Nodes;
using Xunit;

namespace WPFNode.Tests
{
    // 복수의 출력값을 생성하는 테스트용 노드
    public class MultiOutputNode : NodeBase, IFlowEntry
    {
        [NodeFlowOut("Out")]
        public FlowOutPort FlowOut { get; private set; }
        
        [NodeOutput("Value")]
        public OutputPort<int> OutputValue { get; private set; }
        
        [NodeProperty("Count")]
        public WPFNode.Models.Properties.NodeProperty<int> CountProperty { get; private set; }
        
        public MultiOutputNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken)
        {
            // Count 값만큼 출력 생성
            var count = CountProperty.Value;
            
            for (int i = 0; i < count; i++)
            {
                // 출력값 설정
                OutputValue.Value = i + 1;
                
                // FlowOut 반환
                yield return FlowOut;
            }
        }
    }

    public class ListNodeTests
    {
        private readonly ILogger _logger;

        public ListNodeTests()
        {
            // 로깅 설정
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<ListNodeTests>();
        }

        [Fact]
        public async Task ListCreateAndAddTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode      = canvas.AddNode<StartNode>(0, 0);
            var listCreateNode = canvas.AddNode<ListCreateNode>(100, 0);
            var constNode      = canvas.AddNode<ConstantNode<int>>(100, 100);
            var listAddNode    = canvas.AddNode<ListAddNode>(200, 0);
            var trackingNode   = canvas.AddNode<TrackingNode>(300, 0);

            // 3. 노드 설정
            listCreateNode.ElementType.Value = typeof(int);
            listAddNode.ElementType.Value = typeof(int);
            
            // 상수 노드 값 수동 설정
            constNode.Value.Value = 42;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(listCreateNode.FlowIn);
            listCreateNode.FlowOut.Connect(listAddNode.FlowIn);
            listAddNode.FlowOut.Connect(trackingNode.FlowIn);
            
            // 데이터 연결 - Reflection을 통해 연결
            Canvas_ConnectNodePorts(canvas, listCreateNode, "리스트", listAddNode, "리스트");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", listAddNode, "항목");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - 추적 노드가 실행되었는지 확인
            Assert.True(trackingNode.ReceivedValues.Count > 0);
        }

        [Fact]
        public async Task ListForEachTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var listCreateNode = canvas.AddNode<ListCreateNode>(100, 0);
            var constNode1 = canvas.AddNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.AddNode<ConstantNode<int>>(100, 150);
            var listAddNode1 = canvas.AddNode<ListAddNode>(200, 0);
            var listAddNode2 = canvas.AddNode<ListAddNode>(300, 0);
            var forEachNode = canvas.AddNode<ListForEachNode>(400, 0);
            var itemTrackingNode = canvas.AddNode<TrackingNode>(500, 0);
            var completeTrackingNode = canvas.AddNode<TrackingNode>(500, 100);

            // 3. 노드 설정
            listCreateNode.ElementType.Value = typeof(int);
            listAddNode1.ElementType.Value = typeof(int);
            listAddNode2.ElementType.Value = typeof(int);
            forEachNode.ElementType.Value = typeof(int);

            // 상수 노드 값 설정
            constNode1.Value.Value = 10;
            constNode2.Value.Value = 20;
            
            // 완료 트래킹 값 설정
            var completionValue = canvas.AddNode<ConstantNode<int>>(450, 150);

            completionValue.Value.Value = 450;

            // 4. 노드 연결 - Flow
            startNode.FlowOut.Connect(listCreateNode.FlowIn);
            listCreateNode.FlowOut.Connect(listAddNode1.FlowIn);
            listAddNode1.FlowOut.Connect(listAddNode2.FlowIn);
            listAddNode2.FlowOut.Connect(forEachNode.FlowIn);
            forEachNode.ItemFlowOut.Connect(itemTrackingNode.FlowIn);
            forEachNode.CompleteFlowOut.Connect(completeTrackingNode.FlowIn);
            
            // 데이터 연결 - 리플렉션 사용
            Canvas_ConnectNodePorts(canvas, listCreateNode, "리스트", listAddNode1, "리스트");
            Canvas_ConnectNodePorts(canvas, listAddNode1, "결과", listAddNode2, "리스트");
            Canvas_ConnectNodePorts(canvas, listAddNode2, "결과", forEachNode, "리스트");
            
            // 항목 연결
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", listAddNode1, "항목");
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", listAddNode2, "항목");
            Canvas_ConnectNodePorts(canvas, forEachNode, "현재 항목", itemTrackingNode, "Value");
            Canvas_ConnectNodePorts(canvas, completionValue, "Result", completeTrackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            // 각 항목마다 ItemFlowOut이 호출되었는지
            Assert.Equal(2, itemTrackingNode.ReceivedValues.Count);
            Assert.Contains(10, itemTrackingNode.ReceivedValues);
            Assert.Contains(20, itemTrackingNode.ReceivedValues);
            
            // 완료 Flow가 실행되었는지
            Assert.Single(completeTrackingNode.ReceivedValues);
            Assert.Equal(450, completeTrackingNode.ReceivedValues[0]);
        }

        // 리플렉션을 사용하여 노드 포트 연결하는 헬퍼 메서드
        private void Canvas_ConnectNodePorts(INodeCanvas canvas, INode sourceNode, string sourcePortName, INode targetNode, string targetPortName)
        {
            var sourceOutputPorts = sourceNode.OutputPorts;
            var targetInputPorts = targetNode.InputPorts;
            
            IOutputPort sourcePort = null;
            IInputPort targetPort = null;
            
            // 소스 포트 찾기
            foreach (var port in sourceOutputPorts)
            {
                if (port.Name == sourcePortName)
                {
                    sourcePort = port;
                    break;
                }
            }
            
            // 타겟 포트 찾기
            foreach (var port in targetInputPorts)
            {
                if (port.Name == targetPortName)
                {
                    targetPort = port;
                    break;
                }
            }
            
            if (sourcePort != null && targetPort != null)
            {
                // 포트 연결
                sourcePort.Connect(targetPort);
            }
            else
            {
                throw new Exception($"Cannot find ports: {sourcePortName} -> {targetPortName}");
            }
        }
    }
}
