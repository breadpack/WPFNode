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
    [NodeName("List.Collect")]
    [NodeDescription("순차적으로 실행될 때마다 항목을 수집하여 리스트를 생성합니다.")]
    [NodeCategory("컬렉션")]
    public class ListCollectNode : DynamicNode
    {
        [NodeFlowIn("Add")]
        public IFlowInPort AddFlowIn { get; set; }
        
        [NodeFlowIn("Clear")]
        public IFlowInPort ClearFlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }
        
        private IInputPort _itemInput;
        private IOutputPort _listOutput;
        private object _collectedList = null;
        
        public ListCollectNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "List.Collect";
            Description = "순차적으로 실행될 때마다 항목을 수집하여 리스트를 생성합니다.";
        }
        
        private void ElementType_Changed()
        {
            ReconfigurePorts();
            _collectedList = null;
        }
        
        protected override void Configure(NodeBuilder builder)
        {
            var elementType = ElementType?.Value ?? typeof(object);
            var listType = typeof(List<>).MakeGenericType(elementType);
            
            _itemInput = builder.Input("항목", elementType);
            _listOutput = builder.Output("리스트", listType);
            
            // 컬렉션 리스트가 아직 생성되지 않았다면 생성
            if (_collectedList == null)
            {
                _collectedList = Activator.CreateInstance(listType);
                UpdateOutputValue();
            }
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            // 활성화된 Flow In 포트 확인
            var activeFlowInPort = context?.ActiveFlowInPort;
            
            if (activeFlowInPort == ClearFlowIn)
            {
                // Clear 포트가 활성화된 경우 리스트 초기화
                var elementType = ElementType?.Value ?? typeof(object);
                var listType = typeof(List<>).MakeGenericType(elementType);
                _collectedList = Activator.CreateInstance(listType);
            }
            else if (activeFlowInPort == AddFlowIn)
            {
                // Add 포트가 활성화된 경우 항목 추가
                var itemValue = ((dynamic)_itemInput).GetValueOrDefault();
                if (itemValue != null && _collectedList != null)
                {
                    var addMethod = _collectedList.GetType().GetMethod("Add");
                    addMethod?.Invoke(_collectedList, new[] { itemValue });
                }
            }
            
            // 출력 업데이트
            UpdateOutputValue();
            
            // 플로우 출력
            yield return FlowOut;
        }
        
        private void UpdateOutputValue()
        {
            if (_collectedList != null)
            {
                ((dynamic)_listOutput).Value = _collectedList;
            }
        }
    }
}
