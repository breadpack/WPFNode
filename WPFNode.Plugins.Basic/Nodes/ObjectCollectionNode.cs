using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes {
    [NodeName("Object Collection")]
    [NodeDescription("다양한 타입의 객체 리스트를 생성합니다.")]
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

        private readonly List<INodeProperty> _itemProperties = [];
        private          IOutputPort?        _outputPort;

        [JsonConstructor]
        public ObjectCollectionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
            Name        = "Object Collection";
            Description = "다양한 타입의 객체 리스트를 생성합니다.";
        }

        private void SelectedType_PropertyChanged() {
            ReconfigurePorts();
        }

        private void ItemCount_PropertyChanged() {
            ReconfigurePorts();
        }
        
        protected override void Configure(NodeBuilder builder) {
            // 기존 항목 프로퍼티 목록 초기화
            _itemProperties.Clear();
            
            // 입력/출력 포트 구성
            var targetType = SelectedType?.Value ?? typeof(object);
            var itemCount = ItemCount?.Value ?? 0;
            
            // 리스트 타입의 출력 포트 구성
            var listType = typeof(List<>).MakeGenericType(targetType);
            _outputPort = builder.Output("Collection", listType);
            
            if (targetType == null || itemCount <= 0) return;
            
            // 타겟 타입의 쓰기 가능한 속성들 가져오기
            var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                          .Where(p => p.CanWrite)
                                          .ToList();

            if (targetProperties.Count == 0) return;
            
            // 현재 프로퍼티 상태 저장
            var existingProps = Properties
                .Where(p => p.Name.StartsWith("Item_"))
                .ToDictionary(
                    p => p.Name,
                    p => new { 
                        Value = p.Value,
                        CanConnectToPort = p.CanConnectToPort
                    });
            
            // 각 항목에 대해 프로퍼티 구성
            for (int i = 0; i < itemCount; i++) {
                int index = i + 1;
                string itemKey = $"Item{index}_";

                // 항목별 속성 그룹 추가
                foreach (var prop in targetProperties) {
                    string propName = $"{itemKey}{prop.Name}";
                    string displayName = $"항목 {index} - {prop.Name}";

                    INodeProperty nodeProperty;
                    if(existingProps.TryGetValue(propName, out var existingProp)) {
                        // 기존 프로퍼티가 있다면 복원
                        nodeProperty = builder.Property(propName, displayName, prop.PropertyType, canConnectToPort: existingProp.CanConnectToPort);
                        nodeProperty.Value = existingProp.Value;
                    }
                    else {
                        // 새 프로퍼티 생성
                        nodeProperty = builder.Property(propName, displayName, prop.PropertyType, canConnectToPort: true);
                    }

                    // 항목 프로퍼티 리스트에 추가
                    _itemProperties.Add(nodeProperty);
                }
            }
        }

        
        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(CancellationToken cancellationToken = default) {
            var targetType = SelectedType.Value;
            if (targetType == null) throw new InvalidOperationException("Target type is not selected.");

            var itemCount = ItemCount.Value;
            if (itemCount == 0) {
                yield return FlowOut;
            }

            try {
                // 제네릭 리스트 생성
                Type listType   = typeof(List<>).MakeGenericType(targetType);
                var  collection = (IList)Activator.CreateInstance(listType);

                // 타입의 속성 정보 가져오기
                var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                 .Where(p => p.CanWrite)
                                                 .ToList();

                // 각 항목 처리
                for (int i = 0; i < itemCount; i++) {
                    string itemKey = $"Item{i + 1}_";

                    var itemProps = _itemProperties
                                    .Where(p => p.Name.StartsWith(itemKey))
                                    .ToList();

                    // 첫 번째 속성(보통 이름)의 값을 확인하여 유효성 검사
                    if (itemProps.Count == 0)
                        continue;

                    var firstProp  = itemProps.FirstOrDefault();
                    var firstValue = firstProp?.Value;

                    // 첫 번째 속성이 문자열이고 비어있는 경우 이 항목 건너뛰기
                    if (firstValue == null || (firstValue is string str && string.IsNullOrEmpty(str)))
                        continue;

                    // 새 객체 생성
                    var newObject = Activator.CreateInstance(targetType);

                    // 객체 속성 설정
                    foreach (var prop in targetProperties) {
                        string propName  = $"{itemKey}{prop.Name}";
                        var    propValue = itemProps.FirstOrDefault(p => p.Name == propName);
                        if (propValue?.Value == null)
                            continue;

                        try {
                            // 속성 값 설정
                            prop.SetValue(newObject, Convert.ChangeType(propValue.Value, prop.PropertyType));
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Property {prop.Name} 설정 중 오류: {ex.Message}");
                        }
                    }

                    // 컬렉션에 객체 추가
                    collection.Add(newObject);
                }

                // 출력 설정
                if (_outputPort != null) {
                    // 리플렉션을 사용하여 포트의 Value 속성에 값 설정
                    _outputPort.Value = collection;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"컬렉션 처리 중 오류: {ex.Message}");
                throw;
            }

            yield return FlowOut;
        }

        public override void ReadJson(JsonElement element, JsonSerializerOptions options) {
            // 기본 역직렬화 수행
            base.ReadJson(element, options);
            
            // 포트와 프로퍼티 재구성
            ReconfigurePorts();
        }

        public override string ToString() {
            var targetType = SelectedType.Value;
            return $"Object Collection Node ({(targetType?.Name ?? "Unknown")})";
        }
    }
}
