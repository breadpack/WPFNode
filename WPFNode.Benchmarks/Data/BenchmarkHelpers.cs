using System.ComponentModel;
using System.Reflection;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Tests.Models;

namespace WPFNode.Benchmarks.Data;

/// <summary>
/// 벤치마크에서 공통으로 사용하는 헬퍼 메서드들
/// </summary>
public static class BenchmarkHelpers
{
    /// <summary>
    /// 타입 변환 가능성을 확인하는 다양한 방법들
    /// </summary>
    public static class TypeCheckMethods
    {
        public static bool CanConvertTo_Extension(Type sourceType, Type targetType)
            => sourceType.CanConvertTo(targetType);

        public static bool CanConvertTo_IsAssignable(Type sourceType, Type targetType)
            => targetType.IsAssignableFrom(sourceType);

        public static bool CanConvertTo_TypeConverter(Type sourceType, Type targetType)
        {
            var converter = TypeDescriptor.GetConverter(targetType);
            return converter.CanConvertFrom(sourceType);
        }

        public static bool CanConvertTo_ImplicitOperator(Type sourceType, Type targetType)
        {
            var implicitOp = targetType.GetMethod("op_Implicit",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { sourceType },
                null);
            return implicitOp != null;
        }

        public static bool CanConvertTo_Constructor(Type sourceType, Type targetType)
        {
            var constructor = targetType.GetConstructor(new[] { sourceType });
            return constructor != null;
        }
    }

    /// <summary>
    /// 값 변환을 수행하는 다양한 방법들
    /// </summary>
    public static class ConversionMethods
    {
        public static T? TryConvertTo_Extension<T>(object? sourceValue)
        {
            return sourceValue.TryConvertTo<T>(out var result) ? result : default(T);
        }

        public static object? TryConvertTo_TypeConverter(object sourceValue, Type targetType)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                if (converter.CanConvertFrom(sourceValue.GetType()))
                {
                    return converter.ConvertFrom(sourceValue);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static object? TryConvertTo_ChangeType(object sourceValue, Type targetType)
        {
            try
            {
                return Convert.ChangeType(sourceValue, targetType);
            }
            catch
            {
                return null;
            }
        }

        public static T? TryConvertTo_Parse<T>(string sourceValue) where T : struct
        {
            try
            {
                var parseMethod = typeof(T).GetMethod("Parse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string) },
                    null);

                if (parseMethod != null)
                {
                    return (T)parseMethod.Invoke(null, new object[] { sourceValue })!;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static T? TryConvertTo_TryParse<T>(string sourceValue) where T : struct
        {
            try
            {
                var tryParseMethod = typeof(T).GetMethod("TryParse",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(string), typeof(T).MakeByRefType() },
                    null);

                if (tryParseMethod != null)
                {
                    var parameters = new object[] { sourceValue, default(T)! };
                    var success = (bool)tryParseMethod.Invoke(null, parameters)!;
                    if (success)
                    {
                        return (T)parameters[1];
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 성능 측정을 위한 유틸리티 메서드들
    /// </summary>
    public static class PerformanceUtils
    {
        /// <summary>
        /// 지정된 작업을 여러 번 실행하여 평균 시간을 측정합니다.
        /// </summary>
        public static TimeSpan MeasureAverageTime(Action action, int iterations = 1000)
        {
            // 워밍업
            for (int i = 0; i < 10; i++)
            {
                action();
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                action();
            }
            stopwatch.Stop();

            return TimeSpan.FromTicks(stopwatch.ElapsedTicks / iterations);
        }

        /// <summary>
        /// 메모리 사용량을 측정합니다.
        /// </summary>
        public static long MeasureMemoryUsage(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var beforeMemory = GC.GetTotalMemory(false);
            action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var afterMemory = GC.GetTotalMemory(false);
            return afterMemory - beforeMemory;
        }

        /// <summary>
        /// 처리량(초당 작업 수)을 계산합니다.
        /// </summary>
        public static double CalculateThroughput(int operationCount, TimeSpan elapsedTime)
        {
            return operationCount / elapsedTime.TotalSeconds;
        }
    }

    /// <summary>
    /// 테스트 데이터 검증을 위한 메서드들
    /// </summary>
    public static class ValidationHelpers
    {
        public static bool IsValidEmployee(Employee? employee)
        {
            return employee != null &&
                   employee.Id > 0 &&
                   !string.IsNullOrEmpty(employee.Name) &&
                   !string.IsNullOrEmpty(employee.Department) &&
                   employee.Salary >= 0;
        }

        public static bool IsValidStringConstructorType(StringConstructorType? obj)
        {
            return obj != null &&
                   !string.IsNullOrEmpty(obj.Name) &&
                   !string.IsNullOrEmpty(obj.Value) &&
                   !string.IsNullOrEmpty(obj.Category);
        }

        public static bool IsValidImplicitConversionType(ImplicitConversionType? obj)
        {
            return obj != null &&
                   !string.IsNullOrEmpty(obj.Source) &&
                   obj.Value >= 0;
        }

        public static bool IsValidExplicitConversionType(ExplicitConversionType? obj)
        {
            return obj != null &&
                   !string.IsNullOrEmpty(obj.Data) &&
                   !string.IsNullOrEmpty(obj.Type);
        }
    }

    /// <summary>
    /// 벤치마크 결과 분석을 위한 통계 메서드들
    /// </summary>
    public static class StatisticsHelpers
    {
        public static double CalculateAverage(IEnumerable<double> values)
        {
            return values.Average();
        }

        public static double CalculateMedian(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(x => x).ToArray();
            var count = sorted.Length;
            
            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            else
            {
                return sorted[count / 2];
            }
        }

        public static double CalculatePercentile(IEnumerable<double> values, double percentile)
        {
            var sorted = values.OrderBy(x => x).ToArray();
            var index = (percentile / 100.0) * (sorted.Length - 1);
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);
            
            if (lower == upper)
            {
                return sorted[lower];
            }
            
            var weight = index - lower;
            return sorted[lower] * (1 - weight) + sorted[upper] * weight;
        }

        public static double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var avg = values.Average();
            var sumOfSquares = values.Sum(x => Math.Pow(x - avg, 2));
            return Math.Sqrt(sumOfSquares / values.Count());
        }
    }
} 

