using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using WPFNode.Utilities;
using WPFNode.Demo.Models;

namespace WPFNode.Benchmarks.Benchmarks.Core;

/// <summary>
/// 타입 검사 성능 벤치마크
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net60)]
[RankColumn]
public class TypeCheckBenchmarks
{
    /// <summary>
    /// 기본 타입 호환성 검사 - int -> string
    /// </summary>
    [Benchmark(Baseline = true)]
    public bool IntToString_CanConvert()
    {
        return ((object)12345).CanConvertTo<string>();
    }

    /// <summary>
    /// 기본 타입 호환성 검사 - string -> int
    /// </summary>
    [Benchmark]
    public bool StringToInt_CanConvert()
    {
        return "12345".CanConvertTo<int>();
    }

    /// <summary>
    /// 기본 타입 호환성 검사 - string -> double
    /// </summary>
    [Benchmark]
    public bool StringToDouble_CanConvert()
    {
        return "123.45".CanConvertTo<double>();
    }

    /// <summary>
    /// 커스텀 타입 검사 - int -> Employee
    /// </summary>
    [Benchmark]
    public bool IntToEmployee_CanConvert()
    {
        return ((object)1001).CanConvertTo<Employee>();
    }

    /// <summary>
    /// 커스텀 타입 검사 - string -> Employee
    /// </summary>
    [Benchmark]
    public bool StringToEmployee_CanConvert()
    {
        return "{\"Id\":1001,\"Name\":\"Test\"}".CanConvertTo<Employee>();
    }

    /// <summary>
    /// 실패 케이스 - 변환 불가능한 타입
    /// </summary>
    [Benchmark]
    public bool FailureCase_CanConvert()
    {
        return ((object)DateTime.Now).CanConvertTo<Employee>();
    }
} 