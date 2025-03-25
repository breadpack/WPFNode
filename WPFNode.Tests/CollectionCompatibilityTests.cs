using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Nodes;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Primitives;
using WPFNode.Tests.Helpers;
using WPFNode.Tests.Models;
using Xunit;

namespace WPFNode.Tests
{
    public class CollectionCompatibilityTests
    {
        private readonly ILogger _logger;

        public CollectionCompatibilityTests()
        {
            // 로깅 설정
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<CollectionCompatibilityTests>();
        }

        [Fact]
        public async Task OutputPort_List_CanConnectTo_InputPort_Array()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var outputNode = canvas.AddNode<CollectionOutputNode<int>>(100, 0);
            var validationNode = canvas.AddNode<CollectionValidationNode<int>>(200, 0);

            // 3. 노드 설정 - 소스 아이템 설정
            outputNode.SourceItems = new List<int> { 1, 2, 3, 4, 5 };

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(validationNode.FlowIn);
            
            // List<int> 타입의 출력을 IEnumerable<int> 타입의 입력으로 연결
            outputNode.OutputList.Connect(validationNode.InputCollection);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(5, validationNode.ItemCount);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, validationNode.ReceivedItems);
        }

        [Fact]
        public async Task OutputPort_Array_CanConnectTo_InputPort_List()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var outputNode = canvas.AddNode<CollectionOutputNode<string>>(100, 0);
            var validationNode = canvas.AddNode<CollectionValidationNode<string>>(200, 0);

            // 3. 노드 설정 - 소스 아이템 설정
            outputNode.SourceItems = new List<string> { "A", "B", "C" };

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(validationNode.FlowIn);
            
            // string[] 타입의 출력을 IEnumerable<string> 타입의 입력으로 연결
            outputNode.OutputArray.Connect(validationNode.InputCollection);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(3, validationNode.ItemCount);
            Assert.Equal(new[] { "A", "B", "C" }, validationNode.ReceivedItems);
        }

        [Fact]
        public async Task OutputPort_HashSet_CanConnectTo_InputPort_IEnumerable()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var outputNode = canvas.AddNode<CollectionOutputNode<int>>(100, 0);
            var validationNode = canvas.AddNode<CollectionValidationNode<int>>(200, 0);

            // 3. 노드 설정 - 소스 아이템 설정 (중복 값 포함)
            outputNode.SourceItems = new List<int> { 1, 2, 3, 3, 2, 1 };

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(validationNode.FlowIn);
            
            // HashSet<int> 타입의 출력을 IEnumerable<int> 타입의 입력으로 연결
            outputNode.OutputHashSet.Connect(validationNode.InputCollection);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - HashSet은 중복을 제거하므로 3개 항목만 있어야 함
            Assert.True(validationNode.WasReceived);
            Assert.Equal(3, validationNode.ItemCount);
            Assert.Contains(1, validationNode.ReceivedItems);
            Assert.Contains(2, validationNode.ReceivedItems);
            Assert.Contains(3, validationNode.ReceivedItems);
        }

        [Fact]
        public async Task OutputPort_IEnumerable_CanConnectTo_InputPorts_OfCompatibleCollections()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var outputNode = canvas.AddNode<CollectionOutputNode<int>>(100, 0);
            var transformNode = canvas.AddNode<CollectionTransformNode<int>>(200, 0);
            var validationNode = canvas.AddNode<CollectionValidationNode<int>>(300, 0);

            // 3. 노드 설정 - 소스 아이템 설정
            outputNode.SourceItems = new List<int> { 10, 20, 30, 40, 50 };

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(transformNode.FlowIn);
            transformNode.FlowOut.Connect(validationNode.FlowIn);
            
            // IEnumerable<int> 타입의 출력을 변환 노드의 입력으로 연결
            outputNode.OutputIEnumerable.Connect(transformNode.InputSource);
            
            // 변환된 컬렉션(여기서는 리스트)을 검증 노드로 전달
            transformNode.ToList.Connect(validationNode.InputCollection);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(5, validationNode.ItemCount);
            Assert.Equal(new[] { 10, 20, 30, 40, 50 }, validationNode.ReceivedItems);
        }

        [Fact]
        public async Task TestAllCollectionTypesCompatibility()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var outputNode = canvas.AddNode<CollectionOutputNode<int>>(100, 0);
            var inputNode = canvas.AddNode<CollectionInputNode<int>>(200, 0);

            // 3. 노드 설정 - 소스 아이템 설정
            outputNode.SourceItems = new List<int> { 1, 2, 3, 4, 5 };

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(inputNode.FlowIn);
            
            // 다양한 출력 포트를 다양한 입력 포트에 연결
            outputNode.OutputList.Connect(inputNode.InputArray);      // List -> Array
            outputNode.OutputArray.Connect(inputNode.InputList);      // Array -> List
            outputNode.OutputHashSet.Connect(inputNode.InputIEnumerable);  // HashSet -> IEnumerable
            // 모든 포트에 연결하지 않고 일부만 연결하여 테스트

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인 - 연결된 포트들만 값을 수신해야 함
            Assert.True(inputNode.ArrayReceived);
            Assert.True(inputNode.ListReceived);
            Assert.True(inputNode.IEnumerableReceived);
            Assert.False(inputNode.HashSetReceived);  // 연결하지 않았으므로 false여야 함
            
            // 수신된 컬렉션들의 항목 수 확인
            Assert.Equal(5, inputNode.ReceivedArray?.Length);
            Assert.Equal(5, inputNode.ReceivedList?.Count);
            Assert.Equal(5, inputNode.ReceivedIEnumerable?.Count());
        }

        [Fact]
        public async Task OutputPort_ListOfCustomType_CanConnectTo_InputPort_ArrayOfSameType()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            // TestPerson 타입의 컬렉션을 다루는 노드들
            var outputNode = canvas.AddNode<CollectionOutputNode<TestPerson>>(100, 0);
            var validationNode = canvas.AddNode<CollectionValidationNode<TestPerson>>(200, 0);

            // 3. 노드 설정 - 소스 아이템 설정
            var people = new List<TestPerson>
            {
                new TestPerson { Name = "홍길동", Age = 30 },
                new TestPerson { Name = "김철수", Age = 25 },
                new TestPerson { Name = "이영희", Age = 28 }
            };
            outputNode.SourceItems = people;

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(validationNode.FlowIn);
            
            // List<TestPerson> 타입의 출력을 IEnumerable<TestPerson> 타입의 입력으로 연결
            outputNode.OutputList.Connect(validationNode.InputCollection);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(3, validationNode.ItemCount);
            
            // 각 원소가 제대로 전달되었는지 확인
            Assert.Contains(validationNode.ReceivedItems, p => p.Name == "홍길동" && p.Age == 30);
            Assert.Contains(validationNode.ReceivedItems, p => p.Name == "김철수" && p.Age == 25);
            Assert.Contains(validationNode.ReceivedItems, p => p.Name == "이영희" && p.Age == 28);
        }

        [Fact]
        public async Task CollectionTransform_ConvertsFromOneTypeTo_Another()
        {
            // 1. 새 캔버스 생성
            var canvas = NodeCanvas.Create();

            // 2. 노드 추가
            var startNode = canvas.AddNode<StartNode>(0, 0);
            var outputNode = canvas.AddNode<CollectionOutputNode<string>>(100, 0);
            var transformNode = canvas.AddNode<CollectionTransformNode<string>>(200, 0);
            var validationNode = canvas.AddNode<CollectionValidationNode<string>>(300, 0);

            // 3. 노드 설정 - 소스 아이템 설정
            outputNode.SourceItems = new List<string> { "A", "B", "C", "D" };

            // 4. 노드 연결
            startNode.FlowOut.Connect(outputNode.FlowIn);
            outputNode.FlowOut.Connect(transformNode.FlowIn);
            transformNode.FlowOut.Connect(validationNode.FlowIn);
            
            // List<string>을 변환 노드의 IEnumerable<string> 입력에 연결
            outputNode.OutputList.Connect(transformNode.InputSource);
            
            // 변환된 HashSet<string>을 검증 노드에 연결
            transformNode.ToHashSet.Connect(validationNode.InputCollection);

            // 5. 실행
            await canvas.ExecuteAsync();

            // 6. 결과 확인
            Assert.True(validationNode.WasReceived);
            Assert.Equal(4, validationNode.ItemCount);
            Assert.Equal(new[] { "A", "B", "C", "D" }, validationNode.ReceivedItems.OrderBy(s => s).ToArray());
        }
    }
}
