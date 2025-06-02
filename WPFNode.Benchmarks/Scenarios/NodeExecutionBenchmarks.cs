using BenchmarkDotNet.Attributes;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Tests.Models;
using WPFNode.Benchmarks.Data;

namespace WPFNode.Benchmarks.Scenarios;

/// <summary>
/// Node 실행 시나리오 벤치마크
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class NodeExecutionBenchmarks
{
    [Params(100, 1000, 10000)]
    public int DataSize { get; set; }

    private int[] _intArray = Array.Empty<int>();
    private string[] _stringArray = Array.Empty<string>();
    private Employee[] _employeeArray = Array.Empty<Employee>();

    [GlobalSetup]
    public void Setup()
    {
        _intArray = TestDataGenerator.GenerateIntArray(DataSize);
        _stringArray = TestDataGenerator.GenerateStringArray(DataSize);
        _employeeArray = TestDataGenerator.GenerateEmployeeArray(DataSize);
    }

    /// <summary>
    /// 단순 타입 변환 노드 실행 시뮬레이션
    /// </summary>
    [Benchmark(Baseline = true)]
    public string[] SimpleTypeConversionNode()
    {
        var results = new string[DataSize];
        for (int i = 0; i < DataSize; i++)
        {
            // 노드 입력 처리 시뮬레이션
            var input = _intArray[i];
            
            // 타입 변환 수행
            if (((object)input).TryConvertTo<string>(out var result))
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
    /// 복잡한 타입 변환 노드 실행 시뮬레이션
    /// </summary>
    [Benchmark]
    public Employee[] ComplexTypeConversionNode()
    {
        var results = new Employee[DataSize];
        for (int i = 0; i < DataSize; i++)
        {
            // 노드 입력 처리 시뮬레이션
            var input = _intArray[i];
            
            // 복잡한 타입 변환 수행
            if (((object)input).TryConvertTo<Employee>(out var result))
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
    /// 체인 노드 실행 시뮬레이션 (int -> Employee -> string)
    /// </summary>
    [Benchmark]
    public string[] ChainNodeExecution()
    {
        var results = new string[DataSize];
        for (int i = 0; i < DataSize; i++)
        {
            // 첫 번째 노드: int -> Employee
            if (((object)_intArray[i]).TryConvertTo<Employee>(out var employee))
            {
                // 두 번째 노드: Employee -> string
                if (((object)employee).TryConvertTo<string>(out var result))
                {
                    results[i] = result;
                }
                else
                {
                    results[i] = "";
                }
            }
            else
            {
                results[i] = "";
            }
        }
        return results;
    }

    /// <summary>
    /// 조건부 노드 실행 시뮬레이션
    /// </summary>
    [Benchmark]
    public object[] ConditionalNodeExecution()
    {
        var results = new object[DataSize];
        for (int i = 0; i < DataSize; i++)
        {
            // 조건에 따른 다른 변환 수행
            if (i % 2 == 0)
            {
                // 짝수: int -> string
                if (((object)_intArray[i]).TryConvertTo<string>(out var stringResult))
                {
                    results[i] = stringResult;
                }
                else
                {
                    results[i] = "";
                }
            }
            else
            {
                // 홀수: int -> Employee
                if (((object)_intArray[i]).TryConvertTo<Employee>(out var employeeResult))
                {
                    results[i] = employeeResult;
                }
                else
                {
                    results[i] = new Employee();
                }
            }
        }
        return results;
    }

    /// <summary>
    /// 병렬 노드 실행 시뮬레이션
    /// </summary>
    [Benchmark]
    public Employee[] ParallelNodeExecution()
    {
        var results = new Employee[DataSize];
        
        Parallel.For(0, DataSize, i =>
        {
            // 병렬로 노드 실행
            if (((object)_intArray[i]).TryConvertTo<Employee>(out var result))
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
    /// 에러 처리가 포함된 노드 실행 시뮬레이션
    /// </summary>
    [Benchmark]
    public Employee[] ErrorHandlingNodeExecution()
    {
        var results = new Employee[DataSize];
        var invalidData = TestDataGenerator.GenerateInvalidDataArray(DataSize);
        
        for (int i = 0; i < DataSize; i++)
        {
            try
            {
                // 실패 가능성이 높은 변환 시도
                if (invalidData[i].TryConvertTo<Employee>(out var result))
                {
                    results[i] = result;
                }
                else
                {
                    // 기본값으로 폴백
                    results[i] = new Employee { Id = -1, Name = "Error", Department = "Unknown" };
                }
            }
            catch
            {
                // 예외 발생 시 기본값
                results[i] = new Employee { Id = -1, Name = "Exception", Department = "Error" };
            }
        }
        return results;
    }
} 