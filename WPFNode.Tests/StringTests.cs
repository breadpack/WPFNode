using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Plugins.Basic;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Plugins.Basic.Constants;
using WPFNode.Plugins.Basic.String;
using System.Collections.Generic;
using Xunit;

namespace WPFNode.Tests;

public class StringTests
{
    private readonly ILogger _logger;

    public StringTests()
    {
        // 로깅 설정
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<StringTests>();
    }

    [Fact]
    public async Task IsNullOrEmpty_NullString_ShouldReturnTrue()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var nullStringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrEmptyNode = canvas.AddNode<StringIsNullOrEmptyNode>(100, 50);

        // 3. 노드 설정 - null 문자열 설정
        nullStringNode.Value.Value = null;

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrEmptyNode.FlowIn);
        nullStringNode.Result.Connect(isNullOrEmptyNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - null은 IsNullOrEmpty에서 true 반환
        Assert.True(isNullOrEmptyNode.ResultValue);
    }

    [Fact]
    public async Task IsNullOrEmpty_EmptyString_ShouldReturnTrue()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var emptyStringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrEmptyNode = canvas.AddNode<StringIsNullOrEmptyNode>(100, 50);

        // 3. 노드 설정 - 빈 문자열 설정
        emptyStringNode.Value.Value = "";

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrEmptyNode.FlowIn);
        emptyStringNode.Result.Connect(isNullOrEmptyNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 빈 문자열은 IsNullOrEmpty에서 true 반환
        Assert.True(isNullOrEmptyNode.ResultValue);
    }

    [Fact]
    public async Task IsNullOrEmpty_NonEmptyString_ShouldReturnFalse()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var stringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrEmptyNode = canvas.AddNode<StringIsNullOrEmptyNode>(100, 50);

        // 3. 노드 설정 - 비어있지 않은 문자열 설정
        stringNode.Value.Value = "Hello";

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrEmptyNode.FlowIn);
        stringNode.Result.Connect(isNullOrEmptyNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 비어있지 않은 문자열은 IsNullOrEmpty에서 false 반환
        Assert.False(isNullOrEmptyNode.ResultValue);
    }

    [Fact]
    public async Task IsNullOrWhiteSpace_NullString_ShouldReturnTrue()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var nullStringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrWhiteSpaceNode = canvas.AddNode<StringIsNullOrWhiteSpaceNode>(100, 50);

        // 3. 노드 설정 - null 문자열 설정
        nullStringNode.Value.Value = null;

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrWhiteSpaceNode.FlowIn);
        nullStringNode.Result.Connect(isNullOrWhiteSpaceNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - null은 IsNullOrWhiteSpace에서 true 반환
        Assert.True(isNullOrWhiteSpaceNode.ResultValue);
    }

    [Fact]
    public async Task IsNullOrWhiteSpace_EmptyString_ShouldReturnTrue()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var emptyStringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrWhiteSpaceNode = canvas.AddNode<StringIsNullOrWhiteSpaceNode>(100, 50);

        // 3. 노드 설정 - 빈 문자열 설정
        emptyStringNode.Value.Value = "";

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrWhiteSpaceNode.FlowIn);
        emptyStringNode.Result.Connect(isNullOrWhiteSpaceNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 빈 문자열은 IsNullOrWhiteSpace에서 true 반환
        Assert.True(isNullOrWhiteSpaceNode.ResultValue);
    }

    [Fact]
    public async Task IsNullOrWhiteSpace_WhiteSpaceString_ShouldReturnTrue()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var whiteSpaceStringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrWhiteSpaceNode = canvas.AddNode<StringIsNullOrWhiteSpaceNode>(100, 50);

        // 3. 노드 설정 - 공백만 있는 문자열 설정
        whiteSpaceStringNode.Value.Value = "   \t\n";

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrWhiteSpaceNode.FlowIn);
        whiteSpaceStringNode.Result.Connect(isNullOrWhiteSpaceNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 공백만 있는 문자열은 IsNullOrWhiteSpace에서 true 반환
        Assert.True(isNullOrWhiteSpaceNode.ResultValue);
    }

    [Fact]
    public async Task IsNullOrWhiteSpace_NonEmptyString_ShouldReturnFalse()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var stringNode = canvas.AddNode<ConstantNode<string>>(0, 100);
        var isNullOrWhiteSpaceNode = canvas.AddNode<StringIsNullOrWhiteSpaceNode>(100, 50);

        // 3. 노드 설정 - 비어있지 않은 문자열 설정
        stringNode.Value.Value = "Hello World";

        // 4. 노드 연결
        startNode.FlowOut.Connect(isNullOrWhiteSpaceNode.FlowIn);
        stringNode.Result.Connect(isNullOrWhiteSpaceNode.Input);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 비어있지 않은 문자열은 IsNullOrWhiteSpace에서 false 반환
        Assert.False(isNullOrWhiteSpaceNode.ResultValue);
    }

    [Fact]
    public async Task StringFormat_WithOneParameter_ShouldFormatCorrectly()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var formatNode = canvas.AddNode<StringFormatNode>(100, 50);
        var param0Node = canvas.AddNode<ConstantNode<string>>(0, 100);

        // 3. 노드 설정
        formatNode.FormatString.Value = "Hello, {0}!";
        param0Node.Value.Value = "World";

        // 4. 노드 연결
        startNode.FlowOut.Connect(formatNode.FlowIn);

        // FormatString을 설정하면 자동으로 매개변수 포트가 구성됨
        // 이제 동적으로 생성된 첫 번째 매개변수 포트를 찾아 연결
        var paramPort = formatNode.InputPorts.FirstOrDefault(p => p.Name == "매개변수 0");
        Assert.NotNull(paramPort); // 매개변수 포트가 제대로 생성되었는지 확인
        param0Node.Result.Connect(paramPort);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        Assert.Equal("Hello, World!", formatNode.Result.Value);
    }

    [Fact]
    public async Task StringFormat_WithMultipleParameters_ShouldFormatCorrectly()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var formatNode = canvas.AddNode<StringFormatNode>(100, 50);
        var param0Node = canvas.AddNode<ConstantNode<string>>(0, 100);
        var param1Node = canvas.AddNode<ConstantNode<int>>(0, 150);
        var param2Node = canvas.AddNode<ConstantNode<double>>(0, 200);

        // 3. 노드 설정
        formatNode.FormatString.Value = "{0}님 안녕하세요! 현재 {1}개의 메시지가 있으며, 평점은 {2:F1}점입니다.";
        param0Node.Value.Value = "홍길동";
        param1Node.Value.Value = 5;
        param2Node.Value.Value = 4.5;

        // 4. 노드 연결
        startNode.FlowOut.Connect(formatNode.FlowIn);

        // FormatString을 설정하면 자동으로 매개변수 포트가 구성됨
        // 동적으로 생성된 매개변수 포트에 연결
        var param0Port = formatNode.InputPorts.FirstOrDefault(p => p.Name == "매개변수 0");
        var param1Port = formatNode.InputPorts.FirstOrDefault(p => p.Name == "매개변수 1");
        var param2Port = formatNode.InputPorts.FirstOrDefault(p => p.Name == "매개변수 2");
        
        Assert.NotNull(param0Port); // 매개변수 포트가 제대로 생성되었는지 확인
        Assert.NotNull(param1Port);
        Assert.NotNull(param2Port);
        
        param0Node.Result.Connect(param0Port);
        param1Node.Result.Connect(param1Port);
        param2Node.Result.Connect(param2Port);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인
        Assert.Equal("홍길동님 안녕하세요! 현재 5개의 메시지가 있으며, 평점은 4.5점입니다.", formatNode.Result.Value);
    }

    [Fact]
    public async Task StringFormat_WithInvalidFormat_ShouldHandleGracefully()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var formatNode = canvas.AddNode<StringFormatNode>(100, 50);
        var param0Node = canvas.AddNode<ConstantNode<string>>(0, 100);

        // 3. 노드 설정 - 잘못된 형식 문자열 설정
        formatNode.FormatString.Value = "오류가 있는 {0} {1 형식 문자열";
        param0Node.Value.Value = "파라미터";

        // 4. 노드 연결
        startNode.FlowOut.Connect(formatNode.FlowIn);

        // FormatString을 설정하면 자동으로 매개변수 포트가 구성됨
        // 동적으로 생성된 매개변수 포트에 연결
        var paramPort = formatNode.InputPorts.FirstOrDefault(p => p.Name == "매개변수 0");
        Assert.NotNull(paramPort); // 매개변수 포트가 제대로 생성되었는지 확인
        param0Node.Result.Connect(paramPort);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 오류 발생 시 원본 형식 문자열을 반환하는지 확인
        Assert.Equal("오류가 있는 {0} {1 형식 문자열", formatNode.Result.Value);
    }

    [Fact]
    public async Task StringFormat_WithMissingParameters_ShouldHandleGracefully()
    {
        // 1. 새 캔버스 생성
        var canvas = NodeCanvas.Create();

        // 2. 노드 추가
        var startNode = canvas.AddNode<StartNode>(0, 0);
        var formatNode = canvas.AddNode<StringFormatNode>(100, 50);
        var param0Node = canvas.AddNode<ConstantNode<string>>(0, 100);

        // 3. 노드 설정 - 부족한 파라미터 설정
        formatNode.FormatString.Value = "파라미터 0: {0}, 파라미터 1: {1}, 파라미터 2: {2}";
        param0Node.Value.Value = "첫번째";

        // 4. 노드 연결
        startNode.FlowOut.Connect(formatNode.FlowIn);

        // FormatString을 설정하면 자동으로 매개변수 포트가 구성됨
        // 동적으로 생성된 첫 번째 매개변수 포트에만 연결 (나머지는 기본값)
        var paramPort = formatNode.InputPorts.FirstOrDefault(p => p.Name == "매개변수 0");
        Assert.NotNull(paramPort); // 매개변수 포트가 제대로 생성되었는지 확인
        param0Node.Result.Connect(paramPort);

        // 5. 실행
        await canvas.ExecuteAsync();

        // 6. 결과 확인 - 연결되지 않은 파라미터는 빈 문자열로 처리되는지 확인
        Assert.Equal("파라미터 0: 첫번째, 파라미터 1: , 파라미터 2: ", formatNode.Result.Value);
    }
}
