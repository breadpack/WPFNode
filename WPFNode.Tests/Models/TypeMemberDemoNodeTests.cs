using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Nodes;
using WPFNode.Interfaces;
using WPFNode.Models.Serialization;

namespace WPFNode.Tests.Models;

[TestClass]
public class TypeMemberDemoNodeTests
{
    private NodeCanvas _canvas;
    private TypeMemberDemoNode _node;
    private JsonSerializerOptions _jsonOptions;

    [TestInitialize]
    public void Setup()
    {
        _canvas = NodeCanvas.Create();
        _node = _canvas.CreateNode<TypeMemberDemoNode>();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new NodeCanvasJsonConverter() }
        };
    }

    [TestMethod]
    public void Constructor_ShouldInitializeWithDefaultType()
    {
        // 기본 생성자 호출 시 string 타입으로 초기화되어야 함
        Assert.IsNotNull(_node.Properties["selectedType"]);
        Assert.AreEqual(typeof(string), _node.Properties["selectedType"].Value);
        
        // string 타입의 멤버들이 프로퍼티로 생성되어야 함
        var stringMembers = typeof(string).GetProperties()
            .Select(p => p.Name)
            .ToList();
        
        foreach (var member in stringMembers)
        {
            Assert.IsTrue(_node.Properties.ContainsKey(member), $"프로퍼티 {member}가 생성되지 않았습니다.");
        }
    }

    [TestMethod]
    public void TypeChange_ShouldResetProperties()
    {
        // 1. 초기 string 타입의 프로퍼티 중 하나에 값 설정
        var lengthProp = _node.Properties["Length"];
        lengthProp.Value = 10;

        // 2. 타입을 DateTime으로 변경
        _node.Properties["selectedType"].Value = typeof(DateTime);

        // 3. 이전 프로퍼티들이 제거되고 새로운 프로퍼티들이 생성되었는지 확인
        Assert.IsFalse(_node.Properties.ContainsKey("Length"), "이전 타입의 프로퍼티가 남아있습니다.");
        
        var dateTimeMembers = typeof(DateTime).GetProperties()
            .Select(p => p.Name)
            .ToList();
        
        foreach (var member in dateTimeMembers)
        {
            Assert.IsTrue(_node.Properties.ContainsKey(member), $"새 타입의 프로퍼티 {member}가 생성되지 않았습니다.");
        }
    }

    [TestMethod]
    public void Serialization_ShouldPreservePropertyValues()
    {
        // 1. DateTime 타입으로 변경하고 프로퍼티 값 설정
        _node.Properties["selectedType"].Value = typeof(DateTime);
        _node.Properties["Year"].Value = 2024;
        _node.Properties["Month"].Value = 3;
        
        // 2. 직렬화
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        
        // 3. 역직렬화
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);
        var loadedNode = (TypeMemberDemoNode)loadedCanvas.SerializableNodes.First();
        
        // 4. 프로퍼티 값이 보존되었는지 확인
        Assert.AreEqual(typeof(DateTime), loadedNode.Properties["selectedType"].Value);
        Assert.AreEqual(2024, loadedNode.Properties["Year"].Value);
        Assert.AreEqual(3, loadedNode.Properties["Month"].Value);
    }

    [TestMethod]
    public void Serialization_ShouldPreservePortConnections()
    {
        // 1. 테스트를 위한 소스 노드 생성 (TypeSelectorDemoNode 사용)
        var sourceNode = _canvas.CreateNode<TypeSelectorDemoNode>();
        sourceNode.Properties["selectedType"].Value = typeof(string);
        
        // 2. 대상 노드의 타입을 string으로 설정
        _node.Properties["selectedType"].Value = typeof(string);
        
        // 3. 포트 연결 (TypeSelectorDemoNode의 baseType 출력 포트를 TypeMemberDemoNode의 selectedType 프로퍼티에 연결)
        var sourcePort = sourceNode.OutputPorts.First(p => p.Name == "Base Type");
        var targetProperty = _node.Properties["selectedType"];
        
        Assert.IsNotNull(sourcePort, "소스 포트가 null입니다.");
        Assert.IsNotNull(targetProperty, "타겟 프로퍼티가 null입니다.");
        
        // 프로퍼티를 포트로 설정
        targetProperty.CanConnectToPort = true;
        
        // 포트로 캐스팅
        var targetPort = targetProperty as IInputPort;
        Assert.IsNotNull(targetPort, "타겟 프로퍼티를 IInputPort로 캐스팅할 수 없습니다.");
        
        var connection = targetPort.Connect(sourcePort);
        
        // 4. 직렬화
        var json = JsonSerializer.Serialize(_canvas, _jsonOptions);
        
        // 5. 역직렬화
        var loadedCanvas = JsonSerializer.Deserialize<NodeCanvas>(json, _jsonOptions);
        var loadedSourceNode = loadedCanvas.SerializableNodes
            .First(n => n.Id == sourceNode.Id) as TypeSelectorDemoNode;
        var loadedTargetNode = loadedCanvas.SerializableNodes
            .First(n => n.Id == _node.Id) as TypeMemberDemoNode;
        
        // 6. 연결이 보존되었는지 확인
        Assert.IsNotNull(loadedSourceNode, "소스 노드가 로드되지 않았습니다.");
        Assert.IsNotNull(loadedTargetNode, "타겟 노드가 로드되지 않았습니다.");
        
        var loadedSourcePort = loadedSourceNode.OutputPorts.First(p => p.Name == "Base Type");
        var loadedTargetProperty = loadedTargetNode.Properties["selectedType"];
        
        Assert.IsNotNull(loadedSourcePort, "로드된 소스 포트가 null입니다.");
        Assert.IsNotNull(loadedTargetProperty, "로드된 타겟 프로퍼티가 null입니다.");
        
        Assert.IsTrue(loadedTargetProperty.IsConnectedToPort, "포트 연결이 보존되지 않았습니다.");
        Assert.IsTrue(loadedSourcePort.IsConnected, "소스 포트 연결이 보존되지 않았습니다.");
    }

    [TestMethod]
    public async Task ProcessAsync_ShouldComplete()
    {
        // ProcessAsync가 예외 없이 완료되는지 확인
        await _node.ProcessAsync();
    }
} 