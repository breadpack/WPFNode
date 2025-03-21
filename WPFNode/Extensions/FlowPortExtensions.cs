using System;
using WPFNode.Interfaces.Flow;
using WPFNode.Models;

namespace WPFNode.Extensions
{
    /// <summary>
    /// 흐름 포트에 대한 확장 메서드를 제공합니다.
    /// </summary>
    public static class FlowPortExtensions
    {
        /// <summary>
        /// 흐름 출력 포트에서 입력 포트로 연결합니다.
        /// </summary>
        /// <param name="source">연결할 출력 포트</param>
        /// <param name="target">연결 대상 입력 포트</param>
        /// <returns>생성된 흐름 연결</returns>
        public static IFlowConnection Connect(this IFlowOutPort source, IFlowInPort target)
        {
            // 연결 가능 여부 검증
            if (source == null || target == null)
                throw new ArgumentNullException("소스 또는 타겟 포트가 null입니다.");
                
            if (source.Node == null || target.Node == null)
                throw new ArgumentException("포트는 노드에 연결되어 있어야 합니다.");
                
            // 노드 캔버스를 통해 연결
            if (source.Node is NodeBase sourceNodeBase && 
                sourceNodeBase.Canvas is NodeCanvas canvas)
            {
                return canvas.ConnectFlow(source, target);
            }
            
            throw new InvalidOperationException("노드가 유효한 캔버스에 연결되어 있지 않습니다.");
        }
        
        /// <summary>
        /// 흐름 포트에서 연결을 해제합니다.
        /// </summary>
        /// <param name="port">연결을 해제할 포트</param>
        /// <param name="connection">해제할 연결</param>
        public static void Disconnect(this IFlowPort port, IFlowConnection connection)
        {
            if (port == null || connection == null)
                throw new ArgumentNullException("포트 또는 연결이 null입니다.");
                
            // 노드 캔버스를 통해 연결 해제
            if (port.Node is NodeBase nodeBase && 
                nodeBase.Canvas is NodeCanvas canvas)
            {
                canvas.DisconnectFlow(connection);
            }
        }
    }
}
