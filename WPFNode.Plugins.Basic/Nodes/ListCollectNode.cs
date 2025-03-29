using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes {
    [NodeName("List.Collect")]
    [NodeDescription("순차적으로 실행될 때마다 항목을 수집하여 리스트를 생성합니다.")]
    [NodeCategory("컬렉션")]
    public class ListCollectNode : DynamicNode {
        [NodeFlowIn("Add")]
        public IFlowInPort AddFlowIn { get; set; }

        [NodeFlowIn("Clear")]
        public IFlowInPort ClearFlowIn { get; set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }

        [NodeProperty("요소 타입", OnValueChanged = nameof(ElementType_Changed))]
        public NodeProperty<Type> ElementType { get; set; }

        private IInputPort  _itemInput;
        private IOutputPort _listOutput;
        private object      _collectedList;

        public ListCollectNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
            Name        = "List.Collect";
            Description = "순차적으로 실행될 때마다 항목을 수집하여 리스트를 생성합니다.";
        }

        private void ElementType_Changed() {
            ReconfigurePorts();
        }

        protected override void Configure(NodeBuilder builder) {
            var elementType = ElementType?.Value ?? typeof(object);
            var listType    = typeof(List<>).MakeGenericType(elementType);

            _itemInput     = builder.Input("항목", elementType);
            _listOutput    = builder.Output("리스트", listType);
            _collectedList = Activator.CreateInstance(listType)!;
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            FlowExecutionContext? context,
            CancellationToken     cancellationToken = default
        ) {
            // 활성화된 Flow In 포트 확인
            var activeFlowInPort = context?.ActiveFlowInPort;

            if (activeFlowInPort == ClearFlowIn) {
                // Clear 포트가 활성화된 경우 리스트 초기화
                var clearMethod = _collectedList.GetType().GetMethod("Clear");
                clearMethod?.Invoke(_collectedList, null);
            }
            else if (activeFlowInPort == AddFlowIn) {
                // Add 포트가 활성화된 경우 항목 추가
                var itemValue = ((dynamic)_itemInput).GetValueOrDefault();

                // 항목 추가 전 디버그 출력
                System.Diagnostics.Debug.WriteLine($"ListCollectNode: 항목 추가 중 - {itemValue}, 타입: {(itemValue != null ? itemValue.GetType().Name : "null")}");

                var addMethod = _collectedList.GetType().GetMethod("Add");
                addMethod?.Invoke(_collectedList, [itemValue]);

                // 항목 추가 후 현재 컬렉션 크기 출력
                var countProp    = _collectedList.GetType().GetProperty("Count");
                int currentCount = countProp != null ? (int)countProp.GetValue(_collectedList)! : -1;

                // 카운트가 변경되었는지 확인
                System.Diagnostics.Debug.WriteLine($"ListCollectNode: 현재 항목 수 - {currentCount}, 리스트 HashCode: {_collectedList.GetHashCode()}");
            }

            _listOutput.Value = _collectedList;
            // 플로우 출력
            yield return FlowOut;
        }
    }
}