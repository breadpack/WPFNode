using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("동적 속성 접근")]
    [NodeDescription("ExpandoObject나 Dictionary 같은 동적 객체에서 이름으로 속성값을 가져옵니다.")]
    [NodeCategory("데이터 변환")]
    public class DynamicPropertyAccessNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        [NodeInput("동적 객체")]
        public GenericInputPort ObjectInput { get; private set; }

        [NodeProperty("속성 이름")]
        public NodeProperty<string> PropertyName { get; private set; }

        [NodeProperty("기본값")]
        public NodeProperty<object> DefaultValue { get; private set; }

        private IOutputPort _valueOutput;
        private IOutputPort _existsOutput;

        public DynamicPropertyAccessNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "동적 속성 접근";
            Description = "ExpandoObject나 Dictionary 같은 동적 객체에서 이름으로 속성값을 가져옵니다.";
        }

        protected override void Configure(NodeBuilder builder)
        {
            // 항상 object 타입으로 값을 반환 (타입은 런타임에 결정됨)
            _valueOutput = builder.Output("값", typeof(object));
            _existsOutput = builder.Output("존재 여부", typeof(bool));
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            if (ObjectInput == null)
            {
                Logger?.LogWarning("객체 입력 포트가 구성되지 않았습니다.");
                _valueOutput.Value = DefaultValue?.Value;
                _existsOutput.Value = false;
                yield return FlowOut;
                yield break;
            }

            object inputObject = ObjectInput.Value;
            if (inputObject == null)
            {
                Logger?.LogWarning("입력 객체가 null입니다.");
                _valueOutput.Value = DefaultValue?.Value;
                _existsOutput.Value = false;
                yield return FlowOut;
                yield break;
            }

            string propName = PropertyName?.Value;
            if (string.IsNullOrWhiteSpace(propName))
            {
                Logger?.LogWarning("속성 이름이 지정되지 않았습니다.");
                _valueOutput.Value = DefaultValue?.Value;
                _existsOutput.Value = false;
                yield return FlowOut;
                yield break;
            }

            try
            {
                object result = DefaultValue?.Value;
                bool exists = false;

                // ExpandoObject 처리
                if (inputObject is ExpandoObject expando)
                {
                    var expandoDict = (IDictionary<string, object>)expando;
                    exists = expandoDict.TryGetValue(propName, out var value);
                    if (exists)
                    {
                        result = value;
                        Logger?.LogDebug("ExpandoObject에서 속성 '{PropertyName}'의 값 {Value}을(를) 가져왔습니다.", 
                            propName, value);
                    }
                    else
                    {
                        Logger?.LogDebug("ExpandoObject에 속성 '{PropertyName}'이(가) 존재하지 않습니다. 기본값 사용.", 
                            propName);
                    }
                }
                // IDictionary 처리
                else if (inputObject is IDictionary dictionary)
                {
                    exists = dictionary.Contains(propName);
                    if (exists)
                    {
                        result = dictionary[propName];
                        Logger?.LogDebug("Dictionary에서 키 '{PropertyName}'의 값 {Value}을(를) 가져왔습니다.", 
                            propName, result);
                    }
                    else
                    {
                        Logger?.LogDebug("Dictionary에 키 '{PropertyName}'이(가) 존재하지 않습니다. 기본값 사용.", 
                            propName);
                    }
                }
                // 일반 객체 처리 (리플렉션)
                else
                {
                    var objectType = inputObject.GetType();
                    var prop = objectType.GetProperty(propName);
                    
                    if (prop != null)
                    {
                        exists = true;
                        result = prop.GetValue(inputObject);
                        Logger?.LogDebug("객체에서 속성 '{PropertyName}'의 값 {Value}을(를) 가져왔습니다.", 
                            propName, result);
                    }
                    else
                    {
                        var field = objectType.GetField(propName);
                        if (field != null)
                        {
                            exists = true;
                            result = field.GetValue(inputObject);
                            Logger?.LogDebug("객체에서 필드 '{PropertyName}'의 값 {Value}을(를) 가져왔습니다.", 
                                propName, result);
                        }
                        else
                        {
                            Logger?.LogDebug("객체에 속성 또는 필드 '{PropertyName}'이(가) 존재하지 않습니다. 기본값 사용.", 
                                propName);
                        }
                    }
                }

                // 결과 설정
                _valueOutput.Value = result;
                _existsOutput.Value = exists;
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "객체에서 속성 '{PropertyName}' 접근 중 오류 발생: {ErrorMessage}", 
                    propName, ex.Message);
                _valueOutput.Value = DefaultValue?.Value;
                _existsOutput.Value = false;
            }

            yield return FlowOut;
        }

        public override string ToString()
        {
            return $"동적 속성 접근 ({PropertyName?.Value ?? "?"})";
        }
    }
} 