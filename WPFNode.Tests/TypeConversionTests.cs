using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Demo.Nodes;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.String;
using Xunit;
using Newtonsoft.Json;
using System.Threading.Tasks;

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
    }
    
    /// <summary>
    /// int -> Employee 암시적 변환 테스트
    /// </summary>
    [Fact]
    public async Task IntToEmployee_ImplicitConversion_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var intNode = canvas.AddNode<ConstantNode<int>>(0, 100);
        var intToEmployeeNode = canvas.AddNode<IntToEmployeeNode>(100, 100);
        var employeeInfoNode = canvas.AddNode<EmployeeInfoNode>(200, 100);

        // 3. 노드 설정 - 정수 값 설정
        intNode.Value.Value = 1001;

        // 4. 노드 연결
        startNode.FlowOut.Connect(intToEmployeeNode.FlowIn);
        intNode.Result.Connect(intToEmployeeNode.IdInput);
        intToEmployeeNode.EmployeeOutput.Connect(employeeInfoNode.EmployeeInput);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        Assert.Equal(1001, employeeInfoNode.Id.Value);
        Assert.Equal("Employee-1001", employeeInfoNode.Name.Value);
        Assert.Equal("Default", employeeInfoNode.Department.Value);
        Assert.Equal(3000M, employeeInfoNode.Salary.Value);
    }
    
    /// <summary>
    /// string -> Employee 생성자 변환 테스트
    /// </summary>
    [Fact]
    public async Task StringToEmployee_ConstructorConversion_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var stringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var stringToEmployeeNode = canvas.AddNode<StringToEmployeeNode>(100, 100);
        var employeeInfoNode = canvas.AddNode<EmployeeInfoNode>(200, 100);

        // 3. 노드 설정 - JSON 문자열 설정
        var employee = new Employee
        {
            Id = 2001,
            Name = "홍길동",
            Department = "개발",
            Salary = 5000
        };
        stringNode.Value.Value = JsonConvert.SerializeObject(employee);

        // 4. 노드 연결
        startNode.FlowOut.Connect(stringToEmployeeNode.FlowIn);
        stringNode.Result.Connect(stringToEmployeeNode.JsonInput);
        stringToEmployeeNode.EmployeeOutput.Connect(employeeInfoNode.EmployeeInput);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        Assert.Equal(2001, employeeInfoNode.Id.Value);
        Assert.Equal("홍길동", employeeInfoNode.Name.Value);
        Assert.Equal("개발", employeeInfoNode.Department.Value);
        Assert.Equal(5000M, employeeInfoNode.Salary.Value);
    }
    
    /// <summary>
    /// Employee -> string 명시적 변환 테스트
    /// </summary>
    [Fact]
    public async Task EmployeeToString_ExplicitConversion_Test()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var intNode = canvas.AddNode<ConstantNode<int>>(0, 100);
        var intToEmployeeNode = canvas.AddNode<IntToEmployeeNode>(100, 100);
        var employeeToStringNode = canvas.AddNode<EmployeeToStringNode>(200, 100);

        // 3. 노드 설정 - 정수 값 설정
        intNode.Value.Value = 3001;

        // 4. 노드 연결
        startNode.FlowOut.Connect(intToEmployeeNode.FlowIn);
        intNode.Result.Connect(intToEmployeeNode.IdInput);
        intToEmployeeNode.FlowOut.Connect(employeeToStringNode.FlowIn);
        intToEmployeeNode.EmployeeOutput.Connect(employeeToStringNode.EmployeeInput);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - JSON 문자열로 변환되었는지 확인
        string result = employeeToStringNode.JsonOutput.Value as string;
        Assert.NotNull(result);
        
        // JSON 문자열을 다시 Employee로 역직렬화하여 값이 일치하는지 확인
        var resultEmployee = JsonConvert.DeserializeObject<Employee>(result);
        Assert.Equal(3001, resultEmployee.Id);
        Assert.Equal("Employee-3001", resultEmployee.Name);
        Assert.Equal("Default", resultEmployee.Department);
        Assert.Equal(3000M, resultEmployee.Salary);
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
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var intNode = canvas.AddNode<ConstantNode<int>>(0, 100);
        var employeeInfoNode = canvas.AddNode<EmployeeInfoNode>(100, 100);

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
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var stringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var employeeInfoNode = canvas.AddNode<EmployeeInfoNode>(100, 100);

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
}
