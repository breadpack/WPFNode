using BenchmarkDotNet.Attributes;
using WPFNode.Utilities;
using WPFNode.Demo.Models;

namespace WPFNode.Benchmarks.Core;

/// <summary>
/// 타입 검사 성능 벤치마크
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class TypeCheckBenchmarks
{
    /// <summary>
    /// 기본 타입 호환성 검사 - int -> string
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool IntToString_CanConvert()
    {
        return typeof(int).CanConvertTo(typeof(string));
    }

    /// <summary>
    /// 기본 타입 호환성 검사 - string -> int
    /// </summary>
    [Benchmark]
    public bool StringToInt_CanConvert()
    {
        return typeof(string).CanConvertTo(typeof(int));
    }

    /// <summary>
    /// 기본 타입 호환성 검사 - string -> double
    /// </summary>
    [Benchmark]
    public bool StringToDouble_CanConvert()
    {
        return typeof(string).CanConvertTo(typeof(double));
    }

    /// <summary>
    /// 커스텀 타입 검사 - int -> Employee
    /// </summary>
    [Benchmark]
    public bool IntToEmployee_CanConvert()
    {
        return typeof(int).CanConvertTo(typeof(Employee));
    }

    /// <summary>
    /// 커스텀 타입 검사 - string -> Employee
    /// </summary>
    [Benchmark]
    public bool StringToEmployee_CanConvert()
    {
        return typeof(string).CanConvertTo(typeof(Employee));
    }

    /// <summary>
    /// 실패 케이스 - 변환 불가능한 타입
    /// </summary>
    [Benchmark]
    public bool FailureCase_CanConvert()
    {
        return typeof(DateTime).CanConvertTo(typeof(Employee));
    }
} 