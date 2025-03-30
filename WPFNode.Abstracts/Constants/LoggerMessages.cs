namespace WPFNode.Constants;

public static class LoggerMessages
{
    public const string AssemblyLoadError = "어셈블리 로드 중 오류 발생: {AssemblyName}";
    public const string AssemblyInspecting = "어셈블리 검사 중: {AssemblyName}";
    public const string NodeTypeRegistrationFailed = "노드 타입 등록 실패: {TypeName}";
    public const string AssemblyTypeLoadFailed = "어셈블리 타입 로드 실패: {AssemblyName}";
    public const string AssemblyProcessingError = "어셈블리 처리 중 오류 발생: {AssemblyName}";
    public const string PluginDirectoryNotFound = "플러그인 디렉토리가 존재하지 않습니다: {Path}";
    public const string ExternalPluginLoadFailed = "외부 플러그인 어셈블리 로드 실패: {Path}";
    public const string ResourceDictionaryLoadFailed = "리소스 딕셔너리 로드 실패: {Assembly}, {ResourceFile}";
    public const string NodeStyleSearchError = "노드 스타일 검색 중 오류 발생: {NodeType}";
    public const string NodeInstanceCreationFailed = "노드 인스턴스 생성 실패: {Type}";
    public const string NodeExecutionFailed = "노드 실행 실패: {NodeName}";
    public const string CircularDependencyDetected = "순환 의존성이 감지되었습니다: {NodeName}";
    public const string InvalidNodeType = "유효하지 않은 노드 타입입니다: {TypeName}";
    public const string UnregisteredNodeType = "등록되지 않은 노드 타입입니다: {TypeName}";
    public const string ConnectionError = "노드 연결 중 오류 발생: {SourceNode} -> {TargetNode}";
    public const string PropertyValidationError = "속성 유효성 검사 실패: {NodeName}.{PropertyName}";
    
    // 실행 관련 메시지
    public const string ExecutionCancelled = "실행이 취소되었습니다.";
    public const string ExecutionStarted = "노드 실행이 시작되었습니다: {NodeName}";
    public const string ExecutionCompleted = "노드 실행이 완료되었습니다: {NodeName}";
    public const string LevelExecutionStarted = "레벨 {Level} 실행이 시작되었습니다.";
    public const string LevelExecutionCompleted = "레벨 {Level} 실행이 완료되었습니다.";
} 