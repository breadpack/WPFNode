using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class DropDownAttribute : Attribute
{
    // 선택 가능한 값 목록을 동적으로 가져오는 메서드 이름
    public string OptionsProviderMethodName { get; }
    
    public DropDownAttribute(string optionsProviderMethodName)
    {
        OptionsProviderMethodName = optionsProviderMethodName;
    }
}
