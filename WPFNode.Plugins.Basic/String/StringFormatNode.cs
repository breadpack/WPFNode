using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Diagnostics;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.String;

[NodeName("String.Format")]
[NodeCategory("문자열")]
[NodeDescription("형식 문자열에 값을 삽입하여 새 문자열을 생성합니다.")]
public class StringFormatNode : DynamicNode {
    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; private set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; private set; }

    [NodeOutput("결과")]
    public OutputPort<string> Result { get; private set; }

    [NodeProperty("형식 문자열", CanConnectToPort = true, OnValueChanged = nameof(OnFormatStringChanged))]
    public NodeProperty<string> FormatString { get; private set; }

    private          int                         _lastParameterCount = 0;
    private readonly Dictionary<int, IInputPort> _parameterPorts     = new Dictionary<int, IInputPort>();

    public StringFormatNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        InitializeNode();
    }

    private void OnFormatStringChanged() {
        // 노드의 동적 포트를 재구성
        ReconfigurePorts();
    }

    /// <summary>
    /// 형식 문자열에서 매개변수 개수를 추출합니다.
    /// </summary>
    private int GetFormatParameterCount(string format) {
        if (string.IsNullOrEmpty(format))
            return 0;

        try {
            // 중괄호 안의 숫자로 된 형식 지정자 패턴 찾기 ({0}, {1} 등)
            var matches = Regex.Matches(format, @"\{(\d+)(?::[^}]*)?\}");

            if (matches.Count == 0)
                return 0;

            // 가장 큰 인덱스 + 1 = 파라미터 개수
            int maxIndex = -1;
            foreach (Match match in matches) {
                if (match.Groups.Count >= 2 && int.TryParse(match.Groups[1].Value, out int index)) {
                    maxIndex = Math.Max(maxIndex, index);
                }
            }

            return maxIndex + 1;
        }
        catch {
            return 0; // 형식 파싱 실패 시 기본값
        }
    }

    protected override void Configure(NodeBuilder builder) {
        // 형식 문자열에서 필요한 매개변수 포트 개수 확인
        var format         = FormatString?.Value ?? "";
        var parameterCount = GetFormatParameterCount(format);

        // 이전 상태와 비교
        if (parameterCount == _lastParameterCount && _parameterPorts.Count == parameterCount)
            return;

        _lastParameterCount = parameterCount;
        _parameterPorts.Clear();

        // 매개변수 포트 추가
        for (int i = 0; i < parameterCount; i++) {
            var port = builder.Input<object>($"매개변수 {i}");
            _parameterPorts[i] = port;
        }
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    ) {
        try {
            // 형식 문자열 가져오기
            string format = FormatString?.Value ?? "";

            // 매개변수 배열 준비
            var      parameterCount = _parameterPorts.Count;
            object[] parameters     = new object[parameterCount];

            for (int i = 0; i < parameterCount; i++) {
                if (_parameterPorts.TryGetValue(i, out var port)) {
                    try {
                        // 동적으로 타입 처리
                        dynamic dynamicPort = port;
                        parameters[i] = dynamicPort.GetValueOrDefault() ?? "";
                    }
                    catch {
                        parameters[i] = "";
                        Logger?.LogWarning($"파라미터 {i} 값을 가져오는 중 오류 발생");
                    }
                }
                else {
                    parameters[i] = "";
                }
            }

            // string.Format 호출 시도
            string result;
            try {
                result = string.Format(format, parameters);
            }
            catch (FormatException) {
                // 형식 지정자 오류 시 원본 문자열 반환
                result = format;
                Logger?.LogWarning($"잘못된 형식 문자열: {format}");
            }
            catch (Exception ex) {
                result = string.Empty;
                Logger?.LogError(ex, "String.Format 실행 중 오류 발생");
            }

            // 결과 설정
            Result.Value = result;

            // 비동기 처리를 위한 대기
            await Task.CompletedTask;
        }
        catch (Exception ex) {
            Logger?.LogError(ex, "StringFormatNode 실행 중 예외 발생");

            // 오류 발생 시 빈 문자열로 설정
            Result.Value = string.Empty;
        }

        // 플로우 출력
        yield return FlowOut;
    }
}