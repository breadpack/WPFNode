# WPFNode

WPFNode는 WPF 애플리케이션을 위한 강력하고 유연한 노드 기반 에디터 프레임워크입니다.

## 주요 기능

- 시각적 프로그래밍을 위한 노드 기반 인터페이스
- 데이터 흐름 시각화
- 사용자 정의 노드 생성 지원
- 드래그 앤 드롭 노드 연결
- 플러그인 시스템 지원

## 설치

NuGet 패키지 관리자를 통해 설치할 수 있습니다:

```powershell
dotnet add package dev.BreadPack.WPFNode
```

또는 Visual Studio의 NuGet 패키지 관리자에서 'dev.BreadPack.WPFNode'를 검색하여 설치할 수 있습니다.

## 시작하기

1. WPF 프로젝트에 WPFNode 패키지를 설치합니다.
2. XAML에 네임스페이스를 추가합니다:

```xaml
xmlns:wpfnode="clr-namespace:WPFNode.Controls;assembly=WPFNode.Controls"
```

3. 노드 캔버스를 추가합니다:

```xaml
<wpfnode:NodeCanvas x:Name="nodeCanvas" />
```

## 라이선스

이 프로젝트는 MIT 라이선스 하에 배포됩니다. 자세한 내용은 [LICENSE](LICENSE) 파일을 참조하세요.

## 기여하기

버그 리포트, 기능 제안, 풀 리퀘스트를 환영합니다.

## 연락처

- 작성자: BreadPack
- 이메일: milennium9@breadpack.dev 