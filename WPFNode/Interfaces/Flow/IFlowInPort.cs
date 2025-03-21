using System.Threading;
using System.Threading.Tasks;

namespace WPFNode.Interfaces.Flow;

/// <summary>
/// 흐름 입력 포트를 나타내는 인터페이스입니다.
/// </summary>
public interface IFlowInPort : IFlowPort
{
    /// <summary>
    /// 이 포트로 흐름이 도착했을 때 호출되는 메서드입니다.
    /// </summary>
    /// <param name="context">실행 컨텍스트</param>
    /// <param name="cancellationToken">취소 토큰</param>
    Task ReceiveFlowAsync(WPFNode.Models.Execution.ExecutionContext context, CancellationToken cancellationToken);
}
