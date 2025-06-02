# WPFNode.Benchmarks

WPFNode 타입 변환 시스템의 성능을 측정하고 분석하는 벤치마크 프로젝트입니다.

## 🎯 목적

이 프로젝트는 WPFNode의 타입 변환 시스템에 대한 포괄적인 성능 분석을 제공합니다:

- **타입 검사 성능**: `CanConvertTo` 메서드의 성능 측정
- **값 변환 성능**: `TryConvertTo` 메서드의 실제 변환 성능
- **메모리 사용량**: GC 압박과 메모리 할당 패턴 분석
- **실제 사용 시나리오**: 다양한 데이터 크기와 타입 조합에서의 성능
- **최적화 효과**: 캐싱, 병렬 처리 등 최적화 기법의 효과 측정

## 📁 프로젝트 구조

```
WPFNode.Benchmarks/
├── Program.cs                          # 메인 진입점
├── Benchmarks/
│   ├── Core/                          # 핵심 기능 벤치마크
│   │   ├── TypeCheckBenchmarks.cs     # 타입 검사 성능
│   │   ├── ValueConversionBenchmarks.cs # 값 변환 성능
│   │   └── MemoryBenchmarks.cs        # 메모리 사용량
│   ├── Scenarios/                     # 실제 사용 시나리오
│   │   ├── BasicTypeBenchmarks.cs     # 기본 타입 변환
│   │   └── CustomTypeBenchmarks.cs    # 커스텀 타입 변환
│   └── Comparisons/                   # 성능 비교
│       ├── ConversionMethodBenchmarks.cs # 변환 방법별 비교
│       └── OptimizationBenchmarks.cs  # 최적화 전후 비교
├── Data/                              # 테스트 데이터 및 헬퍼
│   ├── TestDataGenerator.cs          # 테스트 데이터 생성
│   └── BenchmarkHelpers.cs           # 공통 헬퍼 메서드
└── Reports/                           # 벤치마크 결과 저장
```

## 🚀 사용법

### 기본 실행

```bash
# 모든 벤치마크 실행
dotnet run -c Release

# 특정 카테고리 실행
dotnet run -c Release core        # 핵심 기능 벤치마크
dotnet run -c Release scenarios  # 실제 사용 시나리오
dotnet run -c Release comparisons # 성능 비교

# 빠른 테스트 (Dry 모드)
dotnet run -c Release quick
```

### 고급 실행 옵션

```bash
# 특정 벤치마크 필터링
dotnet run -c Release -- --filter "*TypeCheck*"
dotnet run -c Release -- --filter "*Memory*"

# 결과를 특정 형식으로 내보내기
dotnet run -c Release -- --exporters html,csv
dotnet run -c Release -- --artifacts ./results

# 메모리 진단 포함
dotnet run -c Release -- --memory
```

## 📊 벤치마크 카테고리

### 1. 핵심 기능 (Core)

#### TypeCheckBenchmarks
- 기본 타입 호환성 검사 (`int` → `string`)
- 커스텀 타입 호환성 검사 (`int` → `Employee`)
- 제네릭 타입 검사 (`List<int>` → `int[]`)
- 반복적 타입 검사 (캐싱 효과 측정)
- 다양한 검사 방법 성능 비교

#### ValueConversionBenchmarks
- 기본 타입 변환 (`int` ↔ `string`, `double` 등)
- 커스텀 타입 변환 (암시적/명시적 연산자)
- JSON 변환 (생성자 기반)
- 변환 방법별 성능 비교
- 실패 케이스 처리 성능

#### MemoryBenchmarks
- 타입 검사 메모리 사용량
- 변환 작업 메모리 할당
- GC 압박 측정
- 메모리 누수 확인
- 대용량 데이터 처리

### 2. 실제 시나리오 (Scenarios)

#### BasicTypeBenchmarks
- 배치 변환 처리 (1K, 10K, 100K 항목)
- 다양한 숫자 타입 변환
- Boolean, DateTime, Enum 변환
- Nullable 타입 처리
- 혼합 타입 변환 시나리오

