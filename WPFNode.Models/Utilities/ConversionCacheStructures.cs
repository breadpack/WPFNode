using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Runtime.CompilerServices;

namespace WPFNode.Utilities;

/// <summary>
/// 타입 쌍에 대한 변환 전략 정보
/// </summary>
public readonly struct TypePairKey : IEquatable<TypePairKey>
{
    public readonly Type SourceType;
    public readonly Type TargetType;
    
    public TypePairKey(Type sourceType, Type targetType)
    {
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }
    
    public bool Equals(TypePairKey other)
    {
        return SourceType == other.SourceType && TargetType == other.TargetType;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is TypePairKey other && Equals(other);
    }
    
    public override int GetHashCode()
    {
        return HashCode.Combine(SourceType, TargetType);
    }
    
    public override string ToString()
    {
        return $"{SourceType.Name} -> {TargetType.Name}";
    }
}

/// <summary>
/// 변환 전략과 관련 메서드 정보를 담는 캐시 엔트리
/// </summary>
public readonly struct ConversionCacheEntry
{
    public readonly ConversionStrategy Strategy;
    public readonly MethodInfo? Method;
    public readonly ConstructorInfo? Constructor;
    
    public ConversionCacheEntry(ConversionStrategy strategy, MethodInfo? method = null, ConstructorInfo? constructor = null)
    {
        Strategy = strategy;
        Method = method;
        Constructor = constructor;
    }
    
    public bool IsValid => Strategy.IsValid();
    
    public override string ToString()
    {
        var methodInfo = Method?.Name ?? Constructor?.DeclaringType?.Name ?? "None";
        return $"{Strategy.GetDescription()} ({methodInfo})";
    }
}

/// <summary>
/// 타입별 메서드 정보 캐시 엔트리
/// </summary>
public readonly struct TypeMethodCache
{
    public readonly MethodInfo[] ImplicitOperators;
    public readonly MethodInfo[] ExplicitOperators;
    public readonly MethodInfo? ParseMethod;
    public readonly MethodInfo? TryParseMethod;
    
    public TypeMethodCache(
        MethodInfo[] implicitOperators,
        MethodInfo[] explicitOperators,
        MethodInfo? parseMethod = null,
        MethodInfo? tryParseMethod = null)
    {
        ImplicitOperators = implicitOperators ?? Array.Empty<MethodInfo>();
        ExplicitOperators = explicitOperators ?? Array.Empty<MethodInfo>();
        ParseMethod = parseMethod;
        TryParseMethod = tryParseMethod;
    }
}

/// <summary>
/// 스레드 안전하면서도 성능 중심의 변환 캐시 시스템
/// 
/// 핵심 설계 원칙:
/// 1. 완전한 Lock-free 구현 (ConcurrentDictionary 활용)
/// 2. 조건부 통계 기록 (성능 모드에서는 비활성화)
/// 3. 단기 실행 애플리케이션 특성 활용 (크기 제한 없음)
/// 4. 메모리 효율적인 키 구조
/// </summary>
public static class ConversionCache
{
    /// <summary>
    /// 타입 쌍별 변환 전략 캐시 (완전 스레드 안전)
    /// </summary>
    private static readonly ConcurrentDictionary<TypePairKey, ConversionCacheEntry> _typePairCache = new();
    
    /// <summary>
    /// 타입별 메서드 정보 캐시 (완전 스레드 안전)
    /// </summary>
    private static readonly ConcurrentDictionary<Type, TypeMethodCache> _typeMethodCache = new();
    
    /// <summary>
    /// 성능 모드 설정 (기본값: true)
    /// true = 최대 성능, false = 디버깅 모드
    /// </summary>
    public static bool IsPerformanceMode { get; set; } = true;
    
    /// <summary>
    /// 통계 정보 (디버그 모드에서만 활성화)
    /// </summary>
    public static class Statistics
    {
        private static long _hits = 0;
        private static long _misses = 0;
        
        public static long Hits => _hits;
        public static long Misses => _misses;
        public static long TotalOperations => _hits + _misses;
        public static double HitRatio => TotalOperations == 0 ? 0.0 : (double)_hits / TotalOperations;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RecordHit()
        {
            // 성능 모드에서는 통계 기록 완전 생략
            if (!IsPerformanceMode)
                Interlocked.Increment(ref _hits);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RecordMiss()
        {
            // 성능 모드에서는 통계 기록 완전 생략
            if (!IsPerformanceMode)
                Interlocked.Increment(ref _misses);
        }
        
        public static void Reset()
        {
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
        }
    }
    
    /// <summary>
    /// 변환 전략 조회 (최고 성능 우선)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetConversionStrategy(Type sourceType, Type targetType, out ConversionCacheEntry entry)
    {
        var key = new TypePairKey(sourceType, targetType);
        
        if (_typePairCache.TryGetValue(key, out entry))
        {
            Statistics.RecordHit();
            return entry.IsValid;
        }
        
        Statistics.RecordMiss();
        return false;
    }
    
    /// <summary>
    /// 변환 전략 저장 (Lock-free, 크기 제한 없음)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CacheConversionStrategy(Type sourceType, Type targetType, ConversionCacheEntry entry)
    {
        var key = new TypePairKey(sourceType, targetType);
        
        // TryAdd는 이미 존재하는 키에 대해서는 아무것도 하지 않음 (성능 최적화)
        _typePairCache.TryAdd(key, entry);
    }
    
    /// <summary>
    /// 타입별 메서드 정보를 캐시에서 가져오기
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetTypeMethodCache(Type type, out TypeMethodCache methodCache)
    {
        return _typeMethodCache.TryGetValue(type, out methodCache);
    }
    
    /// <summary>
    /// 타입별 메서드 정보를 캐시에 저장
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CacheTypeMethodInfo(Type type, TypeMethodCache methodCache)
    {
        _typeMethodCache.TryAdd(type, methodCache);
    }
    
    /// <summary>
    /// 캐시 상태 조회 (성능에 영향 없음)
    /// </summary>
    public static CacheStatus GetCacheStatus()
    {
        return new CacheStatus
        {
            TypePairCount = _typePairCache.Count,
            TypeMethodCount = _typeMethodCache.Count,
            HitRatio = Statistics.HitRatio,
            TotalOperations = Statistics.TotalOperations,
            IsPerformanceMode = IsPerformanceMode
        };
    }
    
    /// <summary>
    /// 캐시 초기화 (테스트/디버깅용)
    /// </summary>
    public static void Clear()
    {
        _typePairCache.Clear();
        _typeMethodCache.Clear();
        Statistics.Reset();
    }
    
    /// <summary>
    /// 성능 모드 전환
    /// </summary>
    public static void SetPerformanceMode(bool enabled)
    {
        IsPerformanceMode = enabled;
        if (enabled)
        {
            Statistics.Reset(); // 성능 모드로 전환 시 통계 초기화
        }
    }
}

/// <summary>
/// 캐시 상태 정보를 담는 구조체
/// </summary>
public readonly struct CacheStatus
{
    public int TypePairCount { get; init; }
    public int TypeMethodCount { get; init; }
    public double HitRatio { get; init; }
    public long TotalOperations { get; init; }
    public bool IsPerformanceMode { get; init; }
    
    public override string ToString()
    {
        return $"TypePairs: {TypePairCount}, " +
               $"TypeMethods: {TypeMethodCount}, " +
               $"HitRatio: {HitRatio:P2}, " +
               $"Operations: {TotalOperations}, " +
               $"Mode: {(IsPerformanceMode ? "Performance" : "Debug")}";
    }
} 