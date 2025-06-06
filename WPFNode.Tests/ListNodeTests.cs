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

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
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

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
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

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
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
            var collectTestNode = canvas.CreateNode<CollectTestNode>(0, 0);
            var listCollectNode = canvas.CreateNode<ListCollectNode>(100, 0);
            var consoleWriteNode = canvas.CreateNode<ConsoleWriteNode>(200, 100);

            // 3. 노드 설정
            collectTestNode.CountProperty.Value = 5; // 5개의 값을 생성
            // listCollectNode의 타입 설정 제거 (GenericInputPort 사용 가정)
            // listCollectNode.ElementType.Value = typeof(int);

            // Flow가 완료된 후 검증하기 위한 TrackingNode 추가
            var completionTrackingNode = canvas.CreateNode<TrackingNode<int>>(300, 100);
            
            // 완료 확인을 위한 상수 노드
            var completionConstant = canvas.CreateNode<ConstantNode<int>>(250, 150);
            completionConstant.Value.Value = 999; // 완료 신호 값
            
            // 4. 노드 연결
            // Flow 연결
            collectTestNode.FlowOut.Connect(listCollectNode.AddFlowIn);
            collectTestNode.CompleteFlowOut.Connect(consoleWriteNode.InPort);
            consoleWriteNode.OutPort.Connect(completionTrackingNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, collectTestNode, "Value", listCollectNode, "Element");
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
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var listCreateNode = canvas.CreateNode<ListCreateNode>(100, 0);
            var constNode = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var listAddNode = canvas.CreateNode<ListAddNode>(200, 0);
            var listAddNode2 = canvas.CreateNode<ListAddNode>(200, 100);
            var trackingNode = canvas.CreateNode<TrackingNode<List<int>>>(300, 0);

            // 3. 노드 설정
            // ListCreateNode는 타입 프로퍼티 유지
            listCreateNode.ElementType.Value = typeof(int);
            // listAddNode, listAddNode2의 타입 설정 제거
            // listAddNode.ElementType.Value = typeof(int);
            // listAddNode2.ElementType.Value = typeof(int);

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
            Canvas_ConnectNodePorts(canvas, listAddNode2, "결과", trackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - 추적 노드가 실행되었는지 확인
            Assert.Single(trackingNode.ReceivedValues);
            var resultList = trackingNode.ReceivedValues[0];
            Assert.Contains(42, resultList);
            Assert.Equal(2, resultList.Count);
        }

        [Fact]
        public async Task ListForEachTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var listCreateNode = canvas.CreateNode<ListCreateNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<int>>(100, 150);
            var listAddNode1 = canvas.CreateNode<ListAddNode>(200, 0);
            var listAddNode2 = canvas.CreateNode<ListAddNode>(300, 0);
            var forEachNode = canvas.CreateNode<ListForEachNode>(400, 0);
            var itemTrackingNode = canvas.CreateNode<TrackingNode<int>>(500, 0);
            var completeTrackingNode = canvas.CreateNode<TrackingNode<int>>(500, 100);

            // 3. 노드 설정
            // ListCreateNode는 타입 프로퍼티 유지
            listCreateNode.ElementType.Value = typeof(int);
            // listAddNode1, listAddNode2, forEachNode의 타입 설정 제거
            // listAddNode1.ElementType.Value = typeof(int);
            // listAddNode2.ElementType.Value = typeof(int);
            // forEachNode.ElementType.Value = typeof(int);

            // 상수 노드 값 설정
            constNode1.Value.Value = 10;
            constNode2.Value.Value = 20;
            
            // 완료 트래킹 값 설정
            var completionValue = canvas.CreateNode<ConstantNode<int>>(450, 150);
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
            Canvas_ConnectNodePorts(canvas, forEachNode, "Element", itemTrackingNode, "Value");
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
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var listCreateNode = canvas.CreateNode<ListCreateNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<int>>(100, 150);
            var listAddNode1 = canvas.CreateNode<ListAddNode>(200, 0);
            var listAddNode2 = canvas.CreateNode<ListAddNode>(200, 100);
            var listRemoveNode = canvas.CreateNode<ListRemoveNode>(300, 0);
            var consoleWriteNode = canvas.CreateNode<ConsoleWriteNode>(400, 0);

            // 3. 노드 설정
            // ListCreateNode는 타입 프로퍼티 유지
            listCreateNode.ElementType.Value = typeof(int);
            // listAddNode1, listAddNode2, listRemoveNode의 타입 설정 제거
            // listAddNode1.ElementType.Value = typeof(int);
            // listAddNode2.ElementType.Value = typeof(int);
            // listRemoveNode.ElementType.Value = typeof(int);
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
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var listCreateNode = canvas.CreateNode<ListCreateNode>(100, 0);
            var constNode = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var listAddNode = canvas.CreateNode<ListAddNode>(200, 0);
            var listClearNode = canvas.CreateNode<ListClearNode>(300, 0);
            var consoleWriteNode = canvas.CreateNode<ConsoleWriteNode>(400, 0);

            // 3. 노드 설정
            // ListCreateNode는 타입 프로퍼티 유지
            listCreateNode.ElementType.Value = typeof(int);
            // listAddNode, listClearNode의 타입 설정 제거
            // listAddNode.ElementType.Value = typeof(int);
            // listClearNode.ElementType.Value = typeof(int);
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
            var addNodeHashCode = listAddNode.OutputPorts[0].Value?.GetHashCode() ?? 0;
            var clearNodeHashCode = listClearNode.OutputPorts[0].Value?.GetHashCode() ?? 0;
            
            _logger.LogInformation($"ListAddNode 결과 HashCode: {addNodeHashCode}");
            _logger.LogInformation($"ListClearNode 결과 HashCode: {clearNodeHashCode}");
            
            Assert.Equal(addNodeHashCode, clearNodeHashCode);
            
            // 내용물이 올바르게 수정되었는지 확인 (비어있어야 함)
            var resultList = (List<int>)listClearNode.OutputPorts[0].Value;
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task ObjectCollectionNodeTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var objectCollectionNode = canvas.CreateNode<ObjectCollectionNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<int>>(100, 150);
            var trackingNode = canvas.CreateNode<TrackingNode<List<int>>>(200, 0);

            // 3. 노드 설정
            objectCollectionNode.SelectedType.Value = typeof(int);
            objectCollectionNode.ItemCount.Value = 2;
            constNode1.Value.Value = 42;
            constNode2.Value.Value = 84;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(objectCollectionNode.FlowIn);
            objectCollectionNode.FlowOut.Connect(trackingNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", objectCollectionNode, "Item 1");
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", objectCollectionNode, "Item 2");
            Canvas_ConnectNodePorts(canvas, objectCollectionNode, "Collection", trackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.Single(trackingNode.ReceivedValues);
            var resultList = trackingNode.ReceivedValues[0];
            Assert.Equal(2, resultList.Count);
            Assert.Contains(42, resultList);
            Assert.Contains(84, resultList);
        }

        [Fact]
        public async Task ObjectCollectionNodeStringTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var objectCollectionNode = canvas.CreateNode<ObjectCollectionNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<string>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<string>>(100, 150);
            var trackingNode = canvas.CreateNode<TrackingNode<List<string>>>(200, 0);

            // 3. 노드 설정
            objectCollectionNode.SelectedType.Value = typeof(string);
            objectCollectionNode.ItemCount.Value = 2;
            constNode1.Value.Value = "Hello";
            constNode2.Value.Value = "World";

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(objectCollectionNode.FlowIn);
            objectCollectionNode.FlowOut.Connect(trackingNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", objectCollectionNode, "Item 1");
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", objectCollectionNode, "Item 2");
            Canvas_ConnectNodePorts(canvas, objectCollectionNode, "Collection", trackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.Single(trackingNode.ReceivedValues);
            var resultList = trackingNode.ReceivedValues[0];
            Assert.Equal(2, resultList.Count);
            Assert.Contains("Hello", resultList);
            Assert.Contains("World", resultList);
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
