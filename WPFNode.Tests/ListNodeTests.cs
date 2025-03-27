using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Nodes;
using WPFNode.Tests.Helpers;
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
    
    // ListCollect 테스트를 위한 IFlowEntry 노드
    public class CollectTestNode : NodeBase, IFlowEntry
    {
        [NodeFlowOut("Out")]
        public FlowOutPort FlowOut { get; private set; }
        
        [NodeFlowOut("Complete")]
        public FlowOutPort CompleteFlowOut { get; private set; }
        
        [NodeOutput("Value")]
        public OutputPort<int> OutputValue { get; private set; }
        
        [NodeProperty("Count")]
        public WPFNode.Models.Properties.NodeProperty<int> CountProperty { get; private set; }
        
        public CollectTestNode(INodeCanvas canvas, Guid id) 
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
            
            // 모든 처리가 완료되면 CompleteFlowOut 반환
            yield return CompleteFlowOut;
        }
    }

    // 리스트 HashCode 추적을 위한 노드
    public class ListHashOutputNode : NodeBase
    {
        [NodeOutput("HashCode")]
        public OutputPort<int> HashCodeOutput { get; private set; }
        
        [NodeInput("List")]
        public InputPort<List<int>> ListInput { get; private set; }
        
        public ListHashOutputNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            Name = "ListHashOutput";
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // 리스트의 HashCode를 출력
            if (ListInput.IsConnected)
            {
                var list = ListInput.GetValueOrDefault();
                if (list != null)
                {
                    HashCodeOutput.Value = list.GetHashCode();
                }
            }
            
            // 플로우 없음
            yield break;
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
        public async Task ListCollectTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var collectTestNode = canvas.AddNode<CollectTestNode>(0, 0);
            var listCollectNode = canvas.AddNode<ListCollectNode>(100, 0);
            var consoleWriteNode = canvas.AddNode<ConsoleWriteNode>(200, 100);

            // 3. 노드 설정
            collectTestNode.CountProperty.Value = 5; // 5개의 값을 생성
            listCollectNode.ElementType.Value = typeof(int);
            
            // Flow가 완료된 후 검증하기 위한 TrackingNode 추가
            var completionTrackingNode = canvas.AddNode<TrackingNode>(300, 100);
            
            // 완료 확인을 위한 상수 노드
            var completionConstant = canvas.AddNode<ConstantNode<int>>(250, 150);
            completionConstant.Value.Value = 999; // 완료 신호 값
            
            // 4. 노드 연결
            // Flow 연결
            collectTestNode.FlowOut.Connect(listCollectNode.AddFlowIn);
            collectTestNode.CompleteFlowOut.Connect(consoleWriteNode.InPort);
            consoleWriteNode.OutPort.Connect(completionTrackingNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, collectTestNode, "Value", listCollectNode, "항목");
            Canvas_ConnectNodePorts(canvas, listCollectNode, "리스트", consoleWriteNode, "Text");
            Canvas_ConnectNodePorts(canvas, completionConstant, "Result", completionTrackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();
            
            // 6. 결과 확인
            // 예상되는 값들이 수집되었는지 검증
            for (int i = 1; i <= 5; i++)
            {
                Assert.Contains(i, ((List<int>)listCollectNode.OutputPorts[0].Value));
            }
            
            // 완료 Flow가 실행되었는지 확인
            Assert.Single(completionTrackingNode.ReceivedValues);
            Assert.Equal(999, completionTrackingNode.ReceivedValues[0]);
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
            var listAddNode2   = canvas.AddNode<ListAddNode>(200, 100);
            var trackingNode   = canvas.AddNode<TrackingNode>(300, 0);

            // 3. 노드 설정
            listCreateNode.ElementType.Value = typeof(int);
            listAddNode.ElementType.Value = typeof(int);
            listAddNode2.ElementType.Value = typeof(int);
            
            // 상수 노드 값 수동 설정
            constNode.Value.Value = 42;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(listCreateNode.FlowIn);
            listCreateNode.FlowOut.Connect(listAddNode.FlowIn);
            listAddNode.FlowOut.Connect(listAddNode2.FlowIn);
            listAddNode2.FlowOut.Connect(trackingNode.FlowIn);
            
            // 데이터 연결 - Reflection을 통해 연결
            Canvas_ConnectNodePorts(canvas, listCreateNode, "리스트", listAddNode, "리스트");
            Canvas_ConnectNodePorts(canvas, listAddNode, "결과", listAddNode2, "리스트");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", listAddNode, "항목");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", listAddNode2, "항목");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - 추적 노드가 실행되었는지 확인
            Assert.True(trackingNode.ReceivedValues.Count > 0);
            Assert.Contains(42, (List<int>)listAddNode.OutputPorts[0].Value!);
            Assert.Contains(42, (List<int>)listAddNode2.OutputPorts[0].Value!);
            Assert.Equal(2, ((List<int>)listAddNode2.OutputPorts[0].Value!).Count);
            Assert.Equal(2, ((List<int>)listAddNode.OutputPorts[0].Value!).Count);
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
        
        [Fact]
        public async Task ListRemoveNodeReferenceTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var listCreateNode = canvas.AddNode<ListCreateNode>(100, 0);
            var constNode1 = canvas.AddNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.AddNode<ConstantNode<int>>(100, 150);
            var listAddNode1 = canvas.AddNode<ListAddNode>(200, 0);
            var listAddNode2 = canvas.AddNode<ListAddNode>(200, 100);
            var listRemoveNode = canvas.AddNode<ListRemoveNode>(300, 0);
            var consoleWriteNode = canvas.AddNode<ConsoleWriteNode>(400, 0);

            // 3. 노드 설정
            listCreateNode.ElementType.Value = typeof(int);
            listAddNode1.ElementType.Value = typeof(int);
            listAddNode2.ElementType.Value = typeof(int);
            listRemoveNode.ElementType.Value = typeof(int);
            constNode1.Value.Value = 10;
            constNode2.Value.Value = 20;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(listCreateNode.FlowIn);
            listCreateNode.FlowOut.Connect(listAddNode1.FlowIn);
            listAddNode1.FlowOut.Connect(listAddNode2.FlowIn);
            listAddNode2.FlowOut.Connect(listRemoveNode.FlowIn);
            listRemoveNode.FlowOut.Connect(consoleWriteNode.InPort);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, listCreateNode, "리스트", listAddNode1, "리스트");
            Canvas_ConnectNodePorts(canvas, listAddNode1, "결과", listAddNode2, "리스트");
            Canvas_ConnectNodePorts(canvas, listAddNode2, "결과", listRemoveNode, "리스트");
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", listAddNode1, "항목");
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", listAddNode2, "항목");
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", listRemoveNode, "항목");  // 10을 제거
            // ConsoleWrite에 현재 리스트 내용 출력
            Canvas_ConnectNodePorts(canvas, listRemoveNode, "결과", consoleWriteNode, "Text");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 
            // HashCode가 동일한지 확인 (참조 유지 검증)
            var addNodeHashCode = listAddNode2.OutputPorts[0].Value.GetHashCode();
            var removeNodeHashCode = listRemoveNode.OutputPorts[0].Value.GetHashCode();
            
            _logger.LogInformation($"ListAddNode 결과 HashCode: {addNodeHashCode}");
            _logger.LogInformation($"ListRemoveNode 결과 HashCode: {removeNodeHashCode}");
            
            Assert.Equal(addNodeHashCode, removeNodeHashCode);
            
            // 내용물이 올바르게 수정되었는지 확인
            var resultList = (List<int>)listRemoveNode.OutputPorts[0].Value;
            Assert.Single(resultList);  // 항목이 1개만 남아있어야 함 
            Assert.Contains(20, resultList);  // 20은 남아있어야 함
            Assert.DoesNotContain(10, resultList);  // 10은 제거되어야 함
        }

        [Fact]
        public async Task ListClearNodeReferenceTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var listCreateNode = canvas.AddNode<ListCreateNode>(100, 0);
            var constNode = canvas.AddNode<ConstantNode<int>>(100, 100);
            var listAddNode = canvas.AddNode<ListAddNode>(200, 0);
            var listClearNode = canvas.AddNode<ListClearNode>(300, 0);
            var consoleWriteNode = canvas.AddNode<ConsoleWriteNode>(400, 0);

            // 3. 노드 설정
            listCreateNode.ElementType.Value = typeof(int);
            listAddNode.ElementType.Value = typeof(int);
            listClearNode.ElementType.Value = typeof(int);
            constNode.Value.Value = 42;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(listCreateNode.FlowIn);
            listCreateNode.FlowOut.Connect(listAddNode.FlowIn);
            listAddNode.FlowOut.Connect(listClearNode.FlowIn);
            listClearNode.FlowOut.Connect(consoleWriteNode.InPort);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, listCreateNode, "리스트", listAddNode, "리스트");
            Canvas_ConnectNodePorts(canvas, listAddNode, "결과", listClearNode, "리스트");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", listAddNode, "항목");
            // ConsoleWrite에 현재 리스트 내용 출력
            Canvas_ConnectNodePorts(canvas, listClearNode, "결과", consoleWriteNode, "Text");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            // HashCode가 동일한지 확인 (참조 유지 검증)
            var addNodeHashCode = listAddNode.OutputPorts[0].Value.GetHashCode();
            var clearNodeHashCode = listClearNode.OutputPorts[0].Value.GetHashCode();
            
            _logger.LogInformation($"ListAddNode 결과 HashCode: {addNodeHashCode}");
            _logger.LogInformation($"ListClearNode 결과 HashCode: {clearNodeHashCode}");
            
            Assert.Equal(addNodeHashCode, clearNodeHashCode);
            
            // 내용물이 올바르게 수정되었는지 확인 (비어있어야 함)
            var resultList = (List<int>)listClearNode.OutputPorts[0].Value;
            Assert.Empty(resultList);
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
