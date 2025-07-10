using Microsoft.Extensions.Logging;
using WPFNode.Models;
using WPFNode.Plugins.Basic.Flow;
using WPFNode.Services;
using Xunit;
using Xunit.Abstractions;

namespace WPFNode.Tests;

public class FlowPortConnectionRemovalTests
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<FlowPortConnectionRemovalTests> _logger;

    public FlowPortConnectionRemovalTests(ITestOutputHelper output)
    {
        _output = output;
        
        // 로거 설정
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        _logger = loggerFactory.CreateLogger<FlowPortConnectionRemovalTests>();
        
        // 서비스 초기화
        NodeServices.Initialize("Test");
    }

    [Fact]
    public async Task RemoveNode_WithFlowConnections_ShouldRemoveAllConnections()
    {
        // Arrange
        var canvas = NodeCanvas.Create();
        
        // Start 노드 생성
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        
        // If 노드 생성 
        var ifNode = canvas.CreateNode<IfNode>(200, 0);
        
        // Flow 연결: Start -> If
        var flowConnection = canvas.Connect(startNode.FlowOut, ifNode.FlowIn);
        
        // 연결 상태 확인
        Assert.Single(canvas.Connections);
        Assert.Contains(flowConnection, canvas.Connections);
        
        _logger.LogInformation($"연결 생성 완료: {canvas.Connections.Count}개");
        
        // Act - If 노드 제거
        canvas.RemoveNode(ifNode);
        
        // Assert - 모든 연결이 제거되었는지 확인
        Assert.Empty(canvas.Connections);
        Assert.DoesNotContain(flowConnection, canvas.Connections);
        
        // 포트의 연결 상태도 확인
        Assert.False(startNode.FlowOut.IsConnected);
        
        _logger.LogInformation("FlowPort 연결 제거 테스트 성공");
    }

    [Fact]
    public async Task RemoveNode_WithMultipleFlowConnections_ShouldRemoveAllConnections()
    {
        // Arrange
        var canvas = NodeCanvas.Create();
        
        // Start 노드 생성
        var startNode = canvas.CreateNode<StartNode>(0, 0);
        
        // If 노드 생성 (중간 노드)
        var ifNode = canvas.CreateNode<IfNode>(200, 0);
        
        // 두 번째 If 노드 생성 (True 분기)
        var ifNode2 = canvas.CreateNode<IfNode>(400, 0);
        
        // 세 번째 If 노드 생성 (False 분기)
        var ifNode3 = canvas.CreateNode<IfNode>(400, 200);
        
        // Flow 연결들 생성
        var flowConn1 = canvas.Connect(startNode.FlowOut, ifNode.FlowIn);
        var flowConn2 = canvas.Connect(ifNode.TruePort, ifNode2.FlowIn);
        var flowConn3 = canvas.Connect(ifNode.FalsePort, ifNode3.FlowIn);
        
        // 연결 상태 확인
        Assert.Equal(3, canvas.Connections.Count);
        
        _logger.LogInformation($"다중 연결 생성 완료: {canvas.Connections.Count}개");
        
        // Act - 중간 If 노드 제거
        canvas.RemoveNode(ifNode);
        
        // Assert - ifNode와 관련된 연결만 제거되었는지 확인
        Assert.Empty(canvas.Connections); // 모든 연결이 제거되어야 함 (ifNode가 중간에 있으므로)
        
        // 제거된 연결들 확인
        Assert.DoesNotContain(flowConn1, canvas.Connections);
        Assert.DoesNotContain(flowConn2, canvas.Connections);
        Assert.DoesNotContain(flowConn3, canvas.Connections);
        
        // 포트 연결 상태 확인
        Assert.False(startNode.FlowOut.IsConnected);
        
        _logger.LogInformation("다중 FlowPort 연결 제거 테스트 성공");
    }
} 