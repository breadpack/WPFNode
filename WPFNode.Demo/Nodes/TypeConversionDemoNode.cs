using System;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Interfaces;

namespace WPFNode.Demo.Nodes
{
    [NodeName("Type Conversion Demo")]
    [NodeDescription("데모용 노드로 향상된 타입 변환 기능을 보여줍니다.")]
    [NodeCategory("Demo")]
    public class TypeConversionDemoNode : NodeBase
    {
        public InputPort<int> IntPort { get; }
        public InputPort<double> DoublePort { get; }
        public InputPort<DateTime> DateTimePort { get; }
        public InputPort<Guid> GuidPort { get; }
        public InputPort<string> StringPort { get; }
        
        public OutputPort<string> ResultPort { get; }
        
        public TypeConversionDemoNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            // 입력 포트 생성
            IntPort = CreateInputPort<int>("Int");
            DoublePort = CreateInputPort<double>("Double");
            DateTimePort = CreateInputPort<DateTime>("DateTime");
            GuidPort = CreateInputPort<Guid>("Guid");
            StringPort = CreateInputPort<string>("String");
            
            // 출력 포트 생성
            ResultPort = CreateOutputPort<string>("Result");
            
            // 프로퍼티 생성
            CreateProperty<int>("IntValue", "정수 값", null, true);
            CreateProperty<double>("DoubleValue", "실수 값", null, true);
            CreateProperty<string>("StringValue", "문자열 값", null, true);
        }
        
        protected override Task ProcessAsync(CancellationToken cancellationToken)
        {
            // 각 포트의 값을 읽어서 문자열로 변환
            var intValue = IntPort.GetValueOrDefault(0);
            var doubleValue = DoublePort.GetValueOrDefault(0.0);
            var dateTimeValue = DateTimePort.GetValueOrDefault(DateTime.Now);
            var guidValue = GuidPort.GetValueOrDefault(Guid.Empty);
            var stringValue = StringPort.GetValueOrDefault("(없음)");
            
            // 결과 문자열 생성
            var result = $"변환 결과:\n" +
                         $"Int: {intValue}\n" +
                         $"Double: {doubleValue}\n" +
                         $"DateTime: {dateTimeValue:yyyy-MM-dd HH:mm:ss}\n" +
                         $"Guid: {guidValue}\n" +
                         $"String: {stringValue}";
            
            // 결과 포트에 값 설정
            ResultPort.Value = result;
            
            return Task.CompletedTask;
        }
    }
}
