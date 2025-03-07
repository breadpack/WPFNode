using System;
using System.Linq;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("Guid 생성기")]
    [NodeDescription("랜덤한 Guid 값을 생성합니다.")]
    [NodeCategory("값 편집")]
    public class GuidGeneratorNode : NodeBase
    {
        private INodeProperty? _guidProperty;
        private INodeProperty? _autoGenerateProperty;
        
        public GuidGeneratorNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "Guid 생성기";
            Description = "랜덤한 Guid 값을 생성합니다.";
            
            Initialize();
        }
        
        private void Initialize()
        {
            // 생성된 Guid 값을 저장할 프로퍼티 추가
            _guidProperty = CreateProperty<Guid>("Guid", "Guid 값");
            
            // 실행 시 자동으로 새 Guid를 생성할지 여부 설정
            _autoGenerateProperty = CreateProperty<bool>("AutoGenerate", "자동 생성");
            _autoGenerateProperty.Value = true;
            
            // 출력 포트 추가
            CreateOutputPort("Result", typeof(Guid));
            
            // 명시적으로 Guid를 생성하는 입력 포트 추가
            CreateInputPort("Generate", typeof(bool));
            
            // 초기값 생성
            GenerateNewGuid();
        }
        
        private void GenerateNewGuid()
        {
            if (_guidProperty != null)
            {
                _guidProperty.Value = Guid.NewGuid();
            }
        }
        
        protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            // Generate 포트가 트리거되었는지 확인
            var generatePort = InputPorts.FirstOrDefault(p => p.Name == "Generate") as InputPort<bool>;
            bool shouldGenerate = false;
            
            if (generatePort != null && generatePort.GetValueOrDefault(false))
            {
                shouldGenerate = true;
            }
            
            // 자동 생성이 활성화되었는지 확인
            bool autoGenerate = _autoGenerateProperty?.Value is bool autoGenerateValue && autoGenerateValue;
            
            // Generate 포트가 트리거되었거나 자동 생성이 활성화된 경우 새 Guid 생성
            if (shouldGenerate || autoGenerate)
            {
                GenerateNewGuid();
            }
            
            // 출력 포트에 Guid 값 설정
            var outputPort = OutputPorts.FirstOrDefault(p => p.Name == "Result") as OutputPort<Guid>;
            if (outputPort != null && _guidProperty != null)
            {
                outputPort.Value = (Guid)_guidProperty.Value;
            }
            
            await Task.CompletedTask;
        }
    }
}
