using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("HashSet.Create")]
    [NodeDescription("빈 해시셋을 생성합니다.")]
    [NodeCategory("컬렉션")]
    public class HashSetCreateNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }
        
        private IOutputPort _hashSetOutput;
        
        public HashSetCreateNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "HashSet.Create";
            Description = "빈 해시셋을 생성합니다.";
        }
        
        private void ElementType_Changed()
        {
            ReconfigurePorts();
        }
        
        protected override void Configure(NodeBuilder builder)
        {
            var elementType = ElementType?.Value ?? typeof(object);
            var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
            
            _hashSetOutput = builder.Output("해시셋", hashSetType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            var elementType = ElementType?.Value ?? typeof(object);
            var hashSetType = typeof(HashSet<>).MakeGenericType(elementType);
            
            // 빈 해시셋 생성
            var hashSet = Activator.CreateInstance(hashSetType);
            _hashSetOutput.Value = hashSet;
            
            yield return FlowOut;
        }
    }
} 