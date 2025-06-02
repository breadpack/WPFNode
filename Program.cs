using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using WPFNode.Benchmarks.Benchmarks.Core;

namespace WPFNode.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        var config = DefaultConfig.Instance
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

        if (args.Length == 0)
        {
            Console.WriteLine("WPFNode 타입 변환 벤치마크");
            Console.WriteLine("사용법: dotnet run [옵션]");
            Console.WriteLine();
            Console.WriteLine("옵션:");
            Console.WriteLine("  all        - 모든 벤치마크 실행");
            Console.WriteLine("  core       - 핵심 벤치마크만 실행");
            Console.WriteLine("  typecheck  - 타입 검사 벤치마크");
            Console.WriteLine("  conversion - 값 변환 벤치마크");
            Console.WriteLine();
            Console.WriteLine("기본값으로 핵심 벤치마크를 실행합니다...");
            
            BenchmarkRunner.Run<TypeCheckBenchmarks>(config);
            BenchmarkRunner.Run<ValueConversionBenchmarks>(config);
            return;
        }

        switch (args[0].ToLower())
        {
            case "all":
            case "core":
                BenchmarkRunner.Run<TypeCheckBenchmarks>(config);
                BenchmarkRunner.Run<ValueConversionBenchmarks>(config);
                break;
            case "typecheck":
                BenchmarkRunner.Run<TypeCheckBenchmarks>(config);
                break;
            case "conversion":
                BenchmarkRunner.Run<ValueConversionBenchmarks>(config);
                break;
            default:
                Console.WriteLine($"알 수 없는 옵션: {args[0]}");
                Console.WriteLine("사용 가능한 옵션: all, core, typecheck, conversion");
                break;
        }
    }
} 