using System;
using Newtonsoft.Json;

namespace WPFNode.Tests.Models
{
    /// <summary>
    /// 생성자에서 문자열을 받아 초기화하는 테스트 타입
    /// 형식: "Name:Value:Category"
    /// </summary>
    public class StringConstructorType
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Category { get; set; }

        // 기본 생성자 (역직렬화 등 필요)
        public StringConstructorType()
        {
            Name = string.Empty;
            Value = string.Empty;
            Category = string.Empty;
        }

        // 문자열 생성자 - "Name:Value:Category" 형식의 문자열을 파싱하여 초기화
        public StringConstructorType(string input)
        {
            try
            {
                var parts = input.Split(':');
                if (parts.Length >= 1) Name = parts[0];
                if (parts.Length >= 2) Value = parts[1];
                if (parts.Length >= 3) Category = parts[2];

                // 비어있는 경우 기본값 설정
                Name = string.IsNullOrEmpty(Name) ? "Unknown" : Name;
                Value = string.IsNullOrEmpty(Value) ? "Empty" : Value;
                Category = string.IsNullOrEmpty(Category) ? "Default" : Category;
            }
            catch
            {
                Name = "Error";
                Value = $"Failed to parse: {input}";
                Category = "Error";
            }
        }

        public override string ToString()
        {
            return $"{Name}:{Value}:{Category}";
        }
    }

    /// <summary>
    /// 암시적 변환 연산자가 정의된 테스트 타입
    /// </summary>
    public class ImplicitConversionType
    {
        public string Source { get; set; }
        public int Value { get; set; }

        // 기본 생성자
        public ImplicitConversionType()
        {
            Source = "Default";
            Value = 0;
        }

        // 암시적 변환 연산자: string -> ImplicitConversionType
        public static implicit operator ImplicitConversionType(string value)
        {
            return new ImplicitConversionType
            {
                Source = "String",
                Value = value.Length
            };
        }

        // 암시적 변환 연산자: int -> ImplicitConversionType
        public static implicit operator ImplicitConversionType(int value)
        {
            return new ImplicitConversionType
            {
                Source = "Integer",
                Value = value
            };
        }

        public override string ToString()
        {
            return $"[{Source}] {Value}";
        }
    }

    /// <summary>
    /// 명시적 변환 연산자가 정의된 테스트 타입
    /// </summary>
    public class ExplicitConversionType
    {
        public string Data { get; set; }
        public string Type { get; set; }

        // 기본 생성자
        public ExplicitConversionType()
        {
            Data = string.Empty;
            Type = "Unknown";
        }

        // 값을 초기화하는 생성자
        public ExplicitConversionType(string data, string type)
        {
            Data = data;
            Type = type;
        }

        // 명시적 변환 연산자: ExplicitConversionType -> string
        public static explicit operator string(ExplicitConversionType value)
        {
            return $"{value.Type}:{value.Data}";
        }

        // 명시적 변환 연산자: ExplicitConversionType -> int
        public static explicit operator int(ExplicitConversionType value)
        {
            if (int.TryParse(value.Data, out int result))
            {
                return result;
            }
            return 0;
        }

        public override string ToString()
        {
            return $"{Type}: {Data}";
        }
    }
}
