using System;

namespace WPFNode.Utilities;

/// <summary>
/// 타입 변환 전략을 정의하는 열거형
/// TypeUtility.TryConvertTo 메서드의 9단계 변환 로직을 기반으로 함
/// </summary>
public enum ConversionStrategy : byte
{
    /// <summary>
    /// 변환 불가능
    /// </summary>
    None = 0,
    
    /// <summary>
    /// 직접 타입 변환 (is 연산자)
    /// 예: object가 이미 대상 타입인 경우
    /// </summary>
    Direct = 1,
    
    /// <summary>
    /// 숫자 타입 간 변환 (Convert.ChangeType)
    /// 예: int -> double, float -> decimal
    /// </summary>
    Numeric = 2,
    
    /// <summary>
    /// 소스 타입의 암시적 변환 연산자 (op_Implicit)
    /// 예: 커스텀 타입에서 정의된 implicit operator
    /// </summary>
    SourceImplicit = 3,
    
    /// <summary>
    /// 대상 타입의 암시적 변환 연산자 (op_Implicit)
    /// 예: 대상 타입에서 정의된 implicit operator
    /// </summary>
    TargetImplicit = 4,
    
    /// <summary>
    /// 명시적 변환 연산자 (op_Explicit)
    /// 예: 커스텀 타입의 explicit operator
    /// </summary>
    Explicit = 5,
    
    /// <summary>
    /// 생성자를 통한 변환
    /// 예: new TargetType(sourceValue)
    /// </summary>
    Constructor = 6,
    
    /// <summary>
    /// Parse/TryParse 메서드를 통한 문자열 변환
    /// 예: int.Parse(stringValue), DateTime.TryParse
    /// </summary>
    Parse = 7,
    
    /// <summary>
    /// TypeConverter를 통한 변환
    /// 예: TypeDescriptor.GetConverter 사용
    /// </summary>
    TypeConverter = 8,
    
    /// <summary>
    /// ToString 메서드를 통한 문자열 변환
    /// 예: anyObject.ToString()
    /// </summary>
    ToString = 9
}

/// <summary>
/// ConversionStrategy 확장 메서드
/// </summary>
public static class ConversionStrategyExtensions
{
    /// <summary>
    /// 변환 전략이 유효한지 확인
    /// </summary>
    public static bool IsValid(this ConversionStrategy strategy)
    {
        return strategy != ConversionStrategy.None;
    }
    
    /// <summary>
    /// 변환 전략의 우선순위 반환 (낮을수록 높은 우선순위)
    /// </summary>
    public static int GetPriority(this ConversionStrategy strategy)
    {
        return (int)strategy;
    }
    
    /// <summary>
    /// 변환 전략의 설명 반환
    /// </summary>
    public static string GetDescription(this ConversionStrategy strategy)
    {
        return strategy switch
        {
            ConversionStrategy.None => "변환 불가능",
            ConversionStrategy.Direct => "직접 타입 변환",
            ConversionStrategy.Numeric => "숫자 타입 간 변환",
            ConversionStrategy.SourceImplicit => "소스 암시적 변환",
            ConversionStrategy.TargetImplicit => "대상 암시적 변환",
            ConversionStrategy.Explicit => "명시적 변환",
            ConversionStrategy.Constructor => "생성자 변환",
            ConversionStrategy.Parse => "Parse/TryParse 변환",
            ConversionStrategy.TypeConverter => "TypeConverter 변환",
            ConversionStrategy.ToString => "ToString 변환",
            _ => "알 수 없는 전략"
        };
    }
} 