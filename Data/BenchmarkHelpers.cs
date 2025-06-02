using System.ComponentModel;
using WPFNode.Utilities;
using WPFNode.Demo.Models;
using WPFNode.Tests.Models;
using Newtonsoft.Json;

namespace WPFNode.Benchmarks.Data;

/// <summary>
/// 벤치마크에서 사용하는 공통 헬퍼 메서드들
/// </summary>
public static class BenchmarkHelpers
{
    /// <summary>
    /// 타입 검사를 수행하는 다양한 방법들
    /// </summary>
    public static class TypeCheckMethods
    {
        public static bool CanConvertTo_Extension<T>(object? sourceValue)
            => sourceValue.CanConvertTo<T>();

        public static bool CanConvertTo_IsAssignable(object? sourceValue, Type targetType)
            => sourceValue?.GetType().IsAssignableTo(targetType) ?? false;

        public static bool CanConvertTo_TypeConverter(object? sourceValue, Type targetType)
        {
            if (sourceValue == null) return false;
            var converter = TypeDescriptor.GetConverter(targetType);
            return converter.CanConvertFrom(sourceValue.GetType());
        }

        public static bool CanConvertTo_ImplicitOperator(object? sourceValue, Type targetType)
        {
            if (sourceValue == null) return false;
            var sourceType = sourceValue.GetType();
            var method = targetType.GetMethod("op_Implicit", new[] { sourceType });
            return method != null;
        }

        public static bool CanConvertTo_Constructor(object? sourceValue, Type targetType)
        {
            if (sourceValue == null) return false;
            var sourceType = sourceValue.GetType();
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

        public static object? TryConvertTo_TypeConverter(object? sourceValue, Type targetType)
        {
            if (sourceValue == null) return null;
            try
            {
                var converter = TypeDescriptor.GetConverter(targetType);
                return converter.ConvertFrom(sourceValue);
            }
            catch
            {
                return null;
            }
        }

        public static object? TryConvertTo_ChangeType(object? sourceValue, Type targetType)
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

        public static T? TryConvertTo_Parse<T>(string? value) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return null;
            try
            {
                var parseMethod = typeof(T).GetMethod("Parse", new[] { typeof(string) });
                return (T?)parseMethod?.Invoke(null, new object[] { value });
            }
            catch
            {
                return null;
            }
        }

        public static T? TryConvertTo_TryParse<T>(string? value) where T : struct
        {
            if (string.IsNullOrEmpty(value)) return null;
            try
            {
                var tryParseMethod = typeof(T).GetMethod("TryParse", new[] { typeof(string), typeof(T).MakeByRefType() });
                if (tryParseMethod != null)
                {
                    var parameters = new object[] { value, default(T)! };
                    var success = (bool)tryParseMethod.Invoke(null, parameters)!;
                    return success ? (T)parameters[1] : null;
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }
    }

    /// <summary>
    /// 성능 측정 유틸리티
    /// </summary>
    public static class PerformanceUtils
    {
        public static TimeSpan MeasureTime(Action action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        public static (TimeSpan Time, T Result) MeasureTime<T>(Func<T> func)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = func();
            stopwatch.Stop();
            return (stopwatch.Elapsed, result);
        }

        public static long MeasureMemory(Action action)
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

        public static double CalculateThroughput(int itemCount, TimeSpan elapsed)
            => itemCount / elapsed.TotalSeconds;
    }

    /// <summary>
    /// 검증 헬퍼
    /// </summary>
    public static class ValidationHelpers
    {
        public static bool ValidateConversion<TSource, TTarget>(TSource source, TTarget? target)
        {
            if (source == null && target == null) return true;
            if (source == null || target == null) return false;
            
            // 기본적인 검증 로직
            return target.ToString() != null;
        }

        public static bool ValidateEmployeeConversion(int id, Employee? employee)
        {
            return employee != null && employee.Id == id;
        }

        public static bool ValidateJsonConversion(string json, Employee? employee)
        {
            if (string.IsNullOrEmpty(json) || employee == null) return false;
            try
            {
                var original = JsonConvert.DeserializeObject<Employee>(json);
                return original?.Id == employee.Id && original?.Name == employee.Name;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 통계 계산 헬퍼
    /// </summary>
    public static class StatisticsHelpers
    {
        public static double CalculateAverage(IEnumerable<double> values)
            => values.Any() ? values.Average() : 0;

        public static double CalculateMedian(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(x => x).ToArray();
            if (sorted.Length == 0) return 0;
            
            var mid = sorted.Length / 2;
            return sorted.Length % 2 == 0 
                ? (sorted[mid - 1] + sorted[mid]) / 2.0 
                : sorted[mid];
        }

        public static double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var array = values.ToArray();
            if (array.Length <= 1) return 0;
            
            var average = array.Average();
            var sumOfSquares = array.Sum(x => Math.Pow(x - average, 2));
            return Math.Sqrt(sumOfSquares / (array.Length - 1));
        }

        public static (double Min, double Max, double Average, double Median, double StdDev) CalculateStatistics(IEnumerable<double> values)
        {
            var array = values.ToArray();
            if (array.Length == 0) return (0, 0, 0, 0, 0);
            
            return (
                array.Min(),
                array.Max(),
                CalculateAverage(array),
                CalculateMedian(array),
                CalculateStandardDeviation(array)
            );
        }
    }
} 