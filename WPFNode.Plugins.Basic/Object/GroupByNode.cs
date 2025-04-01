using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using WPFNode.Attributes;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Properties; // Needed for NodeProperty<T>

namespace WPFNode.Plugins.Basic.Object;

[NodeName("그룹화 (루프)")]
[NodeCategory("객체")]
[NodeDescription("컬렉션을 지정된 키(속성/필드)로 그룹화하고 각 그룹에 대해 루프를 실행합니다.")]
public class GroupByNode : NodeBase {
    // --- Static Ports ---
    [NodeFlowIn("실행")]
    public IFlowInPort FlowIn { get; set; }

    [NodeFlowOut("Loop Body")]
    public IFlowOutPort LoopBody { get; set; }

    [NodeFlowOut("완료")]
    public IFlowOutPort FlowComplete { get; set; }

    // --- Node Properties for Configuration ---
    // Correct attribute usage and remove initializers
    [NodeProperty("컬렉션")]
    public NodeProperty<IEnumerable> InputCollection { get; set; } // Remove = new()

    [NodeProperty("항목 타입", OnValueChanged = nameof(OnDefinitionChanged))]
    public NodeProperty<Type> ItemType { get; set; } // Remove = new()

    [NodeProperty("키 멤버 (속성/필드)", OnValueChanged = nameof(OnDefinitionChanged))]
    [NodeDropDown(nameof(GetKeyMembers))]
    public NodeProperty<string> SelectedKeyMember { get; set; } // Remove = new()

    // --- Dynamic Output Ports (managed via Configure) ---
    private IOutputPort? _currentKeyOutput;
    private IOutputPort? _currentItemsOutput;

    // --- Constructor ---
    [JsonConstructor] // Needed if properties are set via constructor/deserialization
    public GroupByNode(INodeCanvas canvas, Guid guid) : base(canvas, guid) { }

    // --- Configuration Logic ---

    /// <summary>
    /// Provides the list of public instance properties and fields for the dropdown.
    /// </summary>
    public IEnumerable<string> GetKeyMembers() {
        var type = ItemType?.Value;
        if (type == null) return Enumerable.Empty<string>();

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Select(p => p.Name);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                         .Select(f => f.Name);

        return properties.Concat(fields).OrderBy(name => name);
    }

    /// <summary>
    /// Called when ItemType or SelectedKeyMember changes.
    /// </summary>
    private void OnDefinitionChanged() {
        ReconfigurePorts(); // Trigger reconfiguration
    }

    protected override void Configure(NodeBuilder builder) {
        // Determine dynamic port types
        var currentItemType = ItemType?.Value;
        var keyMemberName   = SelectedKeyMember?.Value;
        var keyType         = typeof(object);       // Default
        var itemsListType   = typeof(List<object>); // Default

        if (currentItemType != null) // Determine list type based on ItemType
        {
            itemsListType = typeof(List<>).MakeGenericType(currentItemType);

            // Determine key type if member is selected
            if (!string.IsNullOrWhiteSpace(keyMemberName)) {
                var memberInfo = currentItemType.GetProperty(keyMemberName, BindingFlags.Public | BindingFlags.Instance) as MemberInfo
                              ?? currentItemType.GetField(keyMemberName, BindingFlags.Public | BindingFlags.Instance);

                if (memberInfo != null) {
                    keyType = memberInfo switch {
                        PropertyInfo pi => pi.PropertyType,
                        FieldInfo fi    => fi.FieldType,
                        _               => typeof(object)
                    };
                }
            }
        }

        // Define dynamic output ports using determined types
        _currentKeyOutput   = builder.Output("Current Key", keyType);
        _currentItemsOutput = builder.Output("Current Items", itemsListType);
    }


