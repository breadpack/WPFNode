using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Interfaces;

namespace WPFNode.Demo.Nodes
{
    [NodeName("Advanced Type Conversion")]
    [NodeDescription("다양한 타입 간의 고급 변환 기능을 보여주는 노드입니다.")]
    [NodeCategory("Demo")]
    public class AdvancedConversionNode : NodeBase
    {
        // 문자열 -> 다양한 타입 변환
        public InputPort<string> StringInput { get; }
        
        // 숫자 타입 간 변환
        public InputPort<double> DoubleInput { get; }
        
        // 특수 타입 변환 (Color)
        public InputPort<string> ColorStringInput { get; }
        
        // 출력 포트들
        public OutputPort<int> IntOutput { get; }
        public OutputPort<DateTime> DateTimeOutput { get; }
        public OutputPort<Guid> GuidOutput { get; }
        public OutputPort<int> NumericConvertedOutput { get; }
        public OutputPort<Color> ColorOutput { get; }
        
        public AdvancedConversionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            // 입력 포트 생성
            StringInput = CreateInputPort<string>("문자열 입력");
            DoubleInput = CreateInputPort<double>("실수 입력");
            ColorStringInput = CreateInputPort<string>("색상 문자열");
            
            // 출력 포트 생성
            IntOutput = CreateOutputPort<int>("정수 출력");
            DateTimeOutput = CreateOutputPort<DateTime>("날짜 출력");
            GuidOutput = CreateOutputPort<Guid>("GUID 출력");
            NumericConvertedOutput = CreateOutputPort<int>("숫자 변환 출력");
            ColorOutput = CreateOutputPort<Color>("색상 출력");
            
            // 기본값 설정을 위한 프로퍼티
            CreateProperty<string>("DefaultString", "기본 문자열", null, true);
            var defaultString = Properties["DefaultString"];
            defaultString.Value = "123";
            
            CreateProperty<string>("DefaultColorString", "기본 색상 문자열", null, true);
            var defaultColorString = Properties["DefaultColorString"];
            defaultColorString.Value = "Red";
        }
        
        protected override Task ProcessAsync(CancellationToken cancellationToken)
        {
            // 문자열 -> 다양한 타입으로 변환
            var strValue = StringInput.GetValueOrDefault("");
            
            // 문자열 -> Int 변환
            if (!string.IsNullOrEmpty(strValue) && int.TryParse(strValue, out var intResult))
            {
                IntOutput.Value = intResult;
            }
            else
            {
                IntOutput.Value = 0;
            }
            
            // 문자열 -> DateTime 변환
            if (!string.IsNullOrEmpty(strValue) && DateTime.TryParse(strValue, out var dateResult))
            {
                DateTimeOutput.Value = dateResult;
            }
            else
            {
                DateTimeOutput.Value = DateTime.Now;
            }
            
            // 문자열 -> Guid 변환
            if (!string.IsNullOrEmpty(strValue) && Guid.TryParse(strValue, out var guidResult))
            {
                GuidOutput.Value = guidResult;
            }
            else
            {
                GuidOutput.Value = Guid.Empty;
            }
            
            // Double -> Int 변환 (숫자 타입 간 변환)
            var doubleValue = DoubleInput.GetValueOrDefault(0.0);
            NumericConvertedOutput.Value = (int)doubleValue;
            
            // 문자열 -> Color 변환 (TypeConverter 사용)
            var colorString = ColorStringInput.GetValueOrDefault("");
            try
            {
                if (!string.IsNullOrEmpty(colorString))
                {
                    // 색상 이름으로 변환 시도
                    var colorConverter = new System.ComponentModel.TypeConverter();
                    var color = Color.FromName(colorString);
                    
                    // 유효한 색상이면 적용
                    if (color.A > 0 || color.R > 0 || color.G > 0 || color.B > 0)
                    {
                        ColorOutput.Value = color;
                    }
                    else
                    {
                        // HTML 색상 코드(#RRGGBB) 처리
                        if (colorString.StartsWith("#") && (colorString.Length == 7 || colorString.Length == 9))
                        {
                            try
                            {
                                int r = Convert.ToInt32(colorString.Substring(1, 2), 16);
                                int g = Convert.ToInt32(colorString.Substring(3, 2), 16);
                                int b = Convert.ToInt32(colorString.Substring(5, 2), 16);
                                
                                ColorOutput.Value = Color.FromArgb(255, r, g, b);
                            }
                            catch
                            {
                                ColorOutput.Value = Color.Black;
                            }
                        }
                        else
                        {
                            ColorOutput.Value = Color.Black;
                        }
                    }
                }
                else
                {
                    ColorOutput.Value = Color.Black;
                }
            }
            catch
            {
                ColorOutput.Value = Color.Black;
            }
            
            return Task.CompletedTask;
        }
    }
}
