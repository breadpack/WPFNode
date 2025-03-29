using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using WPFNode.Tests.Models;

namespace WPFNode.Tests.Helpers
{
    /// <summary>
    /// StringConstructorType을 입력으로 받아 속성을 추출하는 노드
    /// </summary>
    [NodeCategory("TypeConversion Tests")]
    [NodeDescription("StringConstructorType 정보를 출력하는 테스트 노드")]
    public class StringConstructorInfoNode : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Input")]
        public InputPort<StringConstructorType> TypeInput { get; private set; } = null!;
        
        [NodeOutput("Name")]
        public OutputPort<string> Name { get; private set; } = null!;
        
        [NodeOutput("Value")]
        public OutputPort<string> Value { get; private set; } = null!;
        
        [NodeOutput("Category")]
        public OutputPort<string> Category { get; private set; } = null!;
        
        // 수신된 데이터 (테스트 검증용)
        public bool WasReceived { get; private set; }
        public StringConstructorType? ReceivedType { get; private set; }

        public StringConstructorInfoNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            base.Name = "StringConstructorInfo";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // InputPort에서 값 가져오기
            var input = TypeInput.GetValueOrDefault();
            
            if (input != null)
            {
                WasReceived = true;
                ReceivedType = input;
                
                // 속성 출력
                Name.Value = input.Name;
                Value.Value = input.Value;
                Category.Value = input.Category;
                
                Debug.WriteLine($"StringConstructorInfoNode: 입력 받음 - {input}");
            }
            else
            {
                Debug.WriteLine("StringConstructorInfoNode: 입력 없음");
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
    
    /// <summary>
    /// ImplicitConversionType을 입력으로 받아 속성을 추출하는 노드
    /// </summary>
    [NodeCategory("TypeConversion Tests")]
    [NodeDescription("ImplicitConversionType 정보를 출력하는 테스트 노드")]
    public class ImplicitConversionInfoNode : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Input")]
        public InputPort<ImplicitConversionType> TypeInput { get; private set; } = null!;
        
        [NodeOutput("Source")]
        public OutputPort<string> Source { get; private set; } = null!;
        
        [NodeOutput("Value")]
        public OutputPort<int> Value { get; private set; } = null!;
        
        // 수신된 데이터 (테스트 검증용)
        public bool WasReceived { get; private set; }
        public ImplicitConversionType? ReceivedType { get; private set; }

        public ImplicitConversionInfoNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            base.Name = "ImplicitConversionInfo";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // InputPort에서 값 가져오기
            var input = TypeInput.GetValueOrDefault();
            
            if (input != null)
            {
                WasReceived = true;
                ReceivedType = input;
                
                // 속성 출력
                Source.Value = input.Source;
                Value.Value = input.Value;
                
                Debug.WriteLine($"ImplicitConversionInfoNode: 입력 받음 - {input}");
            }
            else
            {
                Debug.WriteLine("ImplicitConversionInfoNode: 입력 없음");
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
    
    /// <summary>
    /// ExplicitConversionType을 입력으로 받아 명시적으로 문자열로 변환하는 노드
    /// </summary>
    [NodeCategory("TypeConversion Tests")]
    [NodeDescription("ExplicitConversionType을 문자열로 변환하는 테스트 노드")]
    public class ExplicitConversionToStringNode : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Input")]
        public InputPort<ExplicitConversionType> TypeInput { get; private set; } = null!;
        
        [NodeOutput("StringResult")]
        public OutputPort<string> StringResult { get; private set; } = null!;
        
        [NodeOutput("IntResult")]
        public OutputPort<int> IntResult { get; private set; } = null!;
        
        // 수신된 데이터 (테스트 검증용)
        public bool WasReceived { get; private set; }
        public ExplicitConversionType? ReceivedType { get; private set; }
        public string? ConvertedString { get; private set; }
        public int ConvertedInt { get; private set; }

        public ExplicitConversionToStringNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            base.Name = "ExplicitConversionToString";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // InputPort에서 값 가져오기
            var input = TypeInput.GetValueOrDefault();
            
            if (input != null)
            {
                WasReceived = true;
                ReceivedType = input;
                
                // 명시적 변환 수행
                ConvertedString = (string)input;
                ConvertedInt = (int)input;
                
                // 결과 출력
                StringResult.Value = ConvertedString;
                IntResult.Value = ConvertedInt;
                
                Debug.WriteLine($"ExplicitConversionToStringNode: 입력 받음 - {input}");
                Debug.WriteLine($"ExplicitConversionToStringNode: 변환 결과 - 문자열: {ConvertedString}, 정수: {ConvertedInt}");
            }
            else
            {
                Debug.WriteLine("ExplicitConversionToStringNode: 입력 없음");
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
    
    /// <summary>
    /// 문자열을 입력으로 받아 출력하는 간단한 노드
    /// </summary>
    [NodeCategory("TypeConversion Tests")]
    [NodeDescription("문자열 입력을 받아 출력하는 테스트 노드")]
    public class StringTestNode : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Input")]
        public InputPort<string> Input { get; private set; } = null!;
        
        [NodeOutput("Output")]
        public OutputPort<string> Output { get; private set; } = null!;
        
        // 수신된 데이터 (테스트 검증용)
        public bool WasReceived { get; private set; }
        public string? ReceivedString { get; private set; }

        public StringTestNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            base.Name = "StringTest";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // InputPort에서 값 가져오기
            var input = Input.GetValueOrDefault();
            
            if (input != null)
            {
                WasReceived = true;
                ReceivedString = input;
                
                // 출력
                Output.Value = input;
                
                Debug.WriteLine($"StringTestNode: 입력 받음 - {input}");
            }
            else
            {
                Debug.WriteLine("StringTestNode: 입력 없음");
            }
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
    
    /// <summary>
    /// 정수를 입력으로 받아 출력하는 간단한 노드
    /// </summary>
    [NodeCategory("TypeConversion Tests")]
    [NodeDescription("정수 입력을 받아 출력하는 테스트 노드")]
    public class IntTestNode : NodeBase
    {
        [NodeFlowIn("Execute")]
        public FlowInPort FlowIn { get; private set; } = null!;
        
        [NodeFlowOut("Complete")]
        public FlowOutPort FlowOut { get; private set; } = null!;
        
        [NodeInput("Input")]
        public InputPort<int> Input { get; private set; } = null!;
        
        [NodeOutput("Output")]
        public OutputPort<int> Output { get; private set; } = null!;
        
        // 수신된 데이터 (테스트 검증용)
        public bool WasReceived { get; private set; }
        public int ReceivedInt { get; private set; }

        public IntTestNode(INodeCanvas canvas, Guid id) 
            : base(canvas, id)
        {
            base.Name = "IntTest";
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(FlowExecutionContext? context, CancellationToken cancellationToken)
        {
            // InputPort에서 값 가져오기
            var input = Input.GetValueOrDefault();
            
            WasReceived = true;
            ReceivedInt = input;
            
            // 출력
            Output.Value = input;
            
            Debug.WriteLine($"IntTestNode: 입력 받음 - {input}");
            
            await Task.CompletedTask;
            
            yield return FlowOut;
        }
    }
}
