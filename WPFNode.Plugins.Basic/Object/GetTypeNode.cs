using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WPFNode.Models.Execution;

namespace WPFNode.Plugins.Basic.Object;

[NodeName("Object.GetType")]
[NodeCategory("객체")]
[NodeDescription("객체의 타입 정보를 반환합니다.")]
public class GetTypeNode : NodeBase
{
    [NodeInput("객체")]
    public InputPort<object> Input { get; set; }
    
    [NodeOutput("타입")]
    public OutputPort<Type> TypeResult { get; set; }
    
    [NodeOutput("타입 이름")]
    public OutputPort<string> TypeName { get; set; }
    
    [NodeOutput("네임스페이스")]
    public OutputPort<string> Namespace { get; set; }
    
    [NodeOutput("어셈블리 이름")]
    public OutputPort<string> AssemblyName { get; set; }

    [NodeFlowIn("실행")]
    public FlowInPort FlowIn { get; set; }

    [NodeFlowOut("출력")]
    public FlowOutPort FlowOut { get; set; }
    
    [NodeProperty("전체 이름 사용", CanConnectToPort = false)]
    public NodeProperty<bool> UseFullName { get; set; }

    public GetTypeNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) {
        UseFullName.Value = false;
    }

    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        FlowExecutionContext? context,
        CancellationToken     cancellationToken
    )
    {
        // 입력 객체 가져오기
        object input = Input?.GetValueOrDefault(null);
        
        // 기본값
        Type type = null;
        string typeName = string.Empty;
        string namespaceName = string.Empty;
        string assemblyName = string.Empty;
        
        // 객체가 null이 아닌 경우에만 GetType 호출
        if (input != null)
        {
            type = input.GetType();
            
            // 타입 이름 가져오기
            typeName = UseFullName.Value ? type.FullName : type.Name;
            
            // 네임스페이스 가져오기
            namespaceName = type.Namespace ?? string.Empty;
            
            // 어셈블리 이름 가져오기
            assemblyName = type.Assembly?.GetName()?.Name ?? string.Empty;
        }
        
        // 결과 설정
        if (TypeResult != null)
            TypeResult.Value = type;
            
        if (TypeName != null)
            TypeName.Value = typeName;
            
        if (Namespace != null)
            Namespace.Value = namespaceName;
            
        if (AssemblyName != null)
            AssemblyName.Value = assemblyName;
        
        // 필요한 비동기 작업을 처리하기 위한 대기
        await Task.CompletedTask;
        
        // FlowOut이 있으면 반환
        if (FlowOut != null)
        {
            yield return FlowOut;
        }
    }
}