#### CustomTypeBenchmarks
- Employee 타입 변환 (암시적/명시적)
- 체인 변환 (`int` → `Employee` → `string` → `Employee`)
- 복잡한 JSON 객체 변환
- 병렬 처리 성능
- 메모리 집약적 변환

### 3. 성능 비교 (Comparisons)

#### ConversionMethodBenchmarks
- Parse vs TryParse vs TypeConverter
- Convert.ChangeType vs Extension Method
- 실패 케이스 처리 방법 비교
- 다양한 타입별 최적 변환 방법

#### OptimizationBenchmarks
- 캐싱 적용 전후 비교
- 병렬 처리 vs 순차 처리
- StringBuilder vs 문자열 연결
- 객체 풀링 효과
- 리플렉션 최적화
- 예외 처리 vs TryParse 패턴

## 📈 성능 지표

각 벤치마크는 다음 지표들을 측정합니다:

- **실행 시간**: 평균, 중앙값, 95퍼센타일
- **처리량**: 초당 처리 가능한 작업 수
- **메모리 할당**: Gen0/1/2 GC 횟수, 할당된 바이트 수
- **확장성**: 데이터 크기별 성능 변화

## 🔧 설정 및 매개변수

### 데이터 크기 매개변수
```csharp
[Params(100, 1000, 10000)]
public int DataSize { get; set; }
```

### 반복 횟수 매개변수
```csharp
[Params(1, 100, 1000, 10000)]
public int IterationCount { get; set; }
```

### 벤치마크 속성
- `[MemoryDiagnoser]`: 메모리 사용량 측정
- `[RankColumn]`: 성능 순위 표시
- `[Baseline]`: 기준 벤치마크 설정

## 📋 결과 해석

### 성능 순위
- **Baseline**: 기준이 되는 벤치마크 (1.00x)
- **Ratio**: 기준 대비 성능 비율 (낮을수록 빠름)
- **RatioSD**: 성능 비율의 표준편차

### 메모리 지표
- **Gen 0/1/2**: 각 세대별 GC 발생 횟수
- **Allocated**: 할당된 메모리 양 (바이트)

### 시간 지표
- **Mean**: 평균 실행 시간
- **Error**: 표준 오차
- **StdDev**: 표준 편차

## 🎯 최적화 권장사항

벤치마크 결과를 바탕으로 한 최적화 권장사항:

1. **타입 검사 캐싱**: 동일한 타입 조합의 반복 검사 시 캐싱 적용
2. **적절한 변환 방법 선택**: 상황별 최적 변환 방법 사용
3. **배치 처리**: 대량 데이터 변환 시 병렬 처리 고려
4. **메모리 관리**: 불필요한 할당 최소화 및 객체 재사용
5. **예외 처리**: TryParse 패턴 사용으로 예외 발생 최소화

## 🔍 문제 해결

### 일반적인 문제

1. **OutOfMemoryException**: 데이터 크기 매개변수 조정
2. **긴 실행 시간**: `[SimpleJob(RuntimeMoniker.Net60)]` 사용
3. **불안정한 결과**: 워밍업 라운드 증가

### 디버깅

```bash
# 디버그 모드로 실행
dotnet run -c Debug

# 특정 벤치마크만 실행
dotnet run -c Release -- --filter "TypeCheckBenchmarks.BasicTypeCheck_IntToString"
```

## 📚 참고 자료

- [BenchmarkDotNet 공식 문서](https://benchmarkdotnet.org/)
- [.NET 성능 최적화 가이드](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
- [WPFNode 타입 변환 시스템 문서](../WPFNode.Models/Utilities/TypeUtility.cs)

## 🤝 기여하기

새로운 벤치마크 시나리오나 최적화 아이디어가 있다면:

1. 새 벤치마크 클래스 작성
2. 적절한 카테고리에 배치
3. 문서 업데이트
4. Pull Request 제출

---

**참고**: 벤치마크는 Release 모드에서 실행해야 정확한 성능 측정이 가능합니다. 