using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;
using WPFNode.Models.Execution;
using Microsoft.Extensions.Logging;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("익명 객체 생성")]
    [NodeDescription("동적으로 속성을 가진 익명 객체를 생성합니다.")]
    [NodeCategory("데이터 생성")]
    public class AnonymousObjectNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        [NodeProperty("속성 개수", OnValueChanged = nameof(PropertyCount_Changed))]
        public NodeProperty<int> PropertyCount { get; private set; }

        private IOutputPort _objectOutput;
        private readonly List<INodeProperty> _propertyNameList = new List<INodeProperty>();
        private readonly List<INodeProperty> _propertyValueList = new List<INodeProperty>();

        public AnonymousObjectNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "익명 객체 생성";
            Description = "동적으로 속성을 가진 익명 객체를 생성합니다.";
            
            // 초기값 설정
            PropertyCount.Value = 1;
        }

        private void PropertyCount_Changed()
        {
            if (PropertyCount.Value < 0)
                PropertyCount.Value = 0;
                
            if (PropertyCount.Value > 20)
                PropertyCount.Value = 20; // 최대 20개 속성으로 제한
                
            ReconfigurePorts();
        }

        protected override void Configure(NodeBuilder builder)
        {
            // 프로퍼티 목록 초기화
            _propertyNameList.Clear();
            _propertyValueList.Clear();
            
            // dynamic 타입으로 출력 포트 구성
            _objectOutput = builder.Output("결과", typeof(ExpandoObject));
            
            // 속성 개수에 맞게 이름과 값 프로퍼티 생성
            int propertyCount = PropertyCount?.Value ?? 0;
            
            for (int i = 0; i < propertyCount; i++)
            {
                // 속성 이름 프로퍼티
                var nameProperty = builder.Property($"속성이름_{i}", $"속성이름_{i}", typeof(string), canConnectToPort: true);
                _propertyNameList.Add(nameProperty);
                
                // 속성 값 프로퍼티 (object 타입으로 모든 값 허용)
                var valueProperty = builder.Property($"속성값_{i}", $"속성값_{i}", typeof(object), canConnectToPort: true);
                _propertyValueList.Add(valueProperty);
            }
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ExpandoObject 생성
                dynamic dynamicObject = new ExpandoObject();
                var expandoDict = (IDictionary<string, object>)dynamicObject;
                
                // 모든 속성 추가
                for (int i = 0; i < _propertyNameList.Count; i++)
                {
                    // 속성 이름 가져오기
                    string propName = (string)(_propertyNameList[i]?.Value ?? $"Property{i}");
                    
                    // 이름이 비어있는 경우 기본 이름 사용
                    if (string.IsNullOrWhiteSpace(propName))
                    {
                        propName = $"Property{i}";
                    }
                    
                    // 값 가져오기
                    object propValue = _propertyValueList[i]?.Value;
                    
                    // 속성 추가
                    expandoDict[propName] = propValue;
                    
                    Logger?.LogDebug("익명 객체에 속성 추가: {PropertyName} = {PropertyValue}", propName, propValue);
                }
                
                // 출력 포트에 결과 설정
                _objectOutput.Value = dynamicObject;
                
                Logger?.LogDebug("익명 객체 생성 완료: {PropertyCount}개 속성", expandoDict.Count);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "익명 객체 생성 중 오류 발생");
                throw;
            }
            
            yield return FlowOut;
        }
        
        public override string ToString()
        {
            return $"익명 객체 생성 노드 ({PropertyCount.Value}개 속성)";
        }
    }
} 