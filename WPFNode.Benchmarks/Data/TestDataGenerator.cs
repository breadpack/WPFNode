using Newtonsoft.Json;
using WPFNode.Demo.Models;
using WPFNode.Tests.Models;

namespace WPFNode.Benchmarks.Data;

/// <summary>
/// 벤치마크 테스트에 사용할 다양한 데이터를 생성하는 헬퍼 클래스
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random _random = new(42); // 일관된 결과를 위한 고정 시드

    /// <summary>
    /// 지정된 크기의 정수 배열을 생성합니다.
    /// </summary>
    public static int[] GenerateIntArray(int size)
    {
        return Enumerable.Range(1, size).ToArray();
    }

    /// <summary>
    /// 지정된 크기의 문자열 배열을 생성합니다.
    /// </summary>
    public static string[] GenerateStringArray(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => i.ToString())
            .ToArray();
    }

    /// <summary>
    /// 지정된 크기의 Employee 배열을 생성합니다.
    /// </summary>
    public static Employee[] GenerateEmployeeArray(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => new Employee
            {
                Id = i,
                Name = $"Employee-{i}",
                Department = GetRandomDepartment(),
                Salary = 3000 + (_random.Next(0, 5000))
            })
            .ToArray();
    }

    /// <summary>
    /// 지정된 크기의 Employee JSON 문자열 배열을 생성합니다.
    /// </summary>
    public static string[] GenerateEmployeeJsonArray(int size)
    {
        return GenerateEmployeeArray(size)
            .Select(emp => JsonConvert.SerializeObject(emp))
            .ToArray();
    }

    /// <summary>
    /// 지정된 크기의 StringConstructorType 테스트 문자열 배열을 생성합니다.
    /// </summary>
    public static string[] GenerateStringConstructorTestData(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => $"Name{i}:Value{i}:Category{i % 5}")
            .ToArray();
    }

    /// <summary>
    /// 지정된 크기의 ExplicitConversionType 배열을 생성합니다.
    /// </summary>
    public static ExplicitConversionType[] GenerateExplicitConversionArray(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => new ExplicitConversionType(
                i.ToString(), 
                i % 2 == 0 ? "Number" : "Text"))
            .ToArray();
    }

    /// <summary>
    /// 다양한 숫자 타입의 문자열 표현을 생성합니다.
    /// </summary>
    public static string[] GenerateNumericStringArray(int size)
    {
        var types = new Func<int, string>[]
        {
            i => i.ToString(),                    // int
            i => (i * 1.5).ToString("F2"),       // double
            i => (i * 0.1m).ToString("F4"),      // decimal
            i => ((float)(i * 2.3)).ToString(),  // float
            i => ((long)i * 1000).ToString()     // long
        };

        return Enumerable.Range(1, size)
            .Select(i => types[i % types.Length](i))
            .ToArray();
    }

    /// <summary>
    /// 복잡한 중첩 객체의 JSON 문자열을 생성합니다.
    /// </summary>
    public static string[] GenerateComplexJsonArray(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => JsonConvert.SerializeObject(new
            {
                Id = i,
                Employee = new Employee(i),
                Metadata = new
                {
                    CreatedAt = DateTime.Now.AddDays(-i),
                    Tags = new[] { $"tag{i}", $"category{i % 3}" },
                    Properties = new Dictionary<string, object>
                    {
                        ["priority"] = i % 5,
                        ["active"] = i % 2 == 0,
                        ["score"] = i * 0.1
                    }
                }
            }))
            .ToArray();
    }

    /// <summary>
    /// 성능 테스트용 대용량 문자열을 생성합니다.
    /// </summary>
    public static string[] GenerateLargeStringArray(int size, int stringLength = 1000)
    {
        return Enumerable.Range(1, size)
            .Select(i => new string('A', stringLength) + i.ToString())
            .ToArray();
    }

    /// <summary>
    /// 타입 변환 실패 케이스를 위한 잘못된 형식의 데이터를 생성합니다.
    /// </summary>
    public static string[] GenerateInvalidDataArray(int size)
    {
        var invalidFormats = new[]
        {
            "not-a-number",
            "{ invalid json",
            "",
            "null",
            "undefined",
            "NaN",
            "Infinity",
            "true/false",
            "2023-13-45", // 잘못된 날짜
            "{ \"incomplete\": "
        };

        return Enumerable.Range(0, size)
            .Select(i => invalidFormats[i % invalidFormats.Length])
            .ToArray();
    }

    /// <summary>
    /// 랜덤한 부서명을 반환합니다.
    /// </summary>
    private static string GetRandomDepartment()
    {
        var departments = new[]
        {
            "개발", "인사", "마케팅", "영업", "재무", 
            "운영", "기획", "디자인", "QA", "DevOps"
        };
        return departments[_random.Next(departments.Length)];
    }

    /// <summary>
    /// 메모리 사용량 테스트를 위한 대용량 객체 배열을 생성합니다.
    /// </summary>
    public static object[] GenerateMemoryTestObjects(int size)
    {
        return Enumerable.Range(1, size)
            .Select(i => new
            {
                Id = i,
                Data = new byte[1024], // 1KB 데이터
                Text = new string('X', 100),
                Numbers = Enumerable.Range(1, 10).ToArray(),
                Employee = new Employee(i)
            })
            .Cast<object>()
            .ToArray();
    }
} 

