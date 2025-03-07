using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Models.Properties;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("Object Collection")]
    [NodeDescription("다양한 타입의 객체 리스트를 생성합니다.")]
    [NodeCategory("데이터 변환")]
    public class ObjectCollectionNode : DynamicNode, IDisposable
    {
        private INodeProperty? _selectedType;
        private INodeProperty? _itemCountProperty;
        private Type? _targetType;
        private int _itemCount;
        private IList? _collection;
        private bool _isInitialized = false;
        private bool _isPropertyChangeHandlerAttached = false;
        private bool _disposed = false;
        private Dictionary<string, List<INodeProperty>> _itemProperties = new Dictionary<string, List<INodeProperty>>();

        [JsonConstructor]
        public ObjectCollectionNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "Object Collection";
            Description = "다양한 타입의 객체 리스트를 생성합니다.";
            
            Initialize();
            
            // 속성 변경 이벤트 구독
            PropertyChanged += ObjectCollectionNode_PropertyChanged;
        }
        
        private void ObjectCollectionNode_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Properties 컬렉션이 변경되었을 때 이벤트 핸들러 재연결
            if (e.PropertyName == nameof(Properties))
            {
                AttachPropertyChangeHandlers();
            }
        }
        
        private void AttachPropertyChangeHandlers()
        {
            // 이전 이벤트 핸들러가 있다면 제거
            if (_isPropertyChangeHandlerAttached)
            {
                if (_selectedType is INotifyPropertyChanged oldTypeProperty)
                {
                    oldTypeProperty.PropertyChanged -= SelectedType_PropertyChanged;
                }
                
                if (_itemCountProperty is INotifyPropertyChanged oldCountProperty)
                {
                    oldCountProperty.PropertyChanged -= ItemCount_PropertyChanged;
                }
                
                _isPropertyChangeHandlerAttached = false;
            }
            
            // 타입 속성 찾기 및 이벤트 연결
            if (Properties.TryGetValue("TargetType", out var typeProperty))
            {
                _selectedType = typeProperty;
                
                if (_selectedType is INotifyPropertyChanged notifyTypeChanged)
                {
                    notifyTypeChanged.PropertyChanged += SelectedType_PropertyChanged;
                }
            }
            
            // 항목 수 속성 찾기 및 이벤트 연결
            if (Properties.TryGetValue("ItemCount", out var countProperty))
            {
                _itemCountProperty = countProperty;
                
                if (_itemCountProperty is INotifyPropertyChanged notifyCountChanged)
                {
                    notifyCountChanged.PropertyChanged += ItemCount_PropertyChanged;
                }
            }
            
            _isPropertyChangeHandlerAttached = true;
        }
        
        private void SelectedType_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INodeProperty.Value) && _selectedType != null)
            {
                TargetType = _selectedType.Value as Type;
            }
        }
        
        private void ItemCount_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(INodeProperty.Value) && _itemCountProperty != null)
            {
                if (_itemCountProperty.Value is int count)
                {
                    ItemCount = count;
                }
            }
        }
        
        private void Initialize()
        {
            if (_isInitialized) return;
            
            // Type 선택 프로퍼티 생성
            _selectedType = AddProperty<Type>("TargetType", "대상 타입");
            
            // 항목 수 프로퍼티 생성 - 정수형 기본값 3
            _itemCountProperty = AddProperty("ItemCount", "항목 수", typeof(int));
            _itemCountProperty.Value = 3;
            
            // 초기값 설정
            _selectedType.Value = typeof(object);
            _targetType = typeof(object);
            _itemCount = 3;
            Name = $"Object Collection ({_targetType.Name})";
            
            // 출력 포트 구성
            ConfigureOutputPort();
            
            // 항목 프로퍼티 구성
            ConfigureItemProperties();
            
            // 이벤트 핸들러 연결
            AttachPropertyChangeHandlers();
            
            _isInitialized = true;
        }

        public Type? TargetType
        {
            get => _targetType;
            set
            {
                if (_targetType != value && value != null)
                {
                    _targetType = value;
                    Name = $"Object Collection ({_targetType.Name})";
                    
                    // 출력 포트 업데이트
                    ConfigureOutputPort();
                    
                    // 항목 프로퍼티 재구성
                    ConfigureItemProperties();
                }
            }
        }
        
        public int ItemCount
        {
            get => _itemCount;
            set
            {
                if (_itemCount != value && value > 0 && value <= 10)
                {
                    _itemCount = value;
                    
                    // 항목 프로퍼티 재구성
                    ConfigureItemProperties();
                }
            }
        }
        
        private void ConfigureOutputPort()
        {
            if (_targetType == null) return;
            
            try
            {
                // 기존 출력 포트 모두 제거
                ClearPorts("TargetType", "ItemCount");
                
                // 새 출력 포트 추가
                Type listType = typeof(List<>).MakeGenericType(_targetType);
                AddOutputPort("Collection", listType);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"출력 포트 구성 중 오류: {ex.Message}");
            }
        }
        
        private void ConfigureItemProperties()
        {
            if (_targetType == null) return;
            
            // 기존 항목별 프로퍼티 사전 비우기
            _itemProperties.Clear();
            
            // 타겟 타입의 쓰기 가능한 속성들 가져오기
            var targetProperties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToList();
                
            if (targetProperties.Count == 0) return;
            
            // 각 항목에 대해 프로퍼티 구성
            for (int i = 0; i < _itemCount; i++)
            {
                int index = i + 1;
                string itemKey = $"Item{index}";
                List<INodeProperty> itemProps = new List<INodeProperty>();
                
                // 항목별 속성 그룹 추가
                foreach (var prop in targetProperties)
                {
                    string propName = $"{itemKey}_{prop.Name}";
                    string displayName = $"항목 {index} - {prop.Name}";
                    
                    // 기존 프로퍼티가 있으면 제거
                    if (Properties.ContainsKey(propName))
                    {
                        RemoveProperty(propName);
                    }
                    
                    // 새 프로퍼티 추가
                    var nodeProperty = AddProperty(propName, displayName, prop.PropertyType);
                    
                    // 기본값 설정
                    if (prop.PropertyType == typeof(string))
                    {
                        nodeProperty.Value = $"Item {index}";
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        nodeProperty.Value = i * 10;
                    }
                    
                    // 항목 프로퍼티 리스트에 추가
                    itemProps.Add(nodeProperty);
                }
                
                _itemProperties[itemKey] = itemProps;
            }
        }

        protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            if (_targetType == null) return;

            try
            {
                // 제네릭 리스트 생성
                Type listType = typeof(List<>).MakeGenericType(_targetType);
                _collection = (IList)Activator.CreateInstance(listType);
                
                // 타입의 속성 정보 가져오기
                var targetProperties = _targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanWrite)
                    .ToList();
                
                // 각 항목 처리
                for (int i = 0; i < _itemCount; i++)
                {
                    string itemKey = $"Item{i+1}";
                    
                    // 첫 번째 속성(보통 이름)의 값을 확인하여 유효성 검사
                    if (!_itemProperties.TryGetValue(itemKey, out var itemProps) || itemProps.Count == 0)
                        continue;
                        
                    var firstProp = itemProps.FirstOrDefault();
                    var firstValue = firstProp?.Value;
                    
                    // 첫 번째 속성이 문자열이고 비어있는 경우 이 항목 건너뛰기
                    if (firstValue == null || (firstValue is string str && string.IsNullOrEmpty(str)))
                        continue;
                    
                    // 새 객체 생성
                    var newObject = Activator.CreateInstance(_targetType);
                    
                    // 객체 속성 설정
                    foreach (var prop in targetProperties)
                    {
                        string propName = $"{itemKey}_{prop.Name}";
                        if (!Properties.TryGetValue(propName, out var nodeProperty) || nodeProperty.Value == null)
                            continue;
                            
                        try
                        {
                            // 속성 값 설정
                            prop.SetValue(newObject, Convert.ChangeType(nodeProperty.Value, prop.PropertyType));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Property {prop.Name} 설정 중 오류: {ex.Message}");
                        }
                    }
                    
                    // 컬렉션에 객체 추가
                    _collection.Add(newObject);
                }
                
                // 출력 설정
                var collectionPort = OutputPorts.FirstOrDefault(p => p.Name == "Collection");
                if (collectionPort != null)
                {
                    // 리플렉션을 사용하여 포트의 Value 속성에 값 설정
                    var portType = collectionPort.GetType();
                    var valueProperty = portType.GetProperty("Value");
                    if (valueProperty != null)
                    {
                        valueProperty.SetValue(collectionPort, _collection);
                    }
                }
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"컬렉션 처리 중 오류: {ex.Message}");
                throw;
            }
        }
        
        public override async Task SetParameterAsync(object parameter)
        {
            if (parameter is Type type)
            {
                _selectedType.Value = type;
                TargetType = type;
            }
            else if (parameter is int count)
            {
                _itemCountProperty.Value = count;
                ItemCount = count;
            }
            
            await base.SetParameterAsync(parameter);
        }
        
        public override void ReadJson(JsonElement element, JsonSerializerOptions options)
        {
            // 기본 역직렬화 수행
            base.ReadJson(element, options);
            
            // JSON 역직렬화 후에는 이미 초기화된 상태로 간주
            _isInitialized = true;
            
            // 이벤트 핸들러 연결
            AttachPropertyChangeHandlers();
            
            // 출력 포트 구성
            ConfigureOutputPort();
            
            // 항목 프로퍼티 구성
            ConfigureItemProperties();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
                
            if (disposing)
            {
                // 이벤트 구독 해제
                PropertyChanged -= ObjectCollectionNode_PropertyChanged;
                
                if (_selectedType is INotifyPropertyChanged notifyTypeChanged)
                {
                    notifyTypeChanged.PropertyChanged -= SelectedType_PropertyChanged;
                }
                
                if (_itemCountProperty is INotifyPropertyChanged notifyCountChanged)
                {
                    notifyCountChanged.PropertyChanged -= ItemCount_PropertyChanged;
                }
            }
            
            _disposed = true;
        }

        public override string ToString()
        {
            return $"Object Collection Node ({(_targetType?.Name ?? "Unknown")})";
        }
    }
}
