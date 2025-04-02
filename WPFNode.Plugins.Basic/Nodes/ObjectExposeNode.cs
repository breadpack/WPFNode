using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("객체 속성 노출")]
    [NodeDescription("입력 객체의 모든 속성과 필드를 출력 포트로 노출합니다.")]
    [NodeCategory("데이터 변환")]
    public class ObjectExposeNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        [NodeInput("객체 입력", ConnectionStateChangedCallback = nameof(ObjectInput_ConnectionChanged))]
        public GenericInputPort ObjectInput { get; private set; }

        private readonly Dictionary<string, IOutputPort> _outputPorts = new Dictionary<string, IOutputPort>();
        private Type _lastProcessedType;

        public ObjectExposeNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "객체 속성 노출";
            Description = "입력 객체의 모든 속성과 필드를 출력 포트로 노출합니다.";
        }

        // ObjectInput 포트의 연결 상태 변경 콜백
        public void ObjectInput_ConnectionChanged(IInputPort port)
        {
            if (port is GenericInputPort genericPort)
            {
                if (genericPort.ConnectedType != null)
                {
                    Logger?.LogDebug("ObjectInput 연결 변경: 연결된 타입 {ConnectedType}을(를) 감지했습니다.",
                        genericPort.ConnectedType.Name);
                }
                else
                {
                    Logger?.LogDebug("ObjectInput 연결 해제: 출력 포트를 재구성합니다.");
                }
            }
            
            ReconfigurePorts();
        }

        protected override void Configure(NodeBuilder builder)
        {
            _outputPorts.Clear();

            // 객체 타입 확인
            Type objectType = ObjectInput?.ConnectedType;
            
            if (objectType == null)
            {
                // 타입이 없으면 종료
                Logger?.LogDebug("Configure: 객체 타입이 설정되지 않았습니다.");
                return;
            }

            // 일반 타입 처리
            try
            {
                // 속성 처리
                var properties = objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    string portName = prop.Name;
                    Type propType = prop.PropertyType;

                    var outputPort = builder.Output(portName, propType);
                    _outputPorts[portName] = outputPort;
                }

                // 필드 처리
                var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var field in fields)
                {
                    string portName = field.Name;
                    Type fieldType = field.FieldType;

                    var outputPort = builder.Output(portName, fieldType);
                    _outputPorts[portName] = outputPort;
                }

                Logger?.LogDebug("{Type} 타입에 대한 출력 포트 {PortCount}개를 구성했습니다.", 
                    objectType.Name, _outputPorts.Count);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "출력 포트 구성 중 오류 발생: {ErrorMessage}", ex.Message);
            }
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            if (ObjectInput == null)
            {
                Logger?.LogWarning("객체 입력 포트가 구성되지 않았습니다.");
                yield return FlowOut;
                yield break;
            }

            object inputObject = ObjectInput.Value;
            if (inputObject == null)
            {
                Logger?.LogWarning("입력 객체가 null입니다.");
                yield return FlowOut;
                yield break;
            }

            try
            {
                Type objectType = inputObject.GetType();
                Logger?.LogDebug("입력 객체 타입: {ObjectType}", objectType.Name);

                // 런타임에 들어온 객체 타입이 다르면(예: 사용자가 다른 타입을 연결) 포트 재구성
                if (_lastProcessedType != objectType)
                {
                    Logger?.LogDebug("이전과 다른 타입 감지: {LastType} -> {CurrentType}. 출력 포트를 재구성합니다.",
                        _lastProcessedType?.Name ?? "없음", objectType.Name);
                    _lastProcessedType = objectType;
                    ReconfigurePorts();
                }

                // 일반 객체 속성 처리
                foreach (var property in objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (_outputPorts.TryGetValue(property.Name, out var outputPort))
                    {
                        try
                        {
                            outputPort.Value = property.GetValue(inputObject);
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, "속성 {PropertyName} 값을 가져오는 중 오류 발생: {ErrorMessage}", 
                                property.Name, ex.Message);
                        }
                    }
                }

                // 일반 객체 필드 처리
                foreach (var field in objectType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (_outputPorts.TryGetValue(field.Name, out var outputPort))
                    {
                        try
                        {
                            outputPort.Value = field.GetValue(inputObject);
                        }
                        catch (Exception ex)
                        {
                            Logger?.LogError(ex, "필드 {FieldName} 값을 가져오는 중 오류 발생: {ErrorMessage}", 
                                field.Name, ex.Message);
                        }
                    }
                }

                Logger?.LogDebug("{Type} 타입 객체의 속성과 필드를 출력 포트에 설정했습니다.", 
                    objectType.Name);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "객체 속성 처리 중 오류 발생: {ErrorMessage}", ex.Message);
            }

            yield return FlowOut;
        }

        public override string ToString()
        {
            var type = ObjectInput?.ConnectedType;
            return $"객체 속성 노출 ({(type?.Name ?? "Unknown")})";
        }
    }
} 