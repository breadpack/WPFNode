using BenchmarkDotNet.Attributes;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Tests.Models;
using WPFNode.Benchmarks.Data;

namespace WPFNode.Benchmarks.Core;

/// <summary>
/// 메모리 사용량 측정 벤치마크
/// </summary>
[MemoryDiagnoser]
[GcServer(true)]
public class MemoryBenchmarks
{
    [Params(100, 1000, 10000)]
    public int ObjectCount { get; set; }

    private int[] _intArray = Array.Empty<int>();
    private string[] _stringArray = Array.Empty<string>();
    private string[] _jsonArray = Array.Empty<string>();

    [GlobalSetup]
    public void Setup()
    {
        _intArray = TestDataGenerator.GenerateIntArray(ObjectCount);
        _stringArray = TestDataGenerator.GenerateStringArray(ObjectCount);
        _jsonArray = TestDataGenerator.GenerateEmployeeJsonArray(ObjectCount);
    }

    /// <summary>
    /// 타입 검사 메모리 사용량
    /// </summary>
    [Benchmark]
    public bool[] TypeCheckMemoryUsage()
    {
        var results = new bool[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = typeof(int).CanConvertTo(typeof(string));
        }
        return results;
    }

    /// <summary>
    /// 기본 타입 변환 메모리 사용량
    /// </summary>
    [Benchmark]
    public string[] BasicConversionMemoryUsage()
    {
        var results = new string[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = _intArray[i].TryConvertTo<string>(out var result) ? result ?? "" : "";
        }
        return results;
    }

    /// <summary>
    /// 커스텀 타입 변환 메모리 사용량 - int -> Employee
    /// </summary>
    [Benchmark]
    public Employee?[] CustomConversionMemoryUsage()
    {
        var results = new Employee?[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = ((object)_intArray[i]).TryConvertTo<Employee>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// JSON 변환 메모리 사용량 - string -> Employee
    /// </summary>
    [Benchmark]
    public Employee?[] JsonConversionMemoryUsage()
    {
        var results = new Employee?[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = _jsonArray[i].TryConvertTo<Employee>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// 대량 객체 생성 및 변환 메모리 사용량
    /// </summary>
    [Benchmark]
    public object[] LargeObjectConversionMemoryUsage()
    {
        var results = new object[ObjectCount];
        for (int i = 0; i < ObjectCount; i++)
        {
            var largeObject = new
            {
                Id = i,
                Data = new byte[1024], // 1KB 데이터
                Text = new string('X', 100),
                Numbers = Enumerable.Range(1, 10).ToArray(),
                Employee = new Employee(i)
            };
            results[i] = ((object)largeObject).TryConvertTo<string>(out var result) ? result ?? "" : "";
        }
        return results;
    }

    /// <summary>
    /// 실패 케이스 메모리 사용량 - 변환 실패 시 메모리 누수 확인
    /// </summary>
    [Benchmark]
    public Employee?[] FailureConversionMemoryUsage()
    {
        var results = new Employee?[ObjectCount];
        var invalidData = TestDataGenerator.GenerateInvalidDataArray(ObjectCount);
        
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = invalidData[i].TryConvertTo<Employee>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// 문자열 생성자 타입 변환 메모리 사용량
    /// </summary>
    [Benchmark]
    public StringConstructorType?[] StringConstructorMemoryUsage()
    {
        var results = new StringConstructorType?[ObjectCount];
        var testData = TestDataGenerator.GenerateStringConstructorTestData(ObjectCount);
        
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = testData[i].TryConvertTo<StringConstructorType>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// 암시적 변환 연산자 메모리 사용량
    /// </summary>
    [Benchmark]
    public ImplicitConversionType?[] ImplicitConversionMemoryUsage()
    {
        var results = new ImplicitConversionType?[ObjectCount];
        
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = ((object)_stringArray[i]).TryConvertTo<ImplicitConversionType>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// 명시적 변환 연산자 메모리 사용량
    /// </summary>
    [Benchmark]
    public string[] ExplicitConversionMemoryUsage()
    {
        var results = new string[ObjectCount];
        var explicitTypes = TestDataGenerator.GenerateExplicitConversionArray(ObjectCount);
        
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = ((object)explicitTypes[i]).TryConvertTo<string>(out var result) ? result ?? "" : "";
        }
        return results;
    }

    /// <summary>
    /// 복잡한 중첩 객체 변환 메모리 사용량
    /// </summary>
    [Benchmark]
    public Employee?[] ComplexObjectMemoryUsage()
    {
        var results = new Employee?[ObjectCount];
        var complexJsonArray = TestDataGenerator.GenerateComplexJsonArray(ObjectCount);
        
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = complexJsonArray[i].TryConvertTo<Employee>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// 대용량 문자열 변환 메모리 사용량
    /// </summary>
    [Benchmark]
    public string[] LargeStringMemoryUsage()
    {
        var results = new string[ObjectCount];
        
        for (int i = 0; i < ObjectCount; i++)
        {
            var employee = new Employee(i);
            results[i] = ((object)employee).TryConvertTo<string>(out var result) ? result ?? "" : "";
        }
        return results;
    }

    /// <summary>
    /// 메모리 재사용 패턴 테스트 - 동일한 변환을 반복 수행
    /// </summary>
    [Benchmark]
    public Employee?[] MemoryReusePattern()
    {
        var results = new Employee?[ObjectCount];
        
        // 동일한 값으로 반복 변환하여 캐싱/재사용 효과 측정
        for (int i = 0; i < ObjectCount; i++)
        {
            results[i] = ((object)1001).TryConvertTo<Employee>(out var result) ? result : null;
        }
        return results;
    }

    /// <summary>
    /// GC 압박 테스트 - 많은 임시 객체 생성
    /// </summary>
    [Benchmark]
    public int GcPressureTest()
    {
        var count = 0;
        
        for (int i = 0; i < ObjectCount; i++)
        {
            // 임시 객체들을 많이 생성하여 GC 압박 유발
            var tempEmployee = new Employee(i);
            var tempJson = ((object)tempEmployee).TryConvertTo<string>(out var result1) ? result1 : null;
            var tempEmployee2 = tempJson != null && tempJson.TryConvertTo<Employee>(out var result2) ? result2 : null;
            
            if (tempEmployee2 != null)
            {
                count++;
            }
        }
        
        return count;
    }
} 