using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Nodes {
    [NodeName("객체 생성")]
    [NodeDescription("지정된 타입의 객체를 생성합니다.")]
    [NodeCategory("데이터 변환")]
    public class CreateObjectNode : DynamicNode {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; set; }
        
        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; set; }
        
        [NodeProperty("Target Type", OnValueChanged = nameof(SelectedType_PropertyChanged))]
        NodeProperty<Type> SelectedType { get; set; }

        [NodeProperty("Connected Only")]
        NodeProperty<bool> ConnectedOnly { get; set; }

        private IOutputPort? _outputPort;
        private readonly List<INodeProperty> _propertyList = [];
        
        [JsonConstructor]
        public CreateObjectNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
            Name = "객체 생성";
            Description = "지정된 타입의 객체를 생성합니다.";
        }

        private void SelectedType_PropertyChanged() {
            ReconfigurePorts();
        }
        
    protected override void Configure(NodeBuilder builder) {
        // 프로퍼티 목록 초기화
        _propertyList.Clear();
        
        // 타겟 타입 확인 및 출력 포트 구성
        var targetType = SelectedType?.Value ?? typeof(object);
        _outputPort = builder.Output("Object", targetType);
        
        if (targetType == null || targetType == typeof(object)) return;
        
        // 타겟 타입의 쓰기 가능한 속성과 필드들 가져오기
        var targetProperties = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => p.CanWrite)
            .ToList();
            
        var targetFields = targetType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(f => !f.IsInitOnly)
            .ToList();
        
        if (targetProperties.Count == 0 && targetFields.Count == 0) return;
        
        // 각 속성에 대한 NodeProperty 구성
        foreach (var prop in targetProperties) {
            string propName = prop.Name;
            
            // builder.Property가 내부적으로 기존 프로퍼티를 찾아 반환하므로 
            // 별도의 existingProps Dictionary를 만들 필요가 없음
            var nodeProperty = builder.Property(propName, propName, prop.PropertyType, canConnectToPort: true);
            
            _propertyList.Add(nodeProperty);
        }
        
        // 각 필드에 대한 NodeProperty 구성
        foreach (var field in targetFields) {
            string fieldName = field.Name;
            
            // builder.Property가 내부적으로 기존 프로퍼티를 찾아 반환하므로
            // 별도의 existingProps Dictionary를 만들 필요가 없음
            var nodeProperty = builder.Property(fieldName, fieldName, field.FieldType, canConnectToPort: true);
            
            _propertyList.Add(nodeProperty);
        }
    }

        protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken) {
            var targetType = SelectedType.Value;
            if (targetType == null) 
                throw new InvalidOperationException("Target type is not selected.");
            
            try {
                // 객체 생성
                var newObject = Activator.CreateInstance(targetType);
                
                // 속성 설정
                foreach (var prop in _propertyList) {
                    var targetProp = targetType.GetProperty(prop.Name);
                    var targetField = targetType.GetField(prop.Name);
                    
                    if (targetProp != null && targetProp.CanWrite) {
                        // Property 처리
                        bool isConnected = prop is IInputPort inputPort && inputPort.IsConnected;
                        if (!isConnected && ConnectedOnly.Value) continue;
                        
                        if (prop.Value != null) {
                            try {
                                targetProp.SetValue(newObject, Convert.ChangeType(prop.Value, targetProp.PropertyType));
                            }
                            catch (Exception ex) {
                                Console.WriteLine($"Property {prop.Name} 설정 중 오류: {ex.Message}");
                            }
                        }
                    }
                    else if (targetField != null && !targetField.IsInitOnly) {
                        // Field 처리
                        bool isConnected = prop is IInputPort inputPort && inputPort.IsConnected;
                        if (!isConnected && ConnectedOnly.Value) continue;
                        
                        if (prop.Value != null) {
                            try {
                                targetField.SetValue(newObject, Convert.ChangeType(prop.Value, targetField.FieldType));
                            }
                            catch (Exception ex) {
                                Console.WriteLine($"Field {prop.Name} 설정 중 오류: {ex.Message}");
                            }
                        }
                    }
                }
                
                // 출력 설정
                if (_outputPort != null) {
                    _outputPort.Value = newObject;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"객체 생성 중 오류: {ex.Message}");
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
            return $"객체 생성 노드 ({(targetType?.Name ?? "Unknown")})";
        }
    }
}
