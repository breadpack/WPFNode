using System;
using WPFNode.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace WPFNode.Tests;

/// <summary>
/// 통합된 ConversionCache의 기본 동작 테스트
/// </summary>
public class ConversionCacheTests
{
    private readonly ITestOutputHelper _output;
    
    public ConversionCacheTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Fact]
    public void ConversionCache_BasicOperation_ShouldWork()
    {
        // Arrange
        ConversionCache.Clear();
        var sourceType = typeof(int);
        var targetType = typeof(string);
        
        // Act & Assert - 초기에는 캐시에 없어야 함
        var found = ConversionCache.TryGetConversionStrategy(sourceType, targetType, out var entry);
        Assert.False(found);
        
        // 캐시에 추가
        var newEntry = new ConversionCacheEntry(ConversionStrategy.ToString);
        ConversionCache.CacheConversionStrategy(sourceType, targetType, newEntry);
        
        // 캐시에서 찾을 수 있어야 함
        found = ConversionCache.TryGetConversionStrategy(sourceType, targetType, out entry);
        Assert.True(found);
        Assert.Equal(ConversionStrategy.ToString, entry.Strategy);
        
        _output.WriteLine("✅ ConversionCache 기본 동작 테스트 성공");
    }
    
    [Fact]
    public void ConversionCache_PerformanceMode_ShouldWork()
    {
        // Arrange
        ConversionCache.Clear();
        
        // Act & Assert - 성능 모드 설정
        ConversionCache.IsPerformanceMode = true;
        Assert.True(ConversionCache.IsPerformanceMode);
        
        ConversionCache.IsPerformanceMode = false;
        Assert.False(ConversionCache.IsPerformanceMode);
        
        _output.WriteLine("✅ ConversionCache 성능 모드 설정 테스트 성공");
    }
    
    [Fact]
    public void ConversionCache_GetStatus_ShouldWork()
    {
        // Arrange
        ConversionCache.Clear();
        
        // Act
        var status = ConversionCache.GetCacheStatus();
        
        // Assert
        Assert.Equal(0, status.TypePairCount);
        Assert.Equal(0, status.TypeMethodCount);
        
        _output.WriteLine($"✅ ConversionCache 상태 조회 테스트 성공: {status}");
    }
} 