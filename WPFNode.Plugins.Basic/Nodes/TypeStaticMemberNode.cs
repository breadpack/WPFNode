using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using WPFNode.Attributes;
using WPFNode.Extensions;
using WPFNode.Interfaces;
using WPFNode.Models;
using WPFNode.Models.Execution;
using WPFNode.Models.Properties;
using Microsoft.Extensions.Logging;

namespace WPFNode.Plugins.Basic.Nodes
{
    [NodeName("Type.StaticMember")]
    [NodeDescription("지정한 타입의 public static 멤버(메서드, 프로퍼티, 필드)를 호출합니다.")]
    [NodeCategory("리플렉션")]
    public class TypeStaticMemberNode : NodeBase
    {
        [NodeFlowIn]
        public IFlowInPort FlowIn { get; private set; }

        [NodeFlowOut]
        public IFlowOutPort FlowOut { get; private set; }

        [NodeProperty("타입", OnValueChanged = nameof(TypeOrMember_Changed))]
        public NodeProperty<Type> TargetType { get; private set; }

        [NodeProperty("멤버 이름", OnValueChanged = nameof(TypeOrMember_Changed))]
        [NodeDropDown(nameof(GetAvailableMemberNames), nameof(GetMemberFriendlyNameString))]
        public NodeProperty<string> MemberName { get; private set; }

        // 실제 선택된 MemberInfo를 저장하는 프로퍼티 (직렬화 제외)
        [IgnoreDataMember]
        private MemberInfo SelectedMember { get; set; }

        private IOutputPort _resultOutput;
        private readonly List<IInputPort> _argumentPorts = new List<IInputPort>();
        private MemberTypes _selectedMemberType;
        private ParameterInfo[] _methodParameters;

        public TypeStaticMemberNode(INodeCanvas canvas, Guid guid) : base(canvas, guid)
        {
            Name = "Type.StaticMember";
            Description = "지정한 타입의 public static 멤버(메서드, 프로퍼티, 필드)를 호출합니다.";
        }

        private void TypeOrMember_Changed()
        {
            if (TargetType?.Value != null && !string.IsNullOrWhiteSpace(MemberName?.Value))
            {
                // 멤버 이름이 변경되면 해당 멤버를 찾아서 SelectedMember에 저장
                SelectedMember = FindMember(TargetType.Value, MemberName.Value);
            }
            else
            {
                SelectedMember = null;
            }
            
            ReconfigurePorts();
        }

        protected override void Configure(NodeBuilder builder)
        {
            _argumentPorts.Clear();

            if (SelectedMember == null)
            {
                _resultOutput = builder.Output("결과", typeof(object));
                return;
            }

            // 멤버 종류에 따라 파라미터 및 결과 타입 처리
            Type resultType = typeof(object);

            switch (SelectedMember)
            {
                case MethodInfo methodInfo:
                    _selectedMemberType = MemberTypes.Method;
                    _methodParameters = methodInfo.GetParameters();
                    resultType = methodInfo.ReturnType;
                    
                    // 메서드 파라미터에 대한 입력 포트 생성
                    for (int i = 0; i < _methodParameters.Length; i++)
                    {
                        var parameter = _methodParameters[i];
                        var inputPort = builder.Input($"Arg{i}: {parameter.Name}", parameter.ParameterType);
                        _argumentPorts.Add(inputPort);
                    }
                    break;

                case PropertyInfo propertyInfo:
                    _selectedMemberType = MemberTypes.Property;
                    resultType = propertyInfo.PropertyType;
                    break;

                case FieldInfo fieldInfo:
                    _selectedMemberType = MemberTypes.Field;
                    resultType = fieldInfo.FieldType;
                    break;
            }

            _resultOutput = builder.Output("결과", resultType);
        }

        /// <summary>
        /// 지정된 이름에 맞는 멤버(메서드, 프로퍼티, 필드)를 찾습니다.
        /// </summary>
        private MemberInfo FindMember(Type type, string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName) || type == null)
                return null;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            
            // 모든 사용 가능한 멤버 로드
            var allMembers = GetAvailableMembers().ToList();
            
            // 완전히 일치하는 멤버 찾기
            var exactMatch = allMembers.FirstOrDefault(m => 
                GetMemberIdentifier(m) == memberName);
            
            if (exactMatch != null)
                return exactMatch;
            
            // 프로퍼티 검색
            var property = type.GetProperty(memberName, flags);
            if (property != null) return property;

            // 필드 검색
            var field = type.GetField(memberName, flags);
            if (field != null) return field;
            
            // 메서드 검색 (단순 이름만으로)
            var methods = type.GetMethods(flags)
                .Where(m => m.Name == memberName)
                .ToList();
            
            if (methods.Count == 1)
                return methods[0];
            
            if (methods.Count > 1)
            {
                // 파라미터가 없는 메서드를 우선 선택
                var noParamMethod = methods.FirstOrDefault(m => m.GetParameters().Length == 0);
                if (noParamMethod != null)
                    return noParamMethod;
                
                // 파라미터가 없는 메서드가 없으면 첫 번째 메서드 반환
                return methods[0];
            }
            
