using WPFNode.Utilities;
using Xunit;

namespace WPFNode.Tests;

public class DefaultValueConversionTests
{
    [Fact]
    public void TryConvertTo_EmptyStringToInt_ShouldReturnZero()
    {
        // Act
        var success = "".TryConvertTo<int>(out var result);
        
        // Assert
        Assert.True(success);
        Assert.Equal(0, result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToDouble_ShouldReturnZero()
    {
        // Act
        var success = "".TryConvertTo<double>(out var result);
        
        // Assert
        Assert.True(success);
        Assert.Equal(0.0, result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToBoolean_ShouldFail()
    {
        // Act
        var success = "".TryConvertTo<bool>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.False(result); // default value
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToDateTime_ShouldFail()
    {
        // Act
        var success = "".TryConvertTo<DateTime>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(DateTime), result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToTimeSpan_ShouldFail()
    {
        // Act
        var success = "".TryConvertTo<TimeSpan>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(TimeSpan), result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToGuid_ShouldFail()
    {
        // Act
        var success = "".TryConvertTo<Guid>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(Guid), result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToNullableInt_ShouldReturnNull()
    {
        // Act
        var success = "".TryConvertTo<int?>(out var result);
        
        // Assert
        Assert.True(success);
        Assert.Null(result);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void TryConvertTo_WhitespaceStringToInt_ShouldReturnZero(string input)
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
    [InlineData("\t")]
    [InlineData("\n")]
    public void TryConvertTo_WhitespaceStringToBoolean_ShouldFail(string input)
    {
        // Act
        var success = input.TryConvertTo<bool>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.False(result); // default value
    }
    
    [Fact]
    public void TryConvertTo_InvalidStringToDateTime_ShouldFail()
    {
        // Act
        var success = "invalid date".TryConvertTo<DateTime>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(DateTime), result);
    }
    
    [Fact]
    public void TryConvertTo_InvalidStringToGuid_ShouldFail()
    {
        // Act
        var success = "not a guid".TryConvertTo<Guid>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(Guid), result);
    }
    
    [Fact]
    public void TryConvertTo_EmptyStringToEnum_ShouldFail()
    {
        // Act
        var success = "".TryConvertTo<DayOfWeek>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(DayOfWeek), result);
    }
    
    [Fact]
    public void TryConvertTo_InvalidStringToEnum_ShouldFail()
    {
        // Act
        var success = "InvalidDay".TryConvertTo<DayOfWeek>(out var result);
        
        // Assert
        Assert.False(success);
        Assert.Equal(default(DayOfWeek), result);
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
    
    [Fact]
    public void TryConvertTo_NonGeneric_EmptyStringToBoolean_ShouldReturnNull()
    {
        // Act
        var result = "".TryConvertTo(typeof(bool));
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void TryConvertTo_NonGeneric_EmptyStringToDateTime_ShouldReturnNull()
    {
        // Act
        var result = "".TryConvertTo(typeof(DateTime));
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void TryConvertTo_NonGeneric_EmptyStringToNullableInt_ShouldReturnNull()
    {
        // Act
        var result = "".TryConvertTo(typeof(int?));
        
        // Assert
        Assert.Null(result);
    }
    
    [Fact]
    public void TryConvertTo_CachedNumericConversion_ShouldBeConsistent()
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
    public void TryConvertTo_InvalidConversion_ShouldFail()
    {
        // Arrange - 예외를 발생시킬 수 있는 잘못된 형식의 문자열들
        var invalidInputs = new[] { "definitely not a number", "∞", "NaN", "1.2.3.4" };
        
        foreach (var invalidInput in invalidInputs)
        {
            // Act & Assert - 예외가 발생하면 변환 실패를 반환해야 함
            Assert.False(invalidInput.TryConvertTo<bool>(out var boolResult));
            Assert.False(boolResult); // default value
            
            Assert.False(invalidInput.TryConvertTo<DateTime>(out var dateResult));
            Assert.Equal(default(DateTime), dateResult);
            
            Assert.False(invalidInput.TryConvertTo<Guid>(out var guidResult));
            Assert.Equal(default(Guid), guidResult);
        }
    }
    
    [Fact]
    public void TryConvertTo_ValidNumericString_ShouldSucceed()
    {
        // Act & Assert
        Assert.True("123".TryConvertTo<int>(out var intResult));
        Assert.Equal(123, intResult);
        
        Assert.True("45.67".TryConvertTo<double>(out var doubleResult));
        Assert.Equal(45.67, doubleResult);
        
        Assert.True("true".TryConvertTo<bool>(out var boolResult));
        Assert.True(boolResult);
        
        Assert.True("Monday".TryConvertTo<DayOfWeek>(out var enumResult));
        Assert.Equal(DayOfWeek.Monday, enumResult);
    }
} 