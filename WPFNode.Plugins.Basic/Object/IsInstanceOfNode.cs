using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Object.IsInstanceOf")]
[NodeCategory("객체")]
[NodeDescription("객체가 지정된 타입의 인스턴스인지 확인합니다.")]
public class IsInstanceOfNode : NodeBase
{
    [NodeInput("객체")]
    public InputPort<object> Input { get; set; }
    
    [NodeInput("타입")]
    public InputPort<Type> TypeInput { get; set; }
    
    [NodeOutput("인스턴스 여부")]
    public OutputPort<bool> Result { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("타입 이름", CanConnectToPort = true)]
    public NodeProperty<string> TypeName { get; set; }
    
    [NodeProperty("타입 이름 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseTypeName { get; set; }

    public IsInstanceOfNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        UseTypeName.Value = false;
        TypeName.Value = "System.String";
    }

    protected override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 객체 가져오기
        object input = Input?.GetValueOrDefault(null);
        
        // 기본값
        bool result = false;
        
        // 객체가 null이 아닌 경우에만 타입 확인
        if (input != null)
        {
            Type checkType;
            
            if (UseTypeName.Value)
            {
                // 타입 이름으로 타입 가져오기
                try
                {
                    checkType = Type.GetType(TypeName.Value, false);
                    
                    // 타입을 찾을 수 없는 경우 현재 어셈블리에서 이름으로 검색
                    if (checkType == null)
                    {
                        checkType = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(a => a.GetTypes())
                            .FirstOrDefault(t => t.Name == TypeName.Value || t.FullName == TypeName.Value);
                    }
                }
                catch
                {
                    checkType = null;
                }
            }
            else
            {
                // 타입 입력 포트 값 사용
                checkType = TypeInput?.GetValueOrDefault(null);
            }
            
            // 타입이 유효하면 IsInstanceOfType 호출
            if (checkType != null)
            {
                result = checkType.IsInstanceOfType(input);
            }
        }
        
        // 결과 설정
        if (Result != null)
            Result.Value = result;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
