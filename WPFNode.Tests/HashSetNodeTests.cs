using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Plugins.Basic.Nodes;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Tests.Helpers;
using Xunit;

namespace WPFNode.Tests
{
    // HashSet의 해시코드를 추적하기 위한 노드
    public class HashSetHashOutputNode : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; }
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; }
        
        [NodeOutput("HashCode")]
        public OutputPort<int> HashCodeOutput { get; private set; }
        
        [NodeInput("HashSet")]
        public InputPort<HashSet<int>> HashSetInput { get; private set; }
        
        public HashSetHashOutputNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            Name = "HashSetHashOutput";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // 해시셋의 HashCode를 출력
            if (HashSetInput.IsConnected)
            {
                var hashSet = HashSetInput.GetValueOrDefault();
                if (hashSet != null)
                {
                    HashCodeOutput.Value = hashSet.GetHashCode();
                }
            }
            
            yield return FlowOut;
        }
    }

    public class HashSetNodeTests
    {
        private readonly ILogger _logger;

        public HashSetNodeTests()
        {
            // 로깅 설정
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<HashSetNodeTests>();
        }
        
        [Fact]
        public async Task HashSetCreateAndAddTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var hashSetCreateNode = canvas.CreateNode<HashSetCreateNode>(100, 0);
            var constNode = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var hashSetAddNode = canvas.CreateNode<HashSetAddNode>(200, 0);
            var hashSetAddNode2 = canvas.CreateNode<HashSetAddNode>(300, 0); // 중복 추가 시도
            var validationNode = canvas.CreateNode<CollectionValidationNode<int>>(400, 0);

            // 3. 노드 설정
            hashSetCreateNode.ElementType.Value = typeof(int);
            constNode.Value.Value = 42;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(hashSetCreateNode.FlowIn);
            hashSetCreateNode.FlowOut.Connect(hashSetAddNode.FlowIn);
            hashSetAddNode.FlowOut.Connect(hashSetAddNode2.FlowIn); // 같은 값을 두 번 추가
            hashSetAddNode2.FlowOut.Connect(validationNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, hashSetCreateNode, "해시셋", hashSetAddNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, hashSetAddNode, "결과", hashSetAddNode2, "해시셋");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", hashSetAddNode, "항목");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", hashSetAddNode2, "항목"); // 같은 값을 다시 추가
            Canvas_ConnectNodePorts(canvas, hashSetAddNode2, "결과", validationNode, "Collection");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(1, validationNode.ItemCount); // 중복 추가가 안되므로 항목은 1개만 있어야 함
            Assert.Contains(42, validationNode.ReceivedItems);
        }

        [Fact]
        public async Task HashSetAddSuccessFailureTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var hashSetCreateNode = canvas.CreateNode<HashSetCreateNode>(100, 0);
            var constNode = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var hashSetAddNode = canvas.CreateNode<HashSetAddNode>(200, 0);
            var hashSetAddNode2 = canvas.CreateNode<HashSetAddNode>(300, 0); // 중복 추가 시도
            var successTrackingNode1 = canvas.CreateNode<TrackingNode<bool>>(400, 50);
            var successTrackingNode2 = canvas.CreateNode<TrackingNode<bool>>(400, 100);

            // 3. 노드 설정
            hashSetCreateNode.ElementType.Value = typeof(int);
            constNode.Value.Value = 42;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(hashSetCreateNode.FlowIn);
            hashSetCreateNode.FlowOut.Connect(hashSetAddNode.FlowIn);
            hashSetAddNode.FlowOut.Connect(hashSetAddNode2.FlowIn);
            hashSetAddNode2.FlowOut.Connect(successTrackingNode1.FlowIn);
            successTrackingNode1.FlowOut.Connect(successTrackingNode2.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, hashSetCreateNode, "해시셋", hashSetAddNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, hashSetAddNode, "결과", hashSetAddNode2, "해시셋");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", hashSetAddNode, "항목");
            Canvas_ConnectNodePorts(canvas, constNode, "Result", hashSetAddNode2, "항목"); // 같은 값을 다시 추가
            Canvas_ConnectNodePorts(canvas, hashSetAddNode, "성공", successTrackingNode1, "Value");
            Canvas_ConnectNodePorts(canvas, hashSetAddNode2, "성공", successTrackingNode2, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - 첫 번째 추가는 성공, 두 번째 추가는 실패
            Assert.Equal(2, successTrackingNode1.ReceivedValues.Count + successTrackingNode2.ReceivedValues.Count);
            Assert.Equal(true, successTrackingNode1.ReceivedValues[0]); // 첫 번째 추가 성공
            Assert.Equal(false, successTrackingNode2.ReceivedValues[0]); // 두 번째 추가 실패 (중복)
        }

        [Fact]
        public async Task HashSetRemoveTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var hashSetCreateNode = canvas.CreateNode<HashSetCreateNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<int>>(100, 200);
            var hashSetAddNode = canvas.CreateNode<HashSetAddNode>(200, 0);
            var hashSetAddNode2 = canvas.CreateNode<HashSetAddNode>(300, 0);
            var hashSetRemoveNode = canvas.CreateNode<HashSetRemoveNode>(400, 0);
            var validationNode = canvas.CreateNode<CollectionValidationNode<int>>(500, 0);
            var successTrackingNode = canvas.CreateNode<TrackingNode<bool>>(500, 100);

            // 3. 노드 설정
            hashSetCreateNode.ElementType.Value = typeof(int);
            constNode1.Value.Value = 42;
            constNode2.Value.Value = 100;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(hashSetCreateNode.FlowIn);
            hashSetCreateNode.FlowOut.Connect(hashSetAddNode.FlowIn);
            hashSetAddNode.FlowOut.Connect(hashSetAddNode2.FlowIn);
            hashSetAddNode2.FlowOut.Connect(hashSetRemoveNode.FlowIn);
            hashSetRemoveNode.FlowOut.Connect(validationNode.FlowIn);
            validationNode.FlowOut.Connect(successTrackingNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, hashSetCreateNode, "해시셋", hashSetAddNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, hashSetAddNode, "결과", hashSetAddNode2, "해시셋");
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", hashSetAddNode, "항목"); // 42 추가
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", hashSetAddNode2, "항목"); // 100 추가
            Canvas_ConnectNodePorts(canvas, hashSetAddNode2, "결과", hashSetRemoveNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", hashSetRemoveNode, "항목"); // 42 제거
            Canvas_ConnectNodePorts(canvas, hashSetRemoveNode, "결과", validationNode, "Collection");
            Canvas_ConnectNodePorts(canvas, hashSetRemoveNode, "성공", successTrackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(1, validationNode.ItemCount); // 42가 제거되어 100만 남아야 함
            Assert.Contains(100, validationNode.ReceivedItems);
            Assert.DoesNotContain(42, validationNode.ReceivedItems);
            Assert.Equal(true, successTrackingNode.ReceivedValues[0]); // 제거 성공
        }

        [Fact]
        public async Task HashSetRemoveFailureTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var hashSetCreateNode = canvas.CreateNode<HashSetCreateNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<int>>(100, 200);
            var hashSetAddNode = canvas.CreateNode<HashSetAddNode>(200, 0);
            var hashSetRemoveNode = canvas.CreateNode<HashSetRemoveNode>(300, 0);
            var successTrackingNode = canvas.CreateNode<TrackingNode<bool>>(400, 0);

            // 3. 노드 설정
            hashSetCreateNode.ElementType.Value = typeof(int);
            constNode1.Value.Value = 42;
            constNode2.Value.Value = 100; // 존재하지 않는 항목

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(hashSetCreateNode.FlowIn);
            hashSetCreateNode.FlowOut.Connect(hashSetAddNode.FlowIn);
            hashSetAddNode.FlowOut.Connect(hashSetRemoveNode.FlowIn);
            hashSetRemoveNode.FlowOut.Connect(successTrackingNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, hashSetCreateNode, "해시셋", hashSetAddNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, hashSetAddNode, "결과", hashSetRemoveNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", hashSetAddNode, "항목"); // 42 추가
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", hashSetRemoveNode, "항목"); // 100 제거 시도 (없음)
            Canvas_ConnectNodePorts(canvas, hashSetRemoveNode, "성공", successTrackingNode, "Value");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.Equal(false, successTrackingNode.ReceivedValues[0]); // 제거 실패 (없는 항목)
        }

        [Fact]
        public async Task HashSetClearTest()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var hashSetCreateNode = canvas.CreateNode<HashSetCreateNode>(100, 0);
            var constNode1 = canvas.CreateNode<ConstantNode<int>>(100, 100);
            var constNode2 = canvas.CreateNode<ConstantNode<int>>(100, 200);
            var hashSetAddNode = canvas.CreateNode<HashSetAddNode>(200, 0);
            var hashSetAddNode2 = canvas.CreateNode<HashSetAddNode>(300, 0);
            var hashSetClearNode = canvas.CreateNode<HashSetClearNode>(400, 0);
            var validationNode = canvas.CreateNode<CollectionValidationNode<int>>(500, 0);
            
            // 해시셋 레퍼런스 추적을 위한 노드들
            var hashSetHashBefore = canvas.CreateNode<HashSetHashOutputNode>(300, 100);
            var hashSetHashAfter = canvas.CreateNode<HashSetHashOutputNode>(500, 100);
            var hashCodeTrackingBefore = canvas.CreateNode<TrackingNode<int>>(300, 200);
            var hashCodeTrackingAfter = canvas.CreateNode<TrackingNode<int>>(500, 200);

            // 3. 노드 설정
            hashSetCreateNode.ElementType.Value = typeof(int);
            constNode1.Value.Value = 42;
            constNode2.Value.Value = 100;

            // 4. 노드 연결
            // Flow 연결
            startNode.FlowOut.Connect(hashSetCreateNode.FlowIn);
            hashSetCreateNode.FlowOut.Connect(hashSetAddNode.FlowIn);
            hashSetAddNode.FlowOut.Connect(hashSetAddNode2.FlowIn);
            hashSetAddNode2.FlowOut.Connect(hashSetHashBefore.FlowIn);
            hashCodeTrackingBefore.FlowIn.Connect(hashSetHashBefore.FlowOut);
            hashCodeTrackingBefore.FlowOut.Connect(hashSetClearNode.FlowIn);
            hashSetClearNode.FlowOut.Connect(hashSetHashAfter.FlowIn);
            hashCodeTrackingAfter.FlowIn.Connect(hashSetHashAfter.FlowOut);
            hashCodeTrackingAfter.FlowOut.Connect(validationNode.FlowIn);
            
            // 데이터 연결
            Canvas_ConnectNodePorts(canvas, hashSetCreateNode, "해시셋", hashSetAddNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, hashSetAddNode, "결과", hashSetAddNode2, "해시셋");
            Canvas_ConnectNodePorts(canvas, constNode1, "Result", hashSetAddNode, "항목"); // 42 추가
            Canvas_ConnectNodePorts(canvas, constNode2, "Result", hashSetAddNode2, "항목"); // 100 추가
            
            // 해시셋 레퍼런스 추적을 위한 연결
            Canvas_ConnectNodePorts(canvas, hashSetAddNode2, "결과", hashSetHashBefore, "HashSet");
            Canvas_ConnectNodePorts(canvas, hashSetHashBefore, "HashCode", hashCodeTrackingBefore, "Value");
            
            Canvas_ConnectNodePorts(canvas, hashSetAddNode2, "결과", hashSetClearNode, "해시셋");
            Canvas_ConnectNodePorts(canvas, hashSetClearNode, "결과", hashSetHashAfter, "HashSet");
            Canvas_ConnectNodePorts(canvas, hashSetHashAfter, "HashCode", hashCodeTrackingAfter, "Value");
            
            Canvas_ConnectNodePorts(canvas, hashSetClearNode, "결과", validationNode, "Collection");

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(0, validationNode.ItemCount); // Clear 후 항목이 없어야 함
            
            // 해시셋 레퍼런스가 유지되는지 확인 (같은 HashCode)
            Assert.Equal(hashCodeTrackingBefore.ReceivedValues[0], hashCodeTrackingAfter.ReceivedValues[0]);
        }

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