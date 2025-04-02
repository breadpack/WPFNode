using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Nodes;
using WPFNode.Tests.Helpers;
using Xunit;

namespace WPFNode.Tests
{
    // 테스트를 위한 간단한 사람 클래스
    public class Person
    {
        public string Name { get; set; } = "홍길동";
        public int Age { get; set; } = 30;
        public string Job { get; set; } = "개발자";
        public bool IsActive = true; // 필드로 선언
    }

    // 속성 값 추적을 위한 헬퍼 노드
    public class PropertyValueTrackingNode : NodeBase
    {
        [NodeFlowIn]
        public FlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public FlowOutPort FlowOut { get; private set; }

        [NodeInput("Value")]
        public InputPort<object> ValueInput { get; private set; }

        // 값 추적
        public object ReceivedValue { get; private set; }
        public bool WasExecuted { get; private set; } = false;
        public Type ReceivedValueType { get; private set; }

        public PropertyValueTrackingNode(INodeCanvas canvas, Guid id)
            : base(canvas, id)
        {
            Name = "PropertyValueTracker";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken)
        {
            // 입력 값을 추적
            ReceivedValue = ValueInput.Value;
            ReceivedValueType = ReceivedValue?.GetType();
            WasExecuted = true;
            
            yield return FlowOut;
        }
    }

    // 객체 생성 노드 (테스트용)
    public class ObjectCreatorNode : NodeBase
    {
        [NodeFlowIn]
        public FlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public FlowOutPort FlowOut { get; private set; }

        [NodeProperty("ObjectType")]
        public NodeProperty<string> ObjectTypeProperty { get; private set; }

        [NodeOutput("Object")]
        public OutputPort<object> ObjectOutput { get; private set; }

        public ObjectCreatorNode(INodeCanvas canvas, Guid id)
            : base(canvas, id)
        {
            Name = "ObjectCreator";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken)
        {
            object obj;
            
            switch (ObjectTypeProperty.Value?.ToLower())
            {
                case "person":
                    obj = new Person();
                    break;
                    
                case "expando":
                    dynamic expando = new ExpandoObject();
                    expando.Name = "홍길동";
                    expando.Age = 30;
                    expando.Score = 95.5;
                    expando.Tags = new[] { "개발자", "건축가" };
                    obj = expando;
                    break;
                    
                case "dictionary":
                    obj = new Dictionary<string, object>
                    {
                        { "Name", "홍길동" },
                        { "Age", 30 },
                        { "Score", 95.5 },
                        { "IsActive", true }
                    };
                    break;
                    
                default:
                    obj = new object();
                    break;
            }
            
            ObjectOutput.Value = obj;
            
            yield return FlowOut;
        }
    }

    // ObjectExposeNode 및 DynamicPropertyAccessNode 테스트 클래스
    public class DynamicNodeTests
    {
        private readonly ILogger _logger;

        public DynamicNodeTests()
        {
            // 로깅 설정
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<DynamicNodeTests>();
        }

        // 두 노드 간의 연결 헬퍼 메서드
        private void Canvas_ConnectNodePorts(INodeCanvas canvas, INode sourceNode, string sourcePortName, INode targetNode, string targetPortName)
        {
            // 출력 포트 찾기
            var sourcePort = sourceNode.OutputPorts.FirstOrDefault(p => p.Name == sourcePortName);
            if (sourcePort == null)
                throw new InvalidOperationException($"소스 노드의 '{sourcePortName}' 포트를 찾을 수 없습니다.");

            // 입력 포트 찾기
            var targetPort = targetNode.InputPorts.FirstOrDefault(p => p.Name == targetPortName);
            if (targetPort == null)
                throw new InvalidOperationException($"타겟 노드의 '{targetPortName}' 포트를 찾을 수 없습니다.");

            // 포트 연결
            sourcePort.Connect(targetPort);
        }

