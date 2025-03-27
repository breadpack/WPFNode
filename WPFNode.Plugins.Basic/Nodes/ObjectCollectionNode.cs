using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes {
    [NodeName("Object Collection")]
    [NodeDescription("객체 리스트를 생성합니다.")]
    [NodeCategory("데이터 변환")]
    public class ObjectCollectionNode : DynamicNode {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("Target Type", OnValueChanged = nameof(SelectedType_PropertyChanged))]
        NodeProperty<Type> SelectedType { get; set; }

        [NodeProperty("Item Count", OnValueChanged = nameof(ItemCount_PropertyChanged))]
        NodeProperty<int> ItemCount { get; set; }

        private readonly List<IInputPort> _itemInputPorts = [];
        private IOutputPort? _outputPort;

        [JsonConstructor]
        public ObjectCollectionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
            Name = "Object Collection";
            Description = "객체 리스트를 생성합니다.";
        }

        private void SelectedType_PropertyChanged() {
            ReconfigurePorts();
        }

        private void ItemCount_PropertyChanged() {
            ReconfigurePorts();
        }
        
        protected override void Configure(NodeBuilder builder) {
            // 기존 항목 입력 포트 목록 초기화
            _itemInputPorts.Clear();
            
            // 입력/출력 포트 구성
            var targetType = SelectedType?.Value ?? typeof(object);
            var itemCount = ItemCount?.Value ?? 0;
            
            // 리스트 타입의 출력 포트 구성
            var listType = typeof(List<>).MakeGenericType(targetType);
            _outputPort = builder.Output("Collection", listType);
            
            if (targetType == null || itemCount <= 0) return;
            
            // 각 항목에 대해 입력 포트 구성
            for (int i = 0; i < itemCount; i++) {
                int index = i + 1;
                string portName = $"Item {index}";
                
                // 타입 자체를 입력 포트로 구성
                var inputPort = builder.Input(portName, targetType);
                _itemInputPorts.Add(inputPort);
            }
        }
        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            Models.Execution.FlowExecutionContext? context,
            CancellationToken cancellationToken = default) {
            var targetType = SelectedType.Value;
            if (targetType == null) throw new InvalidOperationException("Target type is not selected.");

            var itemCount = ItemCount.Value;
            if (itemCount == 0) {
                yield return FlowOut;
                yield break;
            }

            try {
                Type listType = typeof(List<>).MakeGenericType(targetType);
                IList collection = (IList)Activator.CreateInstance(listType);
                System.Diagnostics.Debug.WriteLine($"ObjectCollectionNode: 새 리스트 생성, HashCode: {collection.GetHashCode()}");

                // 각 항목 처리
                foreach (var inputPort in _itemInputPorts) {
                    if (!inputPort.IsConnected) continue;
                    
                    // 리플렉션을 사용하여 GetValueOrDefault 메서드 호출
                    var getValueMethod = inputPort.GetType().GetMethod("GetValueOrDefault", Type.EmptyTypes);
                    if (getValueMethod != null) {
                        var itemValue = getValueMethod.Invoke(inputPort, null);
                        
                        // 값이 있는 경우에만 컬렉션에 추가
                        if (itemValue != null) {
                            collection.Add(itemValue);
                        }
                    }
                }

                // 출력 설정
                if (_outputPort != null) {
                    _outputPort.Value = collection;
                }
                
                System.Diagnostics.Debug.WriteLine($"ObjectCollectionNode: 컬렉션 처리 완료, 항목 수: {collection.Count}, HashCode: {collection.GetHashCode()}");
            }
            catch (Exception ex) {
                Console.WriteLine($"컬렉션 처리 중 오류: {ex.Message}");
                throw;
            }

            yield return FlowOut;
        }

        public override string ToString() {
            var targetType = SelectedType.Value;
            return $"Object Collection Node ({(targetType?.Name ?? "Unknown")})";
        }
    }
}
