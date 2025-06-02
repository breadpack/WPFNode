using WPFNode.Utilities;
using Xunit;

namespace WPFNode.Tests;

public class StringToNumericConversionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void TryConvertTo_EmptyOrWhitespaceStringToInt_ShouldReturnZero(string input)
    {
        // Act
        var success = input.TryConvertTo<int>(out var result);
        
        // Assert
        Assert.True(success);
        Assert.Equal(0, result);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryConvertTo_EmptyStringToDouble_ShouldReturnZero(string input)
    {
        // Act
        var success = input.TryConvertTo<double>(out var result);
        
        // Assert
        Assert.True(success);
        Assert.Equal(0.0, result);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryConvertTo_EmptyStringToDecimal_ShouldReturnZero(string input)
    {
        // Act
        var success = input.TryConvertTo<decimal>(out var result);
        
        // Assert
        Assert.True(success);
        Assert.Equal(0m, result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToAllNumericTypes_ShouldReturnZero()
    {
        var emptyString = "";
        
        // byte
        Assert.True(emptyString.TryConvertTo<byte>(out var byteResult));
        Assert.Equal((byte)0, byteResult);
        
        // sbyte
        Assert.True(emptyString.TryConvertTo<sbyte>(out var sbyteResult));
        Assert.Equal((sbyte)0, sbyteResult);
        
        // short
        Assert.True(emptyString.TryConvertTo<short>(out var shortResult));
        Assert.Equal((short)0, shortResult);
        
        // ushort
        Assert.True(emptyString.TryConvertTo<ushort>(out var ushortResult));
        Assert.Equal((ushort)0, ushortResult);
        
        // int
        Assert.True(emptyString.TryConvertTo<int>(out var intResult));
        Assert.Equal(0, intResult);
        
        // uint
        Assert.True(emptyString.TryConvertTo<uint>(out var uintResult));
        Assert.Equal(0U, uintResult);
        
        // long
        Assert.True(emptyString.TryConvertTo<long>(out var longResult));
        Assert.Equal(0L, longResult);
        
        // ulong
        Assert.True(emptyString.TryConvertTo<ulong>(out var ulongResult));
        Assert.Equal(0UL, ulongResult);
        
        // float
        Assert.True(emptyString.TryConvertTo<float>(out var floatResult));
        Assert.Equal(0.0f, floatResult);
        
        // double
        Assert.True(emptyString.TryConvertTo<double>(out var doubleResult));
        Assert.Equal(0.0, doubleResult);
        
        // decimal
        Assert.True(emptyString.TryConvertTo<decimal>(out var decimalResult));
        Assert.Equal(0m, decimalResult);
    }
    
    [Fact]
    public void TryConvertTo_InvalidStringToInt_ShouldReturnZeroOrFail()
    {
        // Arrange - 명확히 유효하지 않은 문자열들
        var invalidStrings = new[] { "abc", "12.34.56", "not a number", "∞" };
        
        foreach (var invalidString in invalidStrings)
        {
            // Act
            var success = invalidString.TryConvertTo<int>(out var result);
            
            // Assert - 성공하면 0이어야 하고, 실패해도 괜찮음
            if (success)
            {
                Assert.Equal(0, result);
            }
            // 실패하는 것도 허용 (모든 유효하지 않은 문자열이 0으로 변환되지는 않을 수 있음)
        }
    }
    
    [Fact]
    public void TryConvertTo_CachedEmptyStringConversion_ShouldBeConsistent()
    {
        var emptyString = "";
        
        // 첫 번째 변환 (캐시에 저장됨)
        Assert.True(emptyString.TryConvertTo<int>(out var result1));
        Assert.Equal(0, result1);
        
        // 두 번째 변환 (캐시에서 가져옴)
        Assert.True(emptyString.TryConvertTo<int>(out var result2));
        Assert.Equal(0, result2);
        
        // 결과 일치 확인
        Assert.Equal(result1, result2);
    }
    
    [Fact]
    public void TryConvertTo_NonGeneric_EmptyStringToInt_ShouldReturnZero()
    {
        // Act
        var result = "".TryConvertTo(typeof(int));
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result);
    }
} 