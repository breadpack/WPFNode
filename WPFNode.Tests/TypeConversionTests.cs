using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Demo.Nodes;
using WPFNode.Tests.Models;
using WPFNode.Tests.Helpers;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.String;
using Xunit;
using Newtonsoft.Json;
using System.Threading.Tasks;
using WPFNode.Tests.Helpers;

namespace WPFNode.Tests;

/// <summary>
/// 타입 변환 테스트 - NodeCanvas를 통한 실제 노드 생성 및 실행을 통해 변환 기능을 테스트합니다.
/// </summary>
public class TypeConversionTests
{
    private readonly ILogger _logger;

    public TypeConversionTests()
    {
        // 로깅 설정
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<TypeConversionTests>();
        
        // 테스트 노드 등록
        RegisterTestNodes();
    }
    
    /// <summary>
    /// 테스트에 필요한 노드 타입 등록
    /// </summary>
    private void RegisterTestNodes()
    {
        var pluginService = WPFNode.Services.NodeServices.PluginService;
        
        // 기본 노드 등록
        pluginService.RegisterNodeType(typeof(StartNode));
        pluginService.RegisterNodeType(typeof(ConstantNode<int>));
        pluginService.RegisterNodeType(typeof(ConstantNode<string>));
        pluginService.RegisterNodeType(typeof(StringFormatNode));
        
        // 커스텀 노드 등록
        pluginService.RegisterNodeType(typeof(IntToEmployeeNode));
        pluginService.RegisterNodeType(typeof(StringToEmployeeNode));
        pluginService.RegisterNodeType(typeof(EmployeeInfoNode));
        pluginService.RegisterNodeType(typeof(EmployeeToStringNode));
        
        // 테스트 변환 타입 노드 등록
        pluginService.RegisterNodeType(typeof(StringConstructorInfoNode));
        pluginService.RegisterNodeType(typeof(ImplicitConversionInfoNode));
        pluginService.RegisterNodeType(typeof(ExplicitConversionToStringNode));
        pluginService.RegisterNodeType(typeof(StringTestNode));
        pluginService.RegisterNodeType(typeof(IntTestNode));
    }
    
    /// <summary>
    /// 직접 int -> Employee 변환 테스트 (중간 노드 없이)
    /// </summary>
    [Fact]
    public async Task DirectConversion_IntToEmployee_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        var intNode = canvas.CreateNode<ConstantNode<int>>(0, 100);
        var employeeInfoNode = canvas.CreateNode<EmployeeInfoNode>(100, 100);

        // 3. 노드 설정 - 정수 값 설정
        intNode.Value.Value = 4001;

