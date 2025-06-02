using BenchmarkDotNet.Attributes;
using WPFNode.Utilities;
using WPFNode.Demo.Models;

namespace WPFNode.Benchmarks.Core;

/// <summary>
/// 값 변환 성능 벤치마크
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ValueConversionBenchmarks
{
    private readonly string _intString = "12345";
    private readonly string _doubleString = "123.45";
    private readonly string _employeeJson = "{\"Id\":1001,\"Name\":\"Test\",\"Department\":\"Dev\"}";

    /// <summary>
    /// 기본 타입 변환 - int -> string
    /// </summary>
    [Benchmark(Baseline = true)]
    public string? IntToString_Conversion()
    {
        return ((object)12345).TryConvertTo<string>(out var result) ? result : null;
    }

    /// <summary>
    /// 기본 타입 변환 - string -> int
    /// </summary>
    [Benchmark]
    public int StringToInt_Conversion()
    {
        return _intString.TryConvertTo<int>(out var result) ? result : 0;
    }

    /// <summary>
    /// 기본 타입 변환 - string -> double
    /// </summary>
    [Benchmark]
    public double StringToDouble_Conversion()
    {
        return _doubleString.TryConvertTo<double>(out var result) ? result : 0.0;
    }

    /// <summary>
    /// 커스텀 타입 변환 - int -> Employee
    /// </summary>
    [Benchmark]
    public Employee? IntToEmployee_Conversion()
    {
        return ((object)1001).TryConvertTo<Employee>(out var result) ? result : null;
    }

    /// <summary>
    /// 커스텀 타입 변환 - string -> Employee
    /// </summary>
    [Benchmark]
    public Employee? StringToEmployee_Conversion()
    {
        return _employeeJson.TryConvertTo<Employee>(out var result) ? result : null;
    }

    /// <summary>
    /// 실패 케이스 - 잘못된 형식
    /// </summary>
    [Benchmark]
    public int FailureCase_InvalidFormat()
    {
        return "not-a-number".TryConvertTo<int>(out var result) ? result : 0;
    }
} 