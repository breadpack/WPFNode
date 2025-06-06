using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Interfaces;
using WPFNode.Utilities;
using Microsoft.Extensions.Logging;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("HashSet.Remove")]
    [NodeDescription("해시셋에서 항목을 제거합니다.")]
    [NodeCategory("컬렉션")]
    public class HashSetRemoveNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        [NodeInput("해시셋", ConnectionStateChangedCallback = nameof(HashSetInput_ConnectionChanged))]
        public GenericInputPort HashSetInput { get; private set; }

        private IInputPort _itemInput;
        private IOutputPort _resultOutput;
        private IOutputPort _successOutput;

        public HashSetRemoveNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "HashSet.Remove";
            Description = "해시셋에서 항목을 제거합니다. 해시셋 입력 타입에 따라 항목 타입을 자동으로 결정합니다.";
        }

        private void HashSetInput_ConnectionChanged(IInputPort port)
        {
            Logger?.LogDebug($"HashSetInput 포트 상태 변경됨 (연결 또는 타입).");
            ReconfigurePorts();
        }

        protected override void Configure(NodeBuilder builder)
        {
            Type hashSetType = typeof(object);
            Type elementType = typeof(object);

            if (HashSetInput?.CurrentResolvedType != null && HashSetInput.CurrentResolvedType != typeof(object))
            {
                hashSetType = HashSetInput.CurrentResolvedType;
                // HashSet<T>의 T 타입 추출
                if (hashSetType.IsGenericType && hashSetType.GetGenericTypeDefinition() == typeof(HashSet<>))
                {
                    elementType = hashSetType.GetGenericArguments()[0];
                }
                Logger?.LogDebug($"HashSetInput 타입({hashSetType.Name}) 기반. ItemType: {elementType.Name}, Output HashSetType: {hashSetType.Name} 사용.");
            }
            else
            {
                Logger?.LogDebug("HashSetInput 타입 불명확. ItemType: object, Output HashSetType: object 사용.");
            }

            _itemInput = builder.Input("항목", elementType);
            _resultOutput = builder.Output("결과", hashSetType);
            _successOutput = builder.Output("성공", typeof(bool));
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            var hashSetValue = HashSetInput?.Value;
            bool success = false;

            if (hashSetValue != null)
            {
                var itemValue = _itemInput?.Value;
                
                try
                {
                    // 동적으로 Remove 메서드 호출
                    // HashSet<T>.Remove는 항목이 없으면 false 반환
                    success = (bool)hashSetValue.GetType().GetMethod("Remove").Invoke(hashSetValue, new[] { itemValue });
                    
                    if (success)
                    {
                        Logger?.LogDebug($"항목 '{itemValue}' (Type: {itemValue?.GetType().Name})을(를) 해시셋에서 제거했습니다.");
                    }
                    else
                    {
                        Logger?.LogDebug($"항목 '{itemValue}' (Type: {itemValue?.GetType().Name})이(가) 해시셋에 존재하지 않습니다.");
                    }
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"항목 제거 중 오류 발생: {ex.Message}");
                    success = false;
                }
            }
            else
            {
                Logger?.LogError("HashSetInput 값이 null입니다.");
            }

            _resultOutput.Value = hashSetValue;
            _successOutput.Value = success;

            yield return FlowOut;
        }
    }
} 