        // 4. 노드 연결 - int 출력 포트를 직접 Employee 입력 포트에 연결
        startNode.FlowOut.Connect(employeeInfoNode.FlowIn);
        intNode.Result.Connect(employeeInfoNode.EmployeeInput);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - int가 암시적으로 Employee로 변환되었는지 확인
        Assert.Equal(4001, employeeInfoNode.Id.Value);
        Assert.Equal("Employee-4001", employeeInfoNode.Name.Value);
        Assert.Equal("Default", employeeInfoNode.Department.Value);
        Assert.Equal(3000M, employeeInfoNode.Salary.Value);
    }
    
    /// <summary>
    /// 직접 string -> Employee 변환 테스트 (중간 노드 없이)
    /// </summary>
    [Fact]
    public async Task DirectConversion_StringToEmployee_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        var stringNode = canvas.CreateNode<ConstantNode<string>>(0, 100);
        var employeeInfoNode = canvas.CreateNode<EmployeeInfoNode>(100, 100);

        // 3. 노드 설정 - JSON 문자열 설정
        var employee = new Employee
        {
            Id = 5001,
            Name = "김철수",
            Department = "인사",
            Salary = 4500
        };
        stringNode.Value.Value = JsonConvert.SerializeObject(employee);

        // 4. 노드 연결 - string 출력 포트를 직접 Employee 입력 포트에 연결
        startNode.FlowOut.Connect(employeeInfoNode.FlowIn);
        stringNode.Result.Connect(employeeInfoNode.EmployeeInput);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - string이 생성자를 통해 Employee로 변환되었는지 확인
        Assert.Equal(5001, employeeInfoNode.Id.Value);
        Assert.Equal("김철수", employeeInfoNode.Name.Value);
        Assert.Equal("인사", employeeInfoNode.Department.Value);
        Assert.Equal(4500M, employeeInfoNode.Salary.Value);
    }
    
    /// <summary>
    /// 타입 유틸리티 기능 테스트
    /// </summary>
    [Fact]
    public void TypeUtility_CanConvertTo_CustomType_Test()
    {
        // 1. 타입 변환 가능 여부 - int -> Employee
        bool intToEmployee = typeof(int).CanConvertTo(typeof(Employee));
        Assert.True(intToEmployee);
        
        // 2. 타입 변환 가능 여부 - string -> Employee
        bool stringToEmployee = typeof(string).CanConvertTo(typeof(Employee));
        Assert.True(stringToEmployee);
        
        // 3. 타입 변환 가능 여부 - Employee -> string
        bool employeeToString = typeof(Employee).CanConvertTo(typeof(string));
        Assert.True(employeeToString);
        
        // 4. 타입 변환 불가능 여부 확인 - bool -> Employee
        bool boolToEmployee = typeof(bool).CanConvertTo(typeof(Employee));
        Assert.False(boolToEmployee);
    }
    
    /// <summary>
    /// 생성자에서 문자열을 받는 타입(StringConstructorType)이 InputPort에서 자동으로 변환되는지 테스트
    /// </summary>
    [Fact]
    public async Task StringConstructor_InputPort_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        var stringNode = canvas.CreateNode<ConstantNode<string>>(0, 100);
        var infoNode = canvas.CreateNode<StringConstructorInfoNode>(300, 100);

        // 3. 노드 설정 - 테스트할 문자열 설정
        stringNode.Value.Value = "테스트이름:샘플값:테스트카테고리";

        // 4. 노드 연결 - 문자열 출력 포트를 StringConstructorType 입력 포트에 연결
        startNode.FlowOut.Connect(infoNode.FlowIn);
        stringNode.Result.Connect(infoNode.TypeInput);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 문자열이 생성자를 통해 StringConstructorType으로 변환되었는지 확인
        Assert.True(infoNode.WasReceived, "StringConstructorType이 수신되지 않았습니다.");
        Assert.NotNull(infoNode.ReceivedType);
        Assert.Equal("테스트이름", infoNode.Name.Value);
        Assert.Equal("샘플값", infoNode.Value.Value);
        Assert.Equal("테스트카테고리", infoNode.Category.Value);
    }
    
    /// <summary>
    /// 암시적 변환 연산자를 가진 타입(ImplicitConversionType)이 InputPort에서 자동으로 변환되는지 테스트
    /// </summary>
    [Fact]
    public async Task ImplicitConversion_InputPort_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        var stringNode = canvas.CreateNode<ConstantNode<string>>(0, 100);
        var intNode = canvas.CreateNode<ConstantNode<int>>(0, 200);
        var infoNode1 = canvas.CreateNode<ImplicitConversionInfoNode>(300, 100);
        var infoNode2 = canvas.CreateNode<ImplicitConversionInfoNode>(300, 300);

        // 3. 노드 설정
        stringNode.Value.Value = "테스트문자열";
        intNode.Value.Value = 42;

        // 4. 노드 연결 - 두 가지 타입의 입력을 ImplicitConversionType 입력 포트에 연결
        startNode.FlowOut.Connect(infoNode1.FlowIn);
        infoNode1.FlowOut.Connect(infoNode2.FlowIn);
        
        stringNode.Result.Connect(infoNode1.TypeInput); // string -> ImplicitConversionType
        intNode.Result.Connect(infoNode2.TypeInput);    // int -> ImplicitConversionType

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 암시적 변환 확인
        // string -> ImplicitConversionType 검증
        Assert.True(infoNode1.WasReceived, "string에서 변환된 ImplicitConversionType이 수신되지 않았습니다.");
        Assert.NotNull(infoNode1.ReceivedType);
        Assert.Equal("String", infoNode1.Source.Value);
        Assert.Equal(stringNode.Value.Value.Length, infoNode1.Value.Value);
        
        // int -> ImplicitConversionType 검증
        Assert.True(infoNode2.WasReceived, "int에서 변환된 ImplicitConversionType이 수신되지 않았습니다.");
        Assert.NotNull(infoNode2.ReceivedType);
        Assert.Equal("Integer", infoNode2.Source.Value);
        Assert.Equal(intNode.Value.Value, infoNode2.Value.Value);
    }
    
    /// <summary>
    /// 명시적 변환 연산자를 가진 타입(ExplicitConversionType)이 OutputPort에서 자동으로 변환되는지 테스트
    /// </summary>
    [Fact]
    public async Task ExplicitConversion_OutputPort_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        var stringConstNode = canvas.CreateNode<ConstantNode<string>>(0, 100);
        var conversionNode = canvas.CreateNode<ExplicitConversionToStringNode>(200, 100);
        var stringTestNode = canvas.CreateNode<StringTestNode>(400, 50);
        var intTestNode = canvas.CreateNode<IntTestNode>(400, 150);

        // 3. 노드 설정 - ExplicitConversionType을 생성할 문자열 설정
        var testData = "42";
        var testType = "Number";
        
        // 테스트를 위한 ExplicitConversionType 인스턴스 생성
        var explicitType = new ExplicitConversionType(testData, testType);
        stringConstNode.Value.Value = (string)explicitType; // 이미 변환된 문자열

        // 4. 노드 연결
        startNode.FlowOut.Connect(conversionNode.FlowIn);
        conversionNode.FlowOut.Connect(stringTestNode.FlowIn);
        stringTestNode.FlowOut.Connect(intTestNode.FlowIn);
        
        // 명시적 변환 테스트 (ExplicitConversionType -> string, int)
        // InputPort에 직접 값을 설정하는 대신 상수 노드를 추가하여 연결
        var explicitTypeNode = canvas.CreateNode<ConstantNode<ExplicitConversionType>>(0, 300);
        explicitTypeNode.Value.Value = explicitType;
        
        explicitTypeNode.Result.Connect(conversionNode.TypeInput);  // ExplicitConversionType 연결
        conversionNode.StringResult.Connect(stringTestNode.Input);  // string으로 변환
        conversionNode.IntResult.Connect(intTestNode.Input);        // int로 변환

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 명시적 변환 확인
        Assert.True(conversionNode.WasReceived, "ExplicitConversionType이 수신되지 않았습니다.");
        Assert.NotNull(conversionNode.ReceivedType);
        
        // string 변환 검증
        Assert.True(stringTestNode.WasReceived, "string으로 변환된 결과가 수신되지 않았습니다.");
        Assert.Equal($"{testType}:{testData}", stringTestNode.ReceivedString);
        
        // int 변환 검증
        Assert.True(intTestNode.WasReceived, "int로 변환된 결과가 수신되지 않았습니다.");
        Assert.Equal(int.Parse(testData), intTestNode.ReceivedInt);
    }
}