    // --- Execution Logic ---
    public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
        IExecutionContext? context,
        CancellationToken  cancellationToken
    ) {
        var collection    = InputCollection?.Value; 
        var keyMemberName = SelectedKeyMember?.Value;
        var itemType      = ItemType?.Value;

        // Validate inputs
        if (collection == null || string.IsNullOrWhiteSpace(keyMemberName) || itemType == null) {
            Debug.WriteLine("GroupByNode: Input collection, key member, or item type is null or empty.");
            yield return FlowComplete;
            yield break;
        }

        // Find the key member (property or field)
        var keyMemberInfo = itemType.GetProperty(keyMemberName, BindingFlags.Public | BindingFlags.Instance) as MemberInfo
                         ?? itemType.GetField(keyMemberName, BindingFlags.Public | BindingFlags.Instance);

        if (keyMemberInfo == null) {
            Debug.WriteLine($"GroupByNode: Key member '{keyMemberName}' not found on type '{itemType.Name}'.");
            yield return FlowComplete;
            yield break;
        }

        // 캐시된 키 값 추출 함수 생성 (성능 최적화)
        Func<object, object> keyExtractor = CreateKeyExtractor(keyMemberInfo);

        // 그룹화 수행
        var groupedItems = TryGroupItems(collection, itemType, keyExtractor);
        
        if (groupedItems == null) {
            yield return FlowComplete;
            yield break;
        }

        // 각 그룹 처리
        foreach (var group in groupedItems) {
            cancellationToken.ThrowIfCancellationRequested();
            
            // 출력 포트 값 설정 시도
            if (!TrySetGroupOutputs(group.Key, group.Value, itemType)) {
                continue; // 출력 설정 실패시 다음 그룹으로
            }

            // 루프 본문 실행
            yield return LoopBody;
        }

        // 완료
        yield return FlowComplete;
    }

    /// <summary>
    /// 멤버 정보를 기반으로 키 추출 함수를 생성합니다.
    /// </summary>
    private Func<object, object> CreateKeyExtractor(MemberInfo memberInfo) {
        return memberInfo switch {
            PropertyInfo pi => obj => pi.GetValue(obj),
            FieldInfo fi => obj => fi.GetValue(obj),
            _ => _ => null
        };
    }

    /// <summary>
    /// 컬렉션을 그룹화하는 메서드 - Dictionary 사용하여 성능 최적화
    /// </summary>
    private Dictionary<object, List<object>> TryGroupItems(
        IEnumerable collection, 
        Type itemType,
        Func<object, object> keyExtractor
    ) {
        try {
            // 결과를 저장할 Dictionary - 키는 null을 허용
            var result = new Dictionary<object, List<object>>(new NullableKeyComparer());
            
            // 컬렉션 순회하며 직접 그룹화 (LINQ보다 빠름)
            foreach (var item in collection) {
                // 유효하지 않은 항목 스킵
                if (item == null || !itemType.IsInstanceOfType(item)) {
                    continue;
                }
                
                // 키 추출
                var key = keyExtractor(item);
                
                // 그룹에 항목 추가
                if (!result.TryGetValue(key, out var group)) {
                    group = new List<object>();
                    result[key] = group;
                }
                
                group.Add(item);
            }
            
            return result;
        }
        catch (Exception ex) {
            Debug.WriteLine($"GroupByNode: Error during grouping - {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Dictionary의 키로 null을 허용하기 위한 Comparer
    /// </summary>
    private class NullableKeyComparer : IEqualityComparer<object> {
        public new bool Equals(object x, object y) {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(object obj) {
            return obj?.GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// 그룹의 출력 포트 값 설정
    /// </summary>
    private bool TrySetGroupOutputs(object key, List<object> items, Type itemType) {
        try {
            // 타입이 지정된 리스트 생성 - 이미 생성된 타입의 인스턴스를 재사용
            var listType = typeof(List<>).MakeGenericType(itemType);
            var typedList = (IList)Activator.CreateInstance(listType, items.Count)!; // 크기 힌트 제공
            
            // 항목 추가 - 이미 올바른 타입의 항목만 필터링되어 있음
            foreach (var item in items) {
                typedList.Add(item);
            }

            // 출력 포트 값 설정
            if (_currentKeyOutput is not null) _currentKeyOutput.Value = key;
            if (_currentItemsOutput is not null) _currentItemsOutput.Value = typedList;
            
            return true;
        }
        catch (Exception ex) {
            Debug.WriteLine($"GroupByNode: Error setting group outputs - {ex.Message}");
            return false;
        }
    }
}