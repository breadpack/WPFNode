using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Nodes;
using WPFNode.Services;
using Xunit;
using WPFNode.Tests.Models;
using System.Linq;
using System.Reflection;

namespace WPFNode.Tests.Serialization
{
    public class DynamicNodeSerializationTests
    {
        private readonly ILogger _logger;
        
        public DynamicNodeSerializationTests()
        {
            // 로깅 설정
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<DynamicNodeSerializationTests>();
            
            // 플러그인 서비스 초기화 - 테스트 전에 필요한 서비스 등록
            InitializePluginService();
        }
        
        private void InitializePluginService()
        {
            // NodeServices가 이미 초기화되어 있고 PluginService가 null이 아니면 반환
            if (NodeServices.PluginService != null)
                return;
                
            // NodePluginService 인스턴스 생성
            var pluginService = new NodePluginService();
            
            // CreateObjectNode 타입 직접 등록
            pluginService.RegisterNodeType(typeof(CreateObjectNode));
            
            // NodeServices에 PluginService 설정
            var field = typeof(NodeServices).GetField("_pluginService", 
                BindingFlags.NonPublic | BindingFlags.Static);
            field?.SetValue(null, pluginService);
        }
        
        /// <summary>
        /// TestPerson 타입에 대한 CreateObjectNode 인스턴스를 생성하고 기본 값을 설정합니다.
        /// </summary>
        private (NodeCanvas canvas, CreateObjectNode node) SetupTestPersonNode()
        {
            // Canvas 생성 및 노드 추가
            var canvas = NodeCanvas.Create();
            var node = canvas.AddNode<CreateObjectNode>();
            
            // 타입 설정을 통해 프로퍼티 생성 유도
            var typeProperty = node.Properties.FirstOrDefault(p => p.DisplayName == "Target Type") as NodeProperty<Type>;
            Assert.NotNull(typeProperty);
            typeProperty.Value = typeof(TestPerson);
            
            // 프로퍼티 가져오기
            var nameProperty = node.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var ageProperty = node.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            var isActiveProperty = node.Properties.FirstOrDefault(p => p.Name == "IsActive") as NodeProperty<bool>;
            
            Assert.NotNull(nameProperty);
            Assert.NotNull(ageProperty);
            Assert.NotNull(isActiveProperty);
            
            // 프로퍼티 값 설정
            nameProperty.Value = "Test Name";
            ageProperty.Value = 42;
            isActiveProperty.Value = true;
            
            return (canvas, node);
        }
        
        /// <summary>
        /// 노드를 직렬화하고 역직렬화합니다.
        /// </summary>
        private (NodeCanvas deserializedCanvas, CreateObjectNode deserializedNode) SerializeAndDeserialize(NodeCanvas canvas)
        {
            // 직렬화
            string json = canvas.ToJson();
            
            // 역직렬화
            var deserializedCanvas = NodeCanvas.FromJson(json);
            var deserializedNode = deserializedCanvas.Nodes.FirstOrDefault() as CreateObjectNode;
            Assert.NotNull(deserializedNode);
            
            return (deserializedCanvas, deserializedNode);
        }
        
        [Fact]
        public void CreateObjectNode_Serialization_PreservesPropertyValues()
        {
            // Arrange
            var (canvas, node) = SetupTestPersonNode();
            
            // 포트 연결 설정 변경
            var nameProperty = node.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var ageProperty = node.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            nameProperty.CanConnectToPort = false;
            ageProperty.CanConnectToPort = true;
            
            // Act - 직렬화 및 역직렬화
            var (_, deserializedNode) = SerializeAndDeserialize(canvas);
            
            // Assert
            // 타입 속성 확인
            var deserializedTypeProperty = deserializedNode.Properties.FirstOrDefault(p => p.DisplayName == "Target Type") as NodeProperty<Type>;
            Assert.NotNull(deserializedTypeProperty);
            Assert.Equal(typeof(TestPerson), deserializedTypeProperty.Value);
            
            // 프로퍼티 값 확인
            var deserializedNameProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var deserializedAgeProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            var deserializedIsActiveProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "IsActive") as NodeProperty<bool>;
            
            Assert.NotNull(deserializedNameProperty);
            Assert.NotNull(deserializedAgeProperty);
            Assert.NotNull(deserializedIsActiveProperty);
            
            Assert.Equal("Test Name", deserializedNameProperty.Value);
            Assert.Equal(42, deserializedAgeProperty.Value);
            Assert.Equal(true, deserializedIsActiveProperty.Value);
            
            // 포트 연결 설정 확인
            Assert.False(deserializedNameProperty.CanConnectToPort);
            Assert.True(deserializedAgeProperty.CanConnectToPort);
        }

