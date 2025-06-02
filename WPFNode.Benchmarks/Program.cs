using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using WPFNode.Benchmarks.Core;
using WPFNode.Benchmarks.Scenarios;

namespace WPFNode.Benchmarks;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("🚀 WPFNode 타입 변환 성능 벤치마크");
        Console.WriteLine("=====================================");

        if (args.Length == 0)
        {
            RunAllBenchmarks();
            ShowMenu();
            return;
        }

        // 명령줄 인수에 따른 벤치마크 실행
        switch (args[0].ToLower())
        {
            default:
            case "all":
                RunAllBenchmarks();
                break;
            case "core":
                RunCoreBenchmarks();
                break;
            case "scenarios":
                RunScenarioBenchmarks();
                break;
            case "comparisons":
                RunComparisonBenchmarks();
                break;
            case "quick":
                RunQuickBenchmarks();
                break;
        }
    }

    private static void ShowMenu()
    {
        Console.WriteLine("사용법: dotnet run [옵션]");
        Console.WriteLine();
        Console.WriteLine("옵션:");
        Console.WriteLine("  all         - 모든 벤치마크 실행");
        Console.WriteLine("  core        - 핵심 타입 변환 벤치마크");
        Console.WriteLine("  scenarios   - 실제 사용 시나리오 벤치마크");
        Console.WriteLine("  comparisons - 변환 방법별 성능 비교");
        Console.WriteLine("  quick       - 빠른 테스트용 벤치마크");
        Console.WriteLine();
        Console.WriteLine("예시:");
        Console.WriteLine("  dotnet run core");
        Console.WriteLine("  dotnet run -- --filter \"*TypeCheck*\"");
    }

    private static void RunAllBenchmarks()
    {
        Console.WriteLine("📊 모든 벤치마크를 실행합니다...");
        
        var config = DefaultConfig.Instance
            .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args: null, config);
    }

    private static void RunCoreBenchmarks()
    {
        Console.WriteLine("🔍 핵심 타입 변환 벤치마크를 실행합니다...");
        
        BenchmarkRunner.Run<TypeCheckBenchmarks>();
        BenchmarkRunner.Run<ValueConversionBenchmarks>();
        BenchmarkRunner.Run<MemoryBenchmarks>();
    }

    private static void RunScenarioBenchmarks()
    {
        Console.WriteLine("🎯 실제 사용 시나리오 벤치마크를 실행합니다...");
        
        BenchmarkRunner.Run<NodeExecutionBenchmarks>();
        BenchmarkRunner.Run<BatchConversionBenchmarks>();
    }

    private static void RunComparisonBenchmarks()
    {
        Console.WriteLine("⚖️ 변환 방법별 성능 비교 벤치마크를 실행합니다...");
        
        BenchmarkRunner.Run<WPFNode.Benchmarks.Comparisons.ConversionMethodBenchmarks>();
    }

    private static void RunQuickBenchmarks()
    {
        Console.WriteLine("⚡ 빠른 테스트용 벤치마크를 실행합니다...");
        
        var config = DefaultConfig.Instance
            .AddJob(Job.Dry); // 빠른 실행을 위한 Dry 모드

        BenchmarkRunner.Run<TypeCheckBenchmarks>(config);
    }
}

