using System;
using System.Collections.Generic;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Interfaces;
using Microsoft.Extensions.Logging;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("HashSet.Clear")]
    [NodeDescription("해시셋의 모든 항목을 제거합니다.")]
    [NodeCategory("컬렉션")]
    public class HashSetClearNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        [NodeInput("해시셋", ConnectionStateChangedCallback = nameof(HashSetInput_ConnectionChanged))]
        public GenericInputPort HashSetInput { get; private set; }

        private IOutputPort _resultOutput;

        public HashSetClearNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "HashSet.Clear";
            Description = "해시셋의 모든 항목을 제거합니다. 해시셋 입력 타입에 따라 결과 타입을 자동으로 결정합니다.";
        }

        private void HashSetInput_ConnectionChanged(IInputPort port)
        {
            Logger?.LogDebug($"HashSetInput 포트 상태 변경됨 (연결 또는 타입).");
            ReconfigurePorts();
        }

        protected override void Configure(NodeBuilder builder)
        {
            Type hashSetType = typeof(object);

            if (HashSetInput?.CurrentResolvedType != null && HashSetInput.CurrentResolvedType != typeof(object))
            {
                hashSetType = HashSetInput.CurrentResolvedType;
                Logger?.LogDebug($"HashSetInput 타입({hashSetType.Name}) 기반. Output HashSetType: {hashSetType.Name} 사용.");
            }
            else
            {
                Logger?.LogDebug("HashSetInput 타입 불명확. Output HashSetType: object 사용.");
            }

            _resultOutput = builder.Output("결과", hashSetType);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            var hashSetValue = HashSetInput?.Value;

            if (hashSetValue != null)
            {
                try
                {
                    // 동적으로 Clear 메서드 호출
                    hashSetValue.GetType().GetMethod("Clear").Invoke(hashSetValue, null);
                    Logger?.LogDebug($"해시셋의 모든 항목을 제거했습니다.");
                }
                catch (Exception ex)
                {
                    Logger?.LogError(ex, $"해시셋 Clear 중 오류 발생: {ex.Message}");
                }
            }
            else
            {
                Logger?.LogError("HashSetInput 값이 null입니다.");
            }

            _resultOutput.Value = hashSetValue;

            yield return FlowOut;
        }
    }
} 