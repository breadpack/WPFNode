using WPFNode.Utilities;
using Xunit;

namespace WPFNode.Tests;

public class ConversionCacheIntegrationTests
{
    [Fact]
    public void TryConvertTo_WithCaching_ShouldImprovePerformance()
    {
        // Arrange
        var sourceValue = 42;
        var iterations = 1000;
        
        // Act - 첫 번째 실행 (캐시 없음)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            sourceValue.TryConvertTo<string>(out var _);
        }
        stopwatch.Stop();
        var firstRunTime = stopwatch.ElapsedMilliseconds;
        
        // Act - 두 번째 실행 (캐시 있음)
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            sourceValue.TryConvertTo<string>(out var _);
        }
        stopwatch.Stop();
        var secondRunTime = stopwatch.ElapsedMilliseconds;
        
        // Assert - 두 번째 실행이 더 빠르거나 비슷해야 함
        Assert.True(secondRunTime <= firstRunTime + 5, // 5ms 여유
            $"Second run ({secondRunTime}ms) should be faster than or equal to first run ({firstRunTime}ms)");
    }
    
    [Fact]
    public void TryConvertTo_CachedConversion_ShouldReturnCorrectResult()
    {
        // Arrange
        var intValue = 123;
        var doubleValue = 45.67;
        var stringValue = "789";
        
        // Act & Assert - 첫 번째 변환 (캐시에 저장됨)
        Assert.True(intValue.TryConvertTo<string>(out var intToString1));
        Assert.Equal("123", intToString1);
        
        Assert.True(doubleValue.TryConvertTo<int>(out var doubleToInt1));
        Assert.Equal(45, doubleToInt1); // 45.67은 45로 버림됨 (C# 표준 동작)
        
        Assert.True(stringValue.TryConvertTo<int>(out var stringToInt1));
        Assert.Equal(789, stringToInt1);
        
        // Act & Assert - 두 번째 변환 (캐시에서 가져옴)
        Assert.True(intValue.TryConvertTo<string>(out var intToString2));
        Assert.Equal("123", intToString2);
        
        Assert.True(doubleValue.TryConvertTo<int>(out var doubleToInt2));
        Assert.Equal(45, doubleToInt2); // 캐시된 결과도 동일해야 함
        
        Assert.True(stringValue.TryConvertTo<int>(out var stringToInt2));
        Assert.Equal(789, stringToInt2);
        
        // 결과가 일치하는지 확인
        Assert.Equal(intToString1, intToString2);
        Assert.Equal(doubleToInt1, doubleToInt2);
        Assert.Equal(stringToInt1, stringToInt2);
    }
    
    [Fact]
    public void TryConvertTo_NonGeneric_WithCaching_ShouldWork()
    {
        // Arrange
        var sourceValue = 42;
        var targetType = typeof(string);
        
        // Act - 첫 번째 변환 (캐시에 저장됨)
        var result1 = sourceValue.TryConvertTo(targetType);
        
        // Act - 두 번째 변환 (캐시에서 가져옴)
        var result2 = sourceValue.TryConvertTo(targetType);
        
        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal("42", result1);
        Assert.Equal("42", result2);
        Assert.Equal(result1, result2);
    }
    
    [Fact]
    public void ConversionCache_ShouldHandleComplexTypes()
    {
        // Arrange
        var employee = new WPFNode.Demo.Models.Employee { Id = 1, Name = "Test", Department = "IT", Salary = 50000 };
        
        // Act - Employee to string (explicit operator)
        Assert.True(employee.TryConvertTo<string>(out var employeeJson1));
        Assert.True(employee.TryConvertTo<string>(out var employeeJson2));
        
        // Assert
        Assert.NotNull(employeeJson1);
        Assert.NotNull(employeeJson2);
        Assert.Equal(employeeJson1, employeeJson2);
        Assert.Contains("Test", employeeJson1);
    }
    
    [Fact]
    public void ConversionCache_ShouldHandleFailedConversions()
    {
        // Arrange
        var invalidString = "not_a_number";
        
        // Act - 실패하는 변환을 여러 번 시도
        Assert.False(invalidString.TryConvertTo<int>(out var result1));
        Assert.False(invalidString.TryConvertTo<int>(out var result2));
        
        // Assert - 실패한 변환도 일관되게 처리되어야 함
        Assert.Equal(0, result1);
        Assert.Equal(0, result2);
    }
    
    [Fact]
    public void ConversionCache_ShouldMaintainThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();
        var results = new List<string>();
        var lockObject = new object();
        
        // Act - 여러 스레드에서 동시에 변환 수행
        for (int i = 0; i < 10; i++)
        {
            var value = i;
            tasks.Add(Task.Run(() =>
            {
                if (value.TryConvertTo<string>(out var result))
                {
                    lock (lockObject)
                    {
                        results.Add(result);
                    }
                }
            }));
        }
        
        Task.WaitAll(tasks.ToArray());
        
        // Assert
        Assert.Equal(10, results.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Contains(i.ToString(), results);
        }
    }
} 