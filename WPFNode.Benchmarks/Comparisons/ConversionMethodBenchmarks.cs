using BenchmarkDotNet.Attributes;
using System.ComponentModel;
using WPFNode.Utilities;
using WPFNode.Benchmarks.Data;

namespace WPFNode.Benchmarks.Comparisons;

/// <summary>
/// 변환 방법별 성능 비교 벤치마크
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ConversionMethodBenchmarks
{
    private readonly string _numberString = "42.5";
    private readonly string _intString = "12345";
    private readonly string _boolString = "true";
    private readonly string _dateString = "2023-01-01T12:00:00";

    /// <summary>
    /// double 변환 방법별 성능 비교 - Parse (기준)
    /// </summary>
    [Benchmark(Baseline = true)]
    public double Double_DirectParse() => double.Parse(_numberString);

    /// <summary>
    /// double 변환 방법별 성능 비교 - TryParse
    /// </summary>
    [Benchmark]
    public bool Double_TryParseMethod() => double.TryParse(_numberString, out _);

    /// <summary>
    /// double 변환 방법별 성능 비교 - TypeConverter
    /// </summary>
    [Benchmark]
    public double Double_TypeConverterMethod()
        => (double)TypeDescriptor.GetConverter(typeof(double))
            .ConvertFromString(_numberString)!;

    /// <summary>
    /// double 변환 방법별 성능 비교 - Convert.ChangeType
    /// </summary>
    [Benchmark]
    public double Double_ConvertChangeType()
        => (double)Convert.ChangeType(_numberString, typeof(double));

    /// <summary>
    /// double 변환 방법별 성능 비교 - Extension Method
    /// </summary>
    [Benchmark]
    public double Double_ExtensionMethod()
    {
        if (_numberString.TryConvertTo<double>(out var result))
        {
            return result;
        }
        return 0.0;
    }

    /// <summary>
    /// int 변환 방법별 성능 비교 - Parse (기준)
    /// </summary>
    [Benchmark]
    public int Int_DirectParse() => int.Parse(_intString);

    /// <summary>
    /// int 변환 방법별 성능 비교 - TryParse
    /// </summary>
    [Benchmark]
    public bool Int_TryParseMethod() => int.TryParse(_intString, out _);

    /// <summary>
    /// int 변환 방법별 성능 비교 - TypeConverter
    /// </summary>
    [Benchmark]
    public int Int_TypeConverterMethod()
        => (int)TypeDescriptor.GetConverter(typeof(int))
            .ConvertFromString(_intString)!;

    /// <summary>
    /// int 변환 방법별 성능 비교 - Convert.ChangeType
    /// </summary>
    [Benchmark]
    public int Int_ConvertChangeType()
        => (int)Convert.ChangeType(_intString, typeof(int));

    /// <summary>
    /// int 변환 방법별 성능 비교 - Extension Method
    /// </summary>
    [Benchmark]
    public int Int_ExtensionMethod()
    {
        if (_intString.TryConvertTo<int>(out var result))
        {
            return result;
        }
        return 0;
    }

    /// <summary>
    /// bool 변환 방법별 성능 비교 - Parse (기준)
    /// </summary>
    [Benchmark]
    public bool Bool_DirectParse() => bool.Parse(_boolString);

    /// <summary>
    /// bool 변환 방법별 성능 비교 - TryParse
    /// </summary>
    [Benchmark]
    public bool Bool_TryParseMethod() => bool.TryParse(_boolString, out _);

    /// <summary>
    /// bool 변환 방법별 성능 비교 - TypeConverter
    /// </summary>
    [Benchmark]
    public bool Bool_TypeConverterMethod()
        => (bool)TypeDescriptor.GetConverter(typeof(bool))
            .ConvertFromString(_boolString)!;

    /// <summary>
    /// bool 변환 방법별 성능 비교 - Convert.ChangeType
    /// </summary>
    [Benchmark]
    public bool Bool_ConvertChangeType()
        => (bool)Convert.ChangeType(_boolString, typeof(bool));

    /// <summary>
    /// bool 변환 방법별 성능 비교 - Extension Method
    /// </summary>
    [Benchmark]
    public bool Bool_ExtensionMethod()
    {
        if (_boolString.TryConvertTo<bool>(out var result))
        {
            return result;
        }
        return false;
    }

    /// <summary>
    /// DateTime 변환 방법별 성능 비교 - Parse (기준)
    /// </summary>
    [Benchmark]
    public DateTime DateTime_DirectParse() => DateTime.Parse(_dateString);

    /// <summary>
    /// DateTime 변환 방법별 성능 비교 - TryParse
    /// </summary>
    [Benchmark]
    public bool DateTime_TryParseMethod() => DateTime.TryParse(_dateString, out _);

    /// <summary>
    /// DateTime 변환 방법별 성능 비교 - TypeConverter
    /// </summary>
    [Benchmark]
    public DateTime DateTime_TypeConverterMethod()
        => (DateTime)TypeDescriptor.GetConverter(typeof(DateTime))
            .ConvertFromString(_dateString)!;

    /// <summary>
    /// DateTime 변환 방법별 성능 비교 - Convert.ChangeType
    /// </summary>
    [Benchmark]
    public DateTime DateTime_ConvertChangeType()
        => (DateTime)Convert.ChangeType(_dateString, typeof(DateTime));

    /// <summary>
    /// DateTime 변환 방법별 성능 비교 - Extension Method
    /// </summary>
    [Benchmark]
    public DateTime DateTime_ExtensionMethod()
    {
        if (_dateString.TryConvertTo<DateTime>(out var result))
        {
            return result;
        }
        return DateTime.MinValue;
    }

    /// <summary>
    /// 실패 케이스 처리 성능 비교 - TryParse (안전)
    /// </summary>
    [Benchmark]
    public bool FailureCase_TryParse()
    {
        return int.TryParse("invalid-number", out _);
    }

    /// <summary>
    /// 실패 케이스 처리 성능 비교 - Extension Method (안전)
    /// </summary>
    [Benchmark]
    public int FailureCase_ExtensionMethod()
    {
        if ("invalid-number".TryConvertTo<int>(out var result))
        {
            return result;
        }
        return 0;
    }

    /// <summary>
    /// 실패 케이스 처리 성능 비교 - TypeConverter (예외 처리)
    /// </summary>
    [Benchmark]
    public int FailureCase_TypeConverter()
    {
        try
        {
            return (int)TypeDescriptor.GetConverter(typeof(int))
                .ConvertFromString("invalid-number")!;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// 실패 케이스 처리 성능 비교 - Convert.ChangeType (예외 처리)
    /// </summary>
    [Benchmark]
    public int FailureCase_ConvertChangeType()
    {
        try
        {
            return (int)Convert.ChangeType("invalid-number", typeof(int));
        }
        catch
        {
            return 0;
        }
    }
} 