        [Fact]
        public async Task ObjectExposeNode_WithExpandoObject_ExposesProperties()
        {
            // 1. 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var objectCreator = canvas.CreateNode<ObjectCreatorNode>(100, 0);
            
            // ObjectExposeNode 대신 DynamicPropertyAccessNode 사용
            var nameAccessNode = canvas.CreateNode<DynamicPropertyAccessNode>(200, 0);
            var ageAccessNode = canvas.CreateNode<DynamicPropertyAccessNode>(200, 100);
            var scoreAccessNode = canvas.CreateNode<DynamicPropertyAccessNode>(200, 200);
            var tagsAccessNode = canvas.CreateNode<DynamicPropertyAccessNode>(200, 300);
            
            var nameTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 0);
            var ageTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 100);
            var scoreTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 200);
            var tagsTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 300);
            
            // 3. 노드 설정
            objectCreator.ObjectTypeProperty.Value = "expando";
            
            // 각 DynamicPropertyAccessNode의 속성 이름 설정
            nameAccessNode.PropertyName.Value = "Name";
            nameAccessNode.DefaultValue.Value = "";
            
            ageAccessNode.PropertyName.Value = "Age";
            ageAccessNode.DefaultValue.Value = 0;
            
            scoreAccessNode.PropertyName.Value = "Score";
            scoreAccessNode.DefaultValue.Value = 0.0;
            
            tagsAccessNode.PropertyName.Value = "Tags";
            tagsAccessNode.DefaultValue.Value = null;

            // 4. 노드 연결 - 흐름
            startNode.FlowOut.Connect(objectCreator.FlowIn);
            objectCreator.FlowOut.Connect(nameAccessNode.FlowIn);
            nameAccessNode.FlowOut.Connect(ageAccessNode.FlowIn);
            ageAccessNode.FlowOut.Connect(scoreAccessNode.FlowIn);
            scoreAccessNode.FlowOut.Connect(tagsAccessNode.FlowIn);
            tagsAccessNode.FlowOut.Connect(nameTracker.FlowIn);
            nameTracker.FlowOut.Connect(ageTracker.FlowIn);
            ageTracker.FlowOut.Connect(scoreTracker.FlowIn);
            scoreTracker.FlowOut.Connect(tagsTracker.FlowIn);
            
            // 객체 입력 연결
            objectCreator.ObjectOutput.Connect(nameAccessNode.ObjectInput);
            objectCreator.ObjectOutput.Connect(ageAccessNode.ObjectInput);
            objectCreator.ObjectOutput.Connect(scoreAccessNode.ObjectInput);
            objectCreator.ObjectOutput.Connect(tagsAccessNode.ObjectInput);
            
            // 각 DynamicPropertyAccessNode의 출력 포트를 tracker에 연결
            var nameValuePort = nameAccessNode.OutputPorts.FirstOrDefault(p => p.Name == "값");
            var ageValuePort = ageAccessNode.OutputPorts.FirstOrDefault(p => p.Name == "값");
            var scoreValuePort = scoreAccessNode.OutputPorts.FirstOrDefault(p => p.Name == "값");
            var tagsValuePort = tagsAccessNode.OutputPorts.FirstOrDefault(p => p.Name == "값");
            
            Assert.NotNull(nameValuePort);
            Assert.NotNull(ageValuePort);
            Assert.NotNull(scoreValuePort);
            Assert.NotNull(tagsValuePort);
            
            nameValuePort.Connect(nameTracker.ValueInput);
            ageValuePort.Connect(ageTracker.ValueInput);
            scoreValuePort.Connect(scoreTracker.ValueInput);
            tagsValuePort.Connect(tagsTracker.ValueInput);
            
            // 5. 실행하여 값 전달
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(nameTracker.WasExecuted);
            Assert.True(ageTracker.WasExecuted);
            Assert.True(scoreTracker.WasExecuted);
            Assert.True(tagsTracker.WasExecuted);
            
            Assert.Equal("홍길동", nameTracker.ReceivedValue);
            Assert.Equal(30, ageTracker.ReceivedValue);
            Assert.Equal(95.5, scoreTracker.ReceivedValue);
            
            // 태그 배열 검증
            Assert.NotNull(tagsTracker.ReceivedValue);
            Assert.IsType<string[]>(tagsTracker.ReceivedValue);
            var tags = (string[])tagsTracker.ReceivedValue;
            Assert.Contains("개발자", tags);
            Assert.Contains("건축가", tags);
        }

        [Fact]
        public async Task DynamicPropertyAccessNode_WithExpandoObject_AccessesByName()
        {
            // 1. 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var objectCreator = canvas.CreateNode<ObjectCreatorNode>(100, 0);
            var nameAccessNode = canvas.CreateNode<DynamicPropertyAccessNode>(200, 0);
            var valueTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 0);
            var existsTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 100);
            
            // 3. 노드 설정
            objectCreator.ObjectTypeProperty.Value = "expando";
            nameAccessNode.PropertyName.Value = "Name";
            nameAccessNode.DefaultValue.Value = "기본값";

            // 4. 노드 연결
            startNode.FlowOut.Connect(objectCreator.FlowIn);
            objectCreator.FlowOut.Connect(nameAccessNode.FlowIn);
            nameAccessNode.FlowOut.Connect(valueTracker.FlowIn);
            valueTracker.FlowOut.Connect(existsTracker.FlowIn);
            
            // 객체 연결
            objectCreator.ObjectOutput.Connect(nameAccessNode.ObjectInput);
            
            // 결과 및 존재 여부 출력 연결
            var valuePort = nameAccessNode.OutputPorts.FirstOrDefault(p => p.Name == "값");
            var existsPort = nameAccessNode.OutputPorts.FirstOrDefault(p => p.Name == "존재 여부");
            
            Assert.NotNull(valuePort);
            Assert.NotNull(existsPort);
            
            valuePort.Connect(valueTracker.ValueInput);
            existsPort.Connect(existsTracker.ValueInput);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(valueTracker.WasExecuted);
            Assert.True(existsTracker.WasExecuted);
            
            Assert.Equal("홍길동", valueTracker.ReceivedValue);
            Assert.Equal(true, existsTracker.ReceivedValue);
        }

        [Fact]
        public async Task DynamicPropertyAccessNode_WithDictionary_AccessesByKey()
        {
            // 1. 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var objectCreator = canvas.CreateNode<ObjectCreatorNode>(100, 0);
            var propertyAccess = canvas.CreateNode<DynamicPropertyAccessNode>(200, 0);
            var valueTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 0);
            var existsTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 100);
            
            // 3. 노드 설정
            objectCreator.ObjectTypeProperty.Value = "dictionary";
            propertyAccess.PropertyName.Value = "Score";
            propertyAccess.DefaultValue.Value = 0;

            // 4. 노드 연결
            startNode.FlowOut.Connect(objectCreator.FlowIn);
            objectCreator.FlowOut.Connect(propertyAccess.FlowIn);
            propertyAccess.FlowOut.Connect(valueTracker.FlowIn);
            valueTracker.FlowOut.Connect(existsTracker.FlowIn);
            
            // 객체 연결
            objectCreator.ObjectOutput.Connect(propertyAccess.ObjectInput);
            
            // 결과 및 존재 여부 출력 연결
            var valuePort = propertyAccess.OutputPorts.FirstOrDefault(p => p.Name == "값");
            var existsPort = propertyAccess.OutputPorts.FirstOrDefault(p => p.Name == "존재 여부");
            
            Assert.NotNull(valuePort);
            Assert.NotNull(existsPort);
            
            valuePort.Connect(valueTracker.ValueInput);
            existsPort.Connect(existsTracker.ValueInput);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(valueTracker.WasExecuted);
            Assert.True(existsTracker.WasExecuted);
            
            Assert.Equal(95.5, valueTracker.ReceivedValue);
            Assert.Equal(true, existsTracker.ReceivedValue);
        }
        
        [Fact]
        public async Task DynamicPropertyAccessNode_WithNonExistentProperty_ReturnsDefaultValue()
        {
            // 1. 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.CreateNode<StartNode>(0, 0);
            var objectCreator = canvas.CreateNode<ObjectCreatorNode>(100, 0);
            var propertyAccess = canvas.CreateNode<DynamicPropertyAccessNode>(200, 0);
            var valueTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 0);
            var existsTracker = canvas.CreateNode<PropertyValueTrackingNode>(300, 100);
            
            // 3. 노드 설정
            objectCreator.ObjectTypeProperty.Value = "dictionary";
            propertyAccess.PropertyName.Value = "NonExistentProperty";
            propertyAccess.DefaultValue.Value = "기본값";

            // 4. 노드 연결
            startNode.FlowOut.Connect(objectCreator.FlowIn);
            objectCreator.FlowOut.Connect(propertyAccess.FlowIn);
            propertyAccess.FlowOut.Connect(valueTracker.FlowIn);
            valueTracker.FlowOut.Connect(existsTracker.FlowIn);
            
            // 객체 연결
            objectCreator.ObjectOutput.Connect(propertyAccess.ObjectInput);
            
            // 결과 및 존재 여부 출력 연결
            var valuePort = propertyAccess.OutputPorts.FirstOrDefault(p => p.Name == "값");
            var existsPort = propertyAccess.OutputPorts.FirstOrDefault(p => p.Name == "존재 여부");
            
            Assert.NotNull(valuePort);
            Assert.NotNull(existsPort);
            
            valuePort.Connect(valueTracker.ValueInput);
            existsPort.Connect(existsTracker.ValueInput);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(valueTracker.WasExecuted);
            Assert.True(existsTracker.WasExecuted);
            
            Assert.Equal("기본값", valueTracker.ReceivedValue);
            Assert.Equal(false, existsTracker.ReceivedValue);
        }

        // ObjectExposeNode는 더 복잡한 설정이 필요하므로 나중에 구현
    }
} 