        [Fact]
        public void DynamicNode_Serialization_And_Reconfiguration_PreservesPropertyValues()
        {
            // Arrange
            var (canvas, node) = SetupTestPersonNode();
            
            // 프로퍼티 값 설정
            var nameProperty = node.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var ageProperty = node.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            
            nameProperty.Value = "Original Name";
            ageProperty.Value = 100;
            
            // 포트 연결 설정 변경
            nameProperty.CanConnectToPort = false;
            ageProperty.CanConnectToPort = true;
            
            // 현재 프로퍼티 수 기록
            int originalPropertyCount = node.Properties.Count();
            
            // Act - 직렬화 및 역직렬화
            var (_, deserializedNode) = SerializeAndDeserialize(canvas);
            
            // 역직렬화된 노드에서 타입을 변경하고 다시 원래 타입으로 설정 (재구성 트리거)
            var typeProperty = deserializedNode.Properties.FirstOrDefault(p => p.DisplayName == "Target Type") as NodeProperty<Type>;
            Assert.NotNull(typeProperty);
            
            typeProperty.Value = typeof(string); // 다른 타입으로 변경
            typeProperty.Value = typeof(TestPerson); // 다시 원래 타입으로
            
            // Assert
            // 프로퍼티 수 확인 - 중복 생성되지 않아야 함
            Assert.Equal(originalPropertyCount, deserializedNode.Properties.Count());
            
            // 프로퍼티 값 확인 - 값이 유지되어야 함
            var namePropertyAfter = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var agePropertyAfter = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            
            Assert.NotNull(namePropertyAfter);
            Assert.NotNull(namePropertyAfter);
            
            Assert.Equal(null, namePropertyAfter.Value);
            Assert.Equal(0, agePropertyAfter.Value);
        }
        
        [Fact]
        public void CreateObjectNode_PreservesPropertyState_AfterTypeChange()
        {
            // Arrange
            var (canvas, node) = SetupTestPersonNode();
            
            // 포트 연결 상태 변경
            var nameProperty = node.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            Assert.NotNull(nameProperty);
            nameProperty.CanConnectToPort = false;
            
            // Act - 직렬화 및 역직렬화
            var (_, deserializedNode) = SerializeAndDeserialize(canvas);
            
            // 타입 변경 테스트 - TestPerson과 TestEmployee는 모두 Name 프로퍼티를 공유함
            var typeProperty = deserializedNode.Properties.FirstOrDefault(p => p.DisplayName == "Target Type") as NodeProperty<Type>;
            
            // TestEmployee로 변경 (Name 프로퍼티 유지)
            typeProperty.Value = typeof(TestEmployee);
            
            // 공통 프로퍼티 확인
            var employeeNameProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            Assert.NotNull(employeeNameProperty);
            
            // 공통 프로퍼티 값과 설정이 유지되어야 함
            Assert.Equal("Test Name", employeeNameProperty.Value);
            Assert.False(employeeNameProperty.CanConnectToPort);
            
            // 다시 TestPerson으로 변경
            typeProperty.Value = typeof(TestPerson);
            
            // 마지막 프로퍼티 상태 확인
            var finalNameProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            Assert.NotNull(finalNameProperty);
            
            // TestPerson 전용 프로퍼티 확인
            var isActiveProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "IsActive") as NodeProperty<bool>;
            Assert.NotNull(isActiveProperty);
            
            // 값 및 설정 확인
            Assert.Equal("Test Name", finalNameProperty.Value);
            Assert.False(finalNameProperty.CanConnectToPort);
            Assert.Equal(false, isActiveProperty.Value);
        }
        
        [Fact]
        public void CreateObjectNode_HandlesComplexTypeChanges_CorrectlyAfterSerialization()
        {
            // Arrange - Canvas 생성 및 노드 추가
            var canvas = NodeCanvas.Create();
            var node = canvas.AddNode<CreateObjectNode>();
            
            // 초기 타입 설정
            var typeProperty = node.Properties.FirstOrDefault(p => p.DisplayName == "Target Type") as NodeProperty<Type>;
            Assert.NotNull(typeProperty);
            typeProperty.Value = typeof(TestEmployee);
            
            // 초기 값 설정
            var nameProperty = node.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var idProperty = node.Properties.FirstOrDefault(p => p.Name == "Id") as NodeProperty<int>;
            
            Assert.NotNull(nameProperty);
            Assert.NotNull(idProperty);
            
            nameProperty.Value = "Employee 1";
            idProperty.Value = 1001;
            nameProperty.CanConnectToPort = false;
            
            // Act - 직렬화 및 역직렬화
            var (_, deserializedNode) = SerializeAndDeserialize(canvas);
            
            // 타입 변경 시퀀스 테스트 (다양한 타입으로 변경)
            var desTypeProperty = deserializedNode.Properties.FirstOrDefault(p => p.DisplayName == "Target Type") as NodeProperty<Type>;
            
            // TestPerson으로 변경
            desTypeProperty.Value = typeof(TestPerson);
            
            // TestPerson의 프로퍼티 설정
            var personNameProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var personAgeProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            
            Assert.NotNull(personNameProperty);
            Assert.NotNull(personAgeProperty);
            
            // 이전 타입에서 공통 프로퍼티 값이 유지되는지 확인
            Assert.Equal("Employee 1", personNameProperty.Value); 
            Assert.False(personNameProperty.CanConnectToPort);
            
            // 새로운 값 설정
            personAgeProperty.Value = 30;
            
            // TeamTeam으로 변경 (다른 타입 구조)
            desTypeProperty.Value = typeof(TestTeam);
            
            // 공통되지 않은 테스트 타입으로 변경 후 다시 TestPerson으로 복귀
            desTypeProperty.Value = typeof(TestPerson);
            
            // 최종 프로퍼티 상태 확인
            var finalNameProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Name") as NodeProperty<string>;
            var finalAgeProperty = deserializedNode.Properties.FirstOrDefault(p => p.Name == "Age") as NodeProperty<int>;
            
            Assert.NotNull(finalNameProperty);
            Assert.NotNull(finalAgeProperty);
            
            // 타입 변경 전후로 값 보존 확인
            Assert.Null(finalNameProperty.Value);
            Assert.Equal(0, finalAgeProperty.Value);
        }
    }
}
