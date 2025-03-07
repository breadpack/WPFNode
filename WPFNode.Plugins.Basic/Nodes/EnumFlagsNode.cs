using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;
using WPFNode.Attributes;
using WPFNode.Models;
using WPFNode.Interfaces;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("열거형 플래그")]
    [NodeDescription("플래그 열거형 값을 선택하고 조합합니다.")]
    [NodeCategory("값 편집")]
    public class EnumFlagsNode : NodeBase
    {
        private INodeProperty? _selectedTypeProperty;
        private INodeProperty? _flagsValueProperty;
        private Type? _enumType;
        private long _flagsValue;
        
        public EnumFlagsNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "열거형 플래그";
            Description = "플래그 열거형 값을 선택하고 조합합니다.";
            
            Initialize();
        }
        
        private void Initialize()
        {
            // 열거형 타입 선택 프로퍼티
            _selectedTypeProperty = CreateProperty<Type>("EnumType", "열거형 타입");
            
            // 플래그 값 프로퍼티
            _flagsValueProperty = CreateProperty<long>("Value", "플래그 값");
            _flagsValueProperty.Value = 0L;
            
            // 개별 플래그 선택 프로퍼티 생성을 위한 기본 타입 설정
            SetEnumType(typeof(System.IO.FileAttributes)); // 기본 플래그 열거형
            
            // 입력 포트와 출력 포트 추가
            CreateInputPort("SetValue", typeof(long));
            CreateOutputPort("Result", typeof(long));
        }
        
        protected override async Task ProcessAsync(CancellationToken cancellationToken = default)
        {
            // 입력 포트로부터 값을 가져옴
            var setValuePort = InputPorts.FirstOrDefault(p => p.Name == "SetValue") as InputPort<long>;
            if (setValuePort != null && setValuePort.IsConnected)
            {
                var inputValue = setValuePort.GetValueOrDefault(_flagsValue);
                if (inputValue != _flagsValue)
                {
                    _flagsValue = inputValue;
                    UpdateFlagsValueProperty();
                }
            }
            
            // 출력 포트에 현재 값 설정
            var outputPort = OutputPorts.FirstOrDefault(p => p.Name == "Result") as OutputPort<long>;
            if (outputPort != null)
            {
                outputPort.Value = _flagsValue;
            }
            
            await Task.CompletedTask;
        }
        
        private void UpdateFlagsValueProperty()
        {
            if (_flagsValueProperty != null)
            {
                _flagsValueProperty.Value = _flagsValue;
                
                // 개별 플래그 프로퍼티 업데이트
                if (_enumType != null)
                {
                    foreach (var flagName in Enum.GetNames(_enumType))
                    {
                        var flagValue = Enum.Parse(_enumType, flagName);
                        var flagLongValue = Convert.ToInt64(flagValue);
                        
                        var propName = $"Flag_{flagName}";
                        if (Properties.TryGetValue(propName, out var flagProperty))
                        {
                            var isSet = (_flagsValue & flagLongValue) == flagLongValue;
                            flagProperty.Value = isSet;
                        }
                    }
                }
            }
        }
        
        public void SetEnumType(Type enumType)
        {
            if (enumType == null || !enumType.IsEnum || 
                enumType.GetCustomAttribute<FlagsAttribute>() == null)
                return;
                
            _enumType = enumType;
            _selectedTypeProperty.Value = enumType;
            Name = $"열거형 플래그 ({enumType.Name})";
            
            // 기존 플래그 프로퍼티 제거
            foreach (var propName in Properties.Keys.ToList())
            {
                if (propName.StartsWith("Flag_"))
                {
                    RemoveProperty(propName);
                }
            }
            
            // 새 플래그 프로퍼티 추가
            foreach (var flagName in Enum.GetNames(enumType))
            {
                var flagValue = Enum.Parse(enumType, flagName);
                var flagLongValue = Convert.ToInt64(flagValue);
                
                if (flagLongValue != 0) // 0 값은 보통 'None'이므로 제외
                {
                    var propName = $"Flag_{flagName}";
                    var displayName = $"플래그: {flagName}";
                    var flagProperty = CreateProperty<bool>(propName, displayName);
                    
                    // 초기값 설정
                    var isSet = (_flagsValue & flagLongValue) == flagLongValue;
                    flagProperty.Value = isSet;
                    
                    // 값 변경 이벤트 구독
                    if (flagProperty is INotifyPropertyChanged notifyPropertyChanged)
                    {
                        notifyPropertyChanged.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(INodeProperty.Value))
                            {
                                bool isSelected = false;
                                if (flagProperty.Value is bool boolValue)
                                {
                                    isSelected = boolValue;
                                }
                                OnFlagChanged(flagName, flagLongValue, isSelected);
                            }
                        };
                    }
                }
            }
        }
        
        private void OnFlagChanged(string flagName, long flagValue, bool isSelected)
        {
            if (isSelected)
            {
                // 플래그 설정
                _flagsValue |= flagValue;
            }
            else
            {
                // 플래그 해제
                _flagsValue &= ~flagValue;
            }
            
            if (_flagsValueProperty != null)
            {
                _flagsValueProperty.Value = _flagsValue;
            }
        }
        
        public override async Task SetParameterAsync(object parameter)
        {
            if (parameter is Type type && type.IsEnum && 
                type.GetCustomAttribute<FlagsAttribute>() != null)
            {
                SetEnumType(type);
            }
            else if (parameter is long value)
            {
                _flagsValue = value;
                UpdateFlagsValueProperty();
            }
            
            await base.SetParameterAsync(parameter);
        }
    }
}