            return null;
        }

        /// <summary>
        /// 멤버에 대한 고유 식별자 문자열을 반환합니다.
        /// 일반 멤버는 이름, 오버로드된 메서드는 GetUserFriendlyName을 사용합니다.
        /// </summary>
        private string GetMemberIdentifier(MemberInfo member)
        {
            if (member == null)
                return string.Empty;
            
            if (member is MethodInfo methodInfo)
            {
                var type = methodInfo.DeclaringType;
                const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
                
                // 오버로드된 메서드인지 확인
                var methodCount = type.GetMethods(flags).Count(m => m.Name == methodInfo.Name);
                
                // 오버로드된 메서드는 파라미터 정보가 포함된 이름 사용
                return methodCount > 1 ? methodInfo.GetUserFriendlyName() : methodInfo.Name;
            }
            
            // 메서드가 아닌 경우 단순 이름 사용
            return member.Name;
        }

        /// <summary>
        /// 사용 가능한 모든 멤버 목록 가져오기
        /// </summary>
        private IEnumerable<MemberInfo> GetAvailableMembers()
        {
            var targetType = TargetType?.Value;
            if (targetType == null)
                return Enumerable.Empty<MemberInfo>();

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static;
            
            // 메서드 정보 가져오기
            var methods = targetType.GetMethods(flags)
                .Where(m => !m.IsSpecialName)
                .Cast<MemberInfo>();
            
            // 프로퍼티 정보 가져오기
            var properties = targetType.GetProperties(flags)
                .Cast<MemberInfo>();
            
            // 필드 정보 가져오기
            var fields = targetType.GetFields(flags)
                .Cast<MemberInfo>();
            
            // 모든 멤버를 합쳐서 반환
            return methods.Concat(properties).Concat(fields);
        }

        /// <summary>
        /// 멤버의 사용자 친화적인 이름 가져오기 (MemberInfo에서 직접 변환)
        /// </summary>
        public string GetMemberFriendlyName(MemberInfo member)
        {
            if (member == null)
                return string.Empty;

            return member switch
            {
                MethodInfo methodInfo => methodInfo.GetUserFriendlySignature(),
                PropertyInfo propertyInfo => propertyInfo.GetUserFriendlyName(),
                FieldInfo fieldInfo => fieldInfo.GetUserFriendlyName(),
                _ => member.Name
            };
        }

        /// <summary>
        /// 멤버 이름에 해당하는 MemberInfo의 사용자 친화적인 이름 가져오기 (드롭다운 표시용)
        /// </summary>
        public string GetMemberFriendlyNameString(string memberName)
        {
            if (string.IsNullOrWhiteSpace(memberName) || TargetType?.Value == null)
                return memberName;

            var member = FindMember(TargetType.Value, memberName);
            if (member == null)
                return memberName;

            return GetMemberFriendlyName(member);
        }

        /// <summary>
        /// 멤버 목록의 이름을 문자열로 반환 (드롭다운에 표시할 항목 목록)
        /// </summary>
        public IEnumerable<string> GetAvailableMemberNames()
        {
            var members = GetAvailableMembers();
            
            // 멤버 식별자로 변환 (오버로드된 메서드는 파라미터 정보 포함)
            return members.Select(GetMemberIdentifier).OrderBy(name => name);
        }

        public override async IAsyncEnumerable<IFlowOutPort> ProcessAsync(
            IExecutionContext? context,
            CancellationToken cancellationToken = default)
        {
            object result = null;

            try
            {
                if (SelectedMember != null)
                {
                    switch (_selectedMemberType)
                    {
                        case MemberTypes.Method:
                            var methodInfo = (MethodInfo)SelectedMember;
                            object[] parameters = new object[_methodParameters.Length];

                            // 파라미터 값 수집
                            for (int i = 0; i < _methodParameters.Length; i++)
                            {
                                parameters[i] = _argumentPorts[i]?.Value;
                                
                                // 값 타입의 경우 null이면 기본값 사용
                                if (parameters[i] == null && _methodParameters[i].ParameterType.IsValueType)
                                {
                                    parameters[i] = Activator.CreateInstance(_methodParameters[i].ParameterType);
                                }
                            }

                            // 메서드 호출
                            result = methodInfo.Invoke(null, parameters);
                            Logger?.LogDebug($"{methodInfo.Name} 메서드 호출 결과: {result}");
                            break;

                        case MemberTypes.Property:
                            var propertyInfo = (PropertyInfo)SelectedMember;
                            result = propertyInfo.GetValue(null);
                            Logger?.LogDebug($"{propertyInfo.Name} 프로퍼티 값: {result}");
                            break;

                        case MemberTypes.Field:
                            var fieldInfo = (FieldInfo)SelectedMember;
                            result = fieldInfo.GetValue(null);
                            Logger?.LogDebug($"{fieldInfo.Name} 필드 값: {result}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, $"멤버 호출 중 오류 발생: {ex.Message}");
            }

            _resultOutput.Value = result;

            yield return FlowOut;
        }
    }
} 