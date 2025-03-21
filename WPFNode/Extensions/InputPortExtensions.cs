using System;
using WPFNode.Models;

namespace WPFNode.Extensions
{
    /// <summary>
    /// InputPort에 대한 확장 메서드를 제공합니다.
    /// </summary>
    public static class InputPortExtensions
    {
        /// <summary>
        /// 테스트 목적으로 InputPort에 값을 직접 설정할 수 있게 합니다.
        /// </summary>
        /// <typeparam name="T">입력 포트 데이터 타입</typeparam>
        /// <param name="input">입력 포트</param>
        /// <param name="value">설정할 값</param>
        public static void SetValue<T>(this InputPort<T> input, T value)
        {
            // 테스트 목적으로 mock 출력 포트 생성하여 연결
            var mockOutput = new OutputPort<T>("Mock", input.Node, 0);
            mockOutput.Value = value;
            
            // 기존 연결이 있으면 제거
            input.Disconnect();
            
            // 연결 시도 (에러 처리 없이 간단하게 처리)
            try
            {
                input.Connect(mockOutput);
            }
            catch
            {
                // 연결 실패 시 무시
            }
        }
    }
}
