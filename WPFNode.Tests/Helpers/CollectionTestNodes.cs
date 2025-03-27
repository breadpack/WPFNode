using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Tests.Helpers
{
    // 컬렉션 출력 노드 - 다양한 타입의 컬렉션 생성을 위한 노드
    public class CollectionOutputNode<T> : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeProperty("Source Items")]
        public List<T> SourceItems { get; set; } = new List<T>();
        
        [NodeOutput("List")]
        public OutputPort<List<T>> OutputList { get; private set; } = null!;
        
        [NodeOutput("Array")]
        public OutputPort<T[]> OutputArray { get; private set; } = null!;
        
        [NodeOutput("HashSet")]
        public OutputPort<HashSet<T>> OutputHashSet { get; private set; } = null!;
        
        [NodeOutput("IEnumerable")]
        public OutputPort<IEnumerable<T>> OutputIEnumerable { get; private set; } = null!;
        
        public CollectionOutputNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            Name = "Collection Output";
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // 모든 출력 포트에 동일한 데이터를 다양한 컬렉션 타입으로 설정
            OutputList.Value = SourceItems.ToList();
            OutputArray.Value = SourceItems.ToArray();
            OutputHashSet.Value = new HashSet<T>(SourceItems);
            OutputIEnumerable.Value = SourceItems.AsEnumerable();
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
    
    // 컬렉션 입력 노드 - 다양한 타입의 컬렉션을 입력 받고 추적하는 노드
    public class CollectionInputNode<T> : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("List")]
        public InputPort<List<T>> InputList { get; private set; } = null!;
        
        [NodeInput("Array")]
        public InputPort<T[]> InputArray { get; private set; } = null!;
        
        [NodeInput("HashSet")]
        public InputPort<HashSet<T>> InputHashSet { get; private set; } = null!;
        
        [NodeInput("IEnumerable")]
        public InputPort<IEnumerable<T>> InputIEnumerable { get; private set; } = null!;
        
        // 수신된 데이터 (테스트 검증용)
        public List<T>? ReceivedList { get; private set; }
        public T[]? ReceivedArray { get; private set; }
        public HashSet<T>? ReceivedHashSet { get; private set; }
        public IEnumerable<T>? ReceivedIEnumerable { get; private set; }
        
        // 수신 여부 추적
        public bool ListReceived { get; private set; }
        public bool ArrayReceived { get; private set; }
        public bool HashSetReceived { get; private set; }
        public bool IEnumerableReceived { get; private set; }
        
        public CollectionInputNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            Name = "Collection Input";
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // 모든 입력 포트로부터 데이터 수신 및 저장
            if (InputList.IsConnected)
            {
                ReceivedList = InputList.GetValueOrDefault();
                ListReceived = ReceivedList != null;
            }
            
            if (InputArray.IsConnected)
            {
                ReceivedArray = InputArray.GetValueOrDefault();
                ArrayReceived = ReceivedArray != null;
            }
            
            if (InputHashSet.IsConnected)
            {
                ReceivedHashSet = InputHashSet.GetValueOrDefault();
                HashSetReceived = ReceivedHashSet != null;
            }
            
            if (InputIEnumerable.IsConnected)
            {
                ReceivedIEnumerable = InputIEnumerable.GetValueOrDefault();
                IEnumerableReceived = ReceivedIEnumerable != null;
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }

    // 특정 컬렉션 타입 변환 노드 (List -> Array -> HashSet -> IEnumerable)
    public class CollectionTransformNode<T> : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Source")]
        public InputPort<IEnumerable<T>> InputSource { get; private set; } = null!;
        
        [NodeOutput("ToList")]
        public OutputPort<List<T>> ToList { get; private set; } = null!;
        
        [NodeOutput("ToArray")]
        public OutputPort<T[]> ToArray { get; private set; } = null!;
        
        [NodeOutput("ToHashSet")]
        public OutputPort<HashSet<T>> ToHashSet { get; private set; } = null!;
        
        public CollectionTransformNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            Name = "Collection Transform";
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            var source = InputSource.GetValueOrDefault();
            
            if (source != null)
            {
                // 각 출력 포트에 변환된 컬렉션 설정
                ToList.Value = source.ToList();
                ToArray.Value = source.ToArray();
                ToHashSet.Value = new HashSet<T>(source);
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }

    // 컬렉션 값 추적 및 검증 노드
    public class CollectionValidationNode<T> : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Collection")]
        public InputPort<IEnumerable<T>> InputCollection { get; private set; } = null!;

        // 컬렉션 항목 검증 결과
        public List<T> ReceivedItems { get; private set; } = new List<T>();
        public int ItemCount { get; private set; }
        public bool WasReceived { get; private set; }
        
        public CollectionValidationNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            Name = "Collection Validation";
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            var collection = InputCollection.GetValueOrDefault();
            
            if (collection != null)
            {
                WasReceived = true;
                ReceivedItems = collection.ToList();
                ItemCount = ReceivedItems.Count;
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
    
    /// <summary>
    /// 상수 값을 출력하는 노드입니다.
    /// </summary>
    /// <typeparam name="T">상수의 데이터 타입</typeparam>
    [NodeCategory("Constants")]
    [NodeDescription("상수 값을 출력합니다.")]
    public class ConstantNode<T> : NodeBase
    {
        [NodeProperty("Value")]
        public NodeProperty<T> Value { get; private set; }

        /// <summary>
        /// 상수 값 출력 포트
        /// </summary>
        [NodeOutput("Result")]
        public OutputPort<T> Result { get; private set; }

        public ConstantNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }

        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken     cancellationToken
        )
        {
            // Value.Value에서 값을 가져와 Result.Value에 설정
            Debug.WriteLine($"ConstantNode.ProcessAsync: Value={Value}, Type={typeof(T).Name}");
        
            if (Value != null && Result != null)
            {
                Result.Value = Value.Value;
                Debug.WriteLine($"ConstantNode.ProcessAsync: 값 설정 완료, Result 값이 설정됨");
            }
            else
            {
                Debug.WriteLine($"ConstantNode.ProcessAsync: Value 또는 Result가 null입니다. Value={Value != null}, Result={Result != null}");
            }
        
            // 필요한 비동기 작업을 처리하기 위한 대기
            yield break;
        }
    } 
}
