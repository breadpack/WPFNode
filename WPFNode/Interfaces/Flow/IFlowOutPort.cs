using System.Threading;
using System.Threading.Tasks;

namespace WPFNode.Interfaces.Flow;

/// <summary>
/// 흐름 출력 포트를 나타내는 인터페이스입니다.
/// </summary>
public interface IFlowOutPort : IFlowPort
{
    /// <summary>
    /// 이 포트에서 연결된 모든 입력 포트로 흐름을 전파합니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task PropagateFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken);
}
