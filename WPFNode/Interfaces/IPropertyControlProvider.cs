using System.Windows;

namespace WPFNode.Interfaces;

public interface IPropertyControlProvider
{
    // 이 제공자가 처리할 수 있는 타입인지 확인
    bool CanHandle(Type propertyType);
    
    // 컨트롤 생성
    FrameworkElement CreateControl(INodeProperty property);
    
    // 우선순위 - 같은 타입을 처리할 수 있는 여러 제공자가 있을 경우 사용
    int Priority { get; }
    
    // 컨트롤의 고유 식별자
    string ControlTypeId { get; }
} 