using BenchmarkDotNet.Attributes;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Tests.Models;
using WPFNode.Benchmarks.Data;

namespace WPFNode.Benchmarks.Scenarios;

/// <summary>
/// 배치 변환 시나리오 벤치마크
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class BatchConversionBenchmarks
{
    [Params(1000, 10000, 100000)]
    public int BatchSize { get; set; }

    private int[] _intArray = Array.Empty<int>();
    private string[] _stringArray = Array.Empty<string>();
    private string[] _employeeJsonArray = Array.Empty<string>();

    [GlobalSetup]
    public void Setup()
    {
        _intArray = TestDataGenerator.GenerateIntArray(BatchSize);
        _stringArray = TestDataGenerator.GenerateStringArray(BatchSize);
        _employeeJsonArray = TestDataGenerator.GenerateEmployeeJsonArray(BatchSize);
    }

    /// <summary>
    /// 순차 배치 변환 - int -> string
    /// </summary>
    [Benchmark(Baseline = true)]
    public string[] SequentialBatch_IntToString()
    {
        var results = new string[BatchSize];
        for (int i = 0; i < BatchSize; i++)
        {
            if (((object)_intArray[i]).TryConvertTo<string>(out var result))
            {
                results[i] = result;
            }
            else
            {
                results[i] = "";
            }
        }
        return results;
    }

    /// <summary>
    /// 병렬 배치 변환 - int -> string
    /// </summary>
    [Benchmark]
    public string[] ParallelBatch_IntToString()
    {
        var results = new string[BatchSize];
        
        Parallel.For(0, BatchSize, i =>
        {
            if (((object)_intArray[i]).TryConvertTo<string>(out var result))
            {
                results[i] = result;
            }
            else
            {
                results[i] = "";
            }
        });
        
        return results;
    }

    /// <summary>
    /// 순차 배치 변환 - string -> int
    /// </summary>
    [Benchmark]
    public int[] SequentialBatch_StringToInt()
    {
        var results = new int[BatchSize];
        for (int i = 0; i < BatchSize; i++)
        {
            if (_stringArray[i].TryConvertTo<int>(out var result))
            {
                results[i] = result;
            }
            else
            {
                results[i] = 0;
            }
        }
        return results;
    }

    /// <summary>
    /// 병렬 배치 변환 - string -> int
    /// </summary>
    [Benchmark]
    public int[] ParallelBatch_StringToInt()
    {
        var results = new int[BatchSize];
        
        Parallel.For(0, BatchSize, i =>
        {
            if (_stringArray[i].TryConvertTo<int>(out var result))
            {
                results[i] = result;
            }
            else
            {
                results[i] = 0;
            }
        });
        
        return results;
    }

    /// <summary>
    /// 순차 배치 변환 - JSON -> Employee
    /// </summary>
    [Benchmark]
    public Employee[] SequentialBatch_JsonToEmployee()
    {
        var results = new Employee[BatchSize];
        for (int i = 0; i < BatchSize; i++)
        {
            if (_employeeJsonArray[i].TryConvertTo<Employee>(out var result))
            {
                results[i] = result;
            }
            else
            {
                results[i] = new Employee();
            }
        }
        return results;
    }

    /// <summary>
    /// 병렬 배치 변환 - JSON -> Employee
    /// </summary>
    [Benchmark]
    public Employee[] ParallelBatch_JsonToEmployee()
    {
        var results = new Employee[BatchSize];
        
        Parallel.For(0, BatchSize, i =>
        {
            if (_employeeJsonArray[i].TryConvertTo<Employee>(out var result))
            {
                results[i] = result;
            }
            else
            {
                results[i] = new Employee();
            }
        });
        
        return results;
    }

    /// <summary>
    /// 청크 단위 배치 변환 (메모리 효율성)
    /// </summary>
    [Benchmark]
    public Employee[] ChunkedBatch_JsonToEmployee()
    {
        var results = new Employee[BatchSize];
        const int chunkSize = 1000;
        
        for (int chunkStart = 0; chunkStart < BatchSize; chunkStart += chunkSize)
        {
            var chunkEnd = Math.Min(chunkStart + chunkSize, BatchSize);
            
            Parallel.For(chunkStart, chunkEnd, i =>
            {
                if (_employeeJsonArray[i].TryConvertTo<Employee>(out var result))
                {
                    results[i] = result;
                }
                else
                {
                    results[i] = new Employee();
                }
            });
            
            // 청크 간 GC 수행 (메모리 관리)
            if (chunkStart % (chunkSize * 10) == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        
        return results;
    }

    /// <summary>
    /// 스트리밍 배치 변환 (메모리 절약)
    /// </summary>
    [Benchmark]
    public int StreamingBatch_Processing()
    {
        var successCount = 0;
        
        for (int i = 0; i < BatchSize; i++)
        {
            // 즉시 처리하고 결과를 누적만 함 (메모리 절약)
            if (((object)_intArray[i]).TryConvertTo<string>(out var stringResult))
            {
                if (stringResult.TryConvertTo<int>(out var intResult))
                {
                    successCount += intResult > 0 ? 1 : 0;
                }
            }
        }
        
        return successCount;
    }

    /// <summary>
    /// 실패 케이스가 포함된 배치 변환
    /// </summary>
    [Benchmark]
    public Employee[] FailureIncludedBatch()
    {
        var results = new Employee[BatchSize];
        var invalidData = TestDataGenerator.GenerateInvalidDataArray(BatchSize);
        
        for (int i = 0; i < BatchSize; i++)
        {
            if (invalidData[i].TryConvertTo<Employee>(out var result))
            {
                results[i] = result;
            }
            else
            {
                // 실패 시 기본값
                results[i] = new Employee { Id = -1, Name = "Failed", Department = "Error" };
            }
        }
        return results;
    }

    /// <summary>
    /// 혼합 타입 배치 변환
    /// </summary>
    [Benchmark]
    public object[] MixedTypeBatch()
    {
        var results = new object[BatchSize];
        
        for (int i = 0; i < BatchSize; i++)
        {
            switch (i % 4)
            {
                case 0:
                    // int -> string
                    if (((object)_intArray[i]).TryConvertTo<string>(out var stringResult))
                    {
                        results[i] = stringResult;
                    }
                    else
                    {
                        results[i] = "";
                    }
                    break;
                case 1:
                    // string -> int
                    if (_stringArray[i].TryConvertTo<int>(out var intResult))
                    {
                        results[i] = intResult;
                    }
                    else
                    {
                        results[i] = 0;
                    }
                    break;
                case 2:
                    // int -> Employee
                    if (((object)_intArray[i]).TryConvertTo<Employee>(out var employeeResult))
                    {
                        results[i] = employeeResult;
                    }
                    else
                    {
                        results[i] = new Employee();
                    }
                    break;
                case 3:
                    // JSON -> Employee
                    if (_employeeJsonArray[i].TryConvertTo<Employee>(out var jsonEmployeeResult))
                    {
                        results[i] = jsonEmployeeResult;
                    }
                    else
                    {
                        results[i] = new Employee();
                    }
                    break;
            }
        }
        return results;
    }
} 