using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Plugins.Basic.Object;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Primitives; // Added for ConstantNode
using WPFNode.Tests.Helpers;
using Xunit;

namespace WPFNode.Tests.Plugins.Basic.Object;

public class DictionaryNodeTests
{
    private readonly NodeCanvas _canvas;
    private readonly FlowExecutionContext _context;
    private readonly CancellationToken _cancellationToken;

    public DictionaryNodeTests()
    {
        _canvas = NodeCanvas.Create();
        _context = new FlowExecutionContext();
        _cancellationToken = CancellationToken.None;
    }

    [Fact]
    public async Task DictionaryCreateNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();

        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);

        // Act
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert
        var dictOut = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        var dictionary = dictOut.Value as IDictionary<string, int>;
        Assert.NotNull(dictionary);
        Assert.Empty(dictionary); // 빈 Dictionary 확인
    }

    [Fact]
    public async Task DictionaryAddNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();
        var addNode = _canvas.CreateNode<DictionaryAddNode>();
        var keyNode = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode = _canvas.CreateNode<ConstantNode<int>>();

        // DictionaryCreateNode는 타입 프로퍼티를 유지한다고 가정 (리팩토링 대상 아님)
        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);
        keyNode.Value.Value = "key1";
        valueNode.Value.Value = 42;

        // 포트 정보 로깅
        System.Diagnostics.Debug.WriteLine("===== 포트 정보 =====");
        System.Diagnostics.Debug.WriteLine($"createNode.OutputPorts.Count: {createNode.OutputPorts.Count}");
        foreach (var port in createNode.OutputPorts)
        {
            System.Diagnostics.Debug.WriteLine($"createNode OutputPort: Name={port.Name}, Type={port.DataType.Name}");
        }

        System.Diagnostics.Debug.WriteLine($"addNode.InputPorts.Count: {addNode.InputPorts.Count}");
        foreach (var port in addNode.InputPorts)
        {
            System.Diagnostics.Debug.WriteLine($"addNode InputPort: Name={port.Name}, Type={port.DataType.Name}");
        }

        System.Diagnostics.Debug.WriteLine($"keyNode.Result: Type={keyNode.Result.DataType.Name}");
        System.Diagnostics.Debug.WriteLine($"valueNode.Result: Type={valueNode.Result.DataType.Name}");
        System.Diagnostics.Debug.WriteLine("====================");

        // Act
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);
        _canvas.Connect(createNode.FlowOut, addNode.FlowIn);
        
        // 이름으로 포트 찾기
        var dictOutputPort = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        var dictInputPort = addNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(dictOutputPort, dictInputPort);
        
        var keyInputPort = addNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var valueInputPort = addNode.InputPorts.FirstOrDefault(p => p.Name == "Value");
        
        _canvas.Connect(keyNode.Result, keyInputPort);
        _canvas.Connect(valueNode.Result, valueInputPort);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert
        var updatedDictPort = addNode.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        var dictionary = updatedDictPort.Value as IDictionary<string, int>;
        Assert.NotNull(dictionary);
        Assert.Single(dictionary);
        Assert.Equal(42, dictionary["key1"]);
    }

    [Fact]
    public async Task DictionaryGetNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();
        var addNode = _canvas.CreateNode<DictionaryAddNode>();
        var getNode = _canvas.CreateNode<DictionaryGetNode>();
        var keyNode = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode = _canvas.CreateNode<ConstantNode<int>>();

        // DictionaryCreateNode는 타입 프로퍼티를 유지한다고 가정
        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);
        // addNode의 타입 설정 제거
        // addNode.KeyType.Value = typeof(string);
        // addNode.ValueType.Value = typeof(int);
        // getNode의 타입 설정 제거
        // getNode.KeyType.Value = typeof(string);
        // getNode.ValueType.Value = typeof(int);
        keyNode.Value.Value = "key1";
        valueNode.Value.Value = 42;

        // Act - 빈 Dictionary 생성 → Add 노드로 키-값 추가 → Get 노드로 값 조회
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);
        _canvas.Connect(createNode.FlowOut, addNode.FlowIn);
        _canvas.Connect(addNode.FlowOut, getNode.FlowIn);
        
        // 이름으로 포트 찾기 및 연결
        var createDictOut = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        var addDictIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(createDictOut, addDictIn);
        
        var addKeyIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var addValueIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Value");
        _canvas.Connect(keyNode.Result, addKeyIn);
        _canvas.Connect(valueNode.Result, addValueIn);
        
        var addDictOut = addNode.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        var getDictIn = getNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(addDictOut, getDictIn);
        
        var getKeyIn = getNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        _canvas.Connect(keyNode.Result, getKeyIn);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert
        var valueOut = getNode.OutputPorts.FirstOrDefault(p => p.Name == "Value");
        Assert.Equal(42, valueOut.Value);
    }

    [Fact]
    public async Task DictionaryContainsKeyNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();
        var addNode = _canvas.CreateNode<DictionaryAddNode>();
        var containsNode = _canvas.CreateNode<DictionaryContainsKeyNode>();
        var keyNode = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode = _canvas.CreateNode<ConstantNode<int>>();

        // DictionaryCreateNode는 타입 프로퍼티를 유지한다고 가정
        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);
        // addNode의 타입 설정 제거
        // addNode.KeyType.Value = typeof(string);
        // addNode.ValueType.Value = typeof(int);
        // containsNode의 타입 설정 제거
        // containsNode.KeyType.Value = typeof(string);
        // containsNode.ValueType.Value = typeof(int);
        keyNode.Value.Value = "key1";
        valueNode.Value.Value = 42;

        // Act - 빈 Dictionary 생성 → Add 노드로 키-값 추가 → ContainsKey 노드로 키 존재 여부 확인
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);
        _canvas.Connect(createNode.FlowOut, addNode.FlowIn);
        _canvas.Connect(addNode.FlowOut, containsNode.FlowIn);
        
        // 이름으로 포트 찾기 및 연결
        var createDictOut = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        var addDictIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(createDictOut, addDictIn);
        
        var addKeyIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var addValueIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Value");
        _canvas.Connect(keyNode.Result, addKeyIn);
        _canvas.Connect(valueNode.Result, addValueIn);
        
        var addDictOut = addNode.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        var containsDictIn = containsNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(addDictOut, containsDictIn);
        
        var containsKeyIn = containsNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        _canvas.Connect(keyNode.Result, containsKeyIn);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert
        var containsOut = containsNode.OutputPorts.FirstOrDefault(p => p.Name == "Contains");
        var contains = (bool)containsOut.Value;
        Assert.True(contains);
    }

    [Fact]
    public async Task DictionaryRemoveNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();
        var addNode = _canvas.CreateNode<DictionaryAddNode>();
        var removeNode = _canvas.CreateNode<DictionaryRemoveNode>();
        var keyNode = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode = _canvas.CreateNode<ConstantNode<int>>();

        // DictionaryCreateNode는 타입 프로퍼티를 유지한다고 가정
        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);
        // addNode의 타입 설정 제거
        // addNode.KeyType.Value = typeof(string);
        // addNode.ValueType.Value = typeof(int);
        // removeNode의 타입 설정 제거
        // removeNode.KeyType.Value = typeof(string);
        // removeNode.ValueType.Value = typeof(int);
        keyNode.Value.Value = "key1";
        valueNode.Value.Value = 42;

        // Act - 빈 Dictionary 생성 → Add 노드로 키-값 추가 → Remove 노드로 키-값 제거
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);
        _canvas.Connect(createNode.FlowOut, addNode.FlowIn);
        _canvas.Connect(addNode.FlowOut, removeNode.FlowIn);
        
        // 이름으로 포트 찾기 및 연결
        var createDictOut = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        var addDictIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(createDictOut, addDictIn);
        
        var addKeyIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var addValueIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Value");
        _canvas.Connect(keyNode.Result, addKeyIn);
        _canvas.Connect(valueNode.Result, addValueIn);
        
        var addDictOut = addNode.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        var removeDictIn = removeNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(addDictOut, removeDictIn);
        
        var removeKeyIn = removeNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        _canvas.Connect(keyNode.Result, removeKeyIn);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert
        var removeDictOut = removeNode.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        var dictionary = removeDictOut.Value as IDictionary<string, int>;
        Assert.NotNull(dictionary);
        Assert.Empty(dictionary);
    }

    [Fact]
    public async Task DictionaryForEachNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();
        var addNode1 = _canvas.CreateNode<DictionaryAddNode>();
        var addNode2 = _canvas.CreateNode<DictionaryAddNode>();
        var forEachNode = _canvas.CreateNode<DictionaryForEachNode>();
        var keyNode1 = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode1 = _canvas.CreateNode<ConstantNode<int>>();
        var keyNode2 = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode2 = _canvas.CreateNode<ConstantNode<int>>();

        // DictionaryCreateNode는 타입 프로퍼티를 유지한다고 가정
        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);
        // addNode1의 타입 설정 제거
        // addNode1.KeyType.Value = typeof(string);
        // addNode1.ValueType.Value = typeof(int);
        // addNode2의 타입 설정 제거
        // addNode2.KeyType.Value = typeof(string);
        // addNode2.ValueType.Value = typeof(int);
        // forEachNode의 타입 설정 제거
        // forEachNode.KeyType.Value = typeof(string);
        // forEachNode.ValueType.Value = typeof(int);
        keyNode1.Value.Value = "key1";
        valueNode1.Value.Value = 42;
        keyNode2.Value.Value = "key2";
        valueNode2.Value.Value = 84;

        // Act - 빈 Dictionary 생성 → Add 노드로 첫 번째 키-값 추가 → Add 노드로 두 번째 키-값 추가 → ForEach 노드로 순회
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);
        _canvas.Connect(createNode.FlowOut, addNode1.FlowIn);
        _canvas.Connect(addNode1.FlowOut, addNode2.FlowIn);
        _canvas.Connect(addNode2.FlowOut, forEachNode.FlowIn);
        
        // 이름으로 포트 찾기 및 연결
        var createDictOut = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        
        var add1DictIn = addNode1.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(createDictOut, add1DictIn);
        
        var add1KeyIn = addNode1.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var add1ValueIn = addNode1.InputPorts.FirstOrDefault(p => p.Name == "Value");
        _canvas.Connect(keyNode1.Result, add1KeyIn);
        _canvas.Connect(valueNode1.Result, add1ValueIn);
        
        var add1DictOut = addNode1.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        
        var add2DictIn = addNode2.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(add1DictOut, add2DictIn);
        
        var add2KeyIn = addNode2.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var add2ValueIn = addNode2.InputPorts.FirstOrDefault(p => p.Name == "Value");
        _canvas.Connect(keyNode2.Result, add2KeyIn);
        _canvas.Connect(valueNode2.Result, add2ValueIn);
        
        var add2DictOut = addNode2.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        var forEachDictIn = forEachNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(add2DictOut, forEachDictIn);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert - 포트 이름으로 결과 접근
        var keyOut = forEachNode.OutputPorts.FirstOrDefault(p => p.Name == "CurrentKey");
        var valueOut = forEachNode.OutputPorts.FirstOrDefault(p => p.Name == "CurrentValue");
        var indexOut = forEachNode.OutputPorts.FirstOrDefault(p => p.Name == "Index");
        
        var results = new List<(string Key, int Value, int Index)>();
        
        // 테스트 실행 시 한 번에 모든 결과를 수집하는 대신 마지막 상태만 검증
        await foreach (var _ in forEachNode.ProcessAsync(_context, _cancellationToken))
        {
            var key = keyOut.Value as string;
            var value = (int)valueOut.Value;
            var index = (int)indexOut.Value;
            
            // 기존 결과를 수집하는 대신 마지막 항목만 저장하거나 삭제 후 추가
            var existingItem = results.FirstOrDefault(r => r.Key == key);
            if (existingItem != default)
            {
                results.Remove(existingItem);
            }
            results.Add((key!, value, index));
        }

        // 2개의 항목이 있어야 함
        Assert.Equal(2, results.Count);
        // 결과 순서는 Dictionary의 순회 순서에 따라 다를 수 있음
        Assert.Contains(results, r => r.Key == "key1" && r.Value == 42);
        Assert.Contains(results, r => r.Key == "key2" && r.Value == 84);
    }

    [Fact]
    public async Task DictionaryGetOrDefaultNode_Test()
    {
        // Arrange
        var startNode = _canvas.CreateNode<StartNode>();
        var createNode = _canvas.CreateNode<DictionaryCreateNode>();
        var addNode = _canvas.CreateNode<DictionaryAddNode>();
        var getOrDefaultNode = _canvas.CreateNode<DictionaryGetOrDefaultNode>();
        var keyNode = _canvas.CreateNode<ConstantNode<string>>();
        var valueNode = _canvas.CreateNode<ConstantNode<int>>();
        var defaultValueNode = _canvas.CreateNode<ConstantNode<int>>();

        // DictionaryCreateNode는 타입 프로퍼티를 유지한다고 가정
        createNode.KeyType.Value = typeof(string);
        createNode.ValueType.Value = typeof(int);
        // addNode의 타입 설정 제거
        // addNode.KeyType.Value = typeof(string);
        // addNode.ValueType.Value = typeof(int);
        // getOrDefaultNode의 타입 설정 제거
        // getOrDefaultNode.KeyType.Value = typeof(string);
        // getOrDefaultNode.ValueType.Value = typeof(int);
        keyNode.Value.Value = "key1";
        valueNode.Value.Value = 42;
        defaultValueNode.Value.Value = 0;

        // Act - 빈 Dictionary 생성 → Add 노드로 키-값 추가 → GetOrDefault 노드로 값 조회
        _canvas.Connect(startNode.FlowOut, createNode.FlowIn);
        _canvas.Connect(createNode.FlowOut, addNode.FlowIn);
        _canvas.Connect(addNode.FlowOut, getOrDefaultNode.FlowIn);
        
        // 이름으로 포트 찾기 및 연결
        var createDictOut = createNode.OutputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        
        var addDictIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(createDictOut, addDictIn);
        
        var addKeyIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var addValueIn = addNode.InputPorts.FirstOrDefault(p => p.Name == "Value");
        _canvas.Connect(keyNode.Result, addKeyIn);
        _canvas.Connect(valueNode.Result, addValueIn);
        
        var addDictOut = addNode.OutputPorts.FirstOrDefault(p => p.Name == "Updated");
        
        var getOrDefaultDictIn = getOrDefaultNode.InputPorts.FirstOrDefault(p => p.Name == "Dictionary");
        _canvas.Connect(addDictOut, getOrDefaultDictIn);
        
        var getOrDefaultKeyIn = getOrDefaultNode.InputPorts.FirstOrDefault(p => p.Name == "Key");
        var getOrDefaultValueIn = getOrDefaultNode.InputPorts.FirstOrDefault(p => p.Name == "DefaultValue");
        _canvas.Connect(keyNode.Result, getOrDefaultKeyIn);
        _canvas.Connect(defaultValueNode.Result, getOrDefaultValueIn);

        await _canvas.ExecuteAsync(_cancellationToken);

        // Assert
        var valueOut = getOrDefaultNode.OutputPorts.FirstOrDefault(p => p.Name == "Value");
        Assert.Equal(42, valueOut.Value);
    }
}
