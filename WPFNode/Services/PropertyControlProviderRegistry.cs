using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPFNode.Controls.PropertyControls;
using WPFNode.Interfaces;

namespace WPFNode.Services;

public class PropertyControlProviderRegistry
{
    private readonly Dictionary<string, IPropertyControlProvider> _providers = new();
    
    public void RegisterProvider(IPropertyControlProvider provider)
    {
        _providers[provider.ControlTypeId] = provider;
    }
    
    public void RegisterProviders()
    {
        // 리플렉션을 통한 Provider 등록
        var providerTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    return Array.Empty<Type>();
                }
            })
            .Where(t => typeof(IPropertyControlProvider).IsAssignableFrom(t) && 
                       !t.IsInterface && 
                       !t.IsAbstract);

        foreach (var providerType in providerTypes)
        {
            try
            {
                if (Activator.CreateInstance(providerType) is IPropertyControlProvider provider)
                {
                    RegisterProvider(provider);
                }
            }
            catch (Exception)
            {
                // 생성자 호출 실패 시 해당 타입은 건너뜀
            }
        }
    }
    
    public IPropertyControlProvider? GetProvider(Type propertyType)
    {
        return _providers.Values
            .Where(p => p.CanHandle(propertyType))
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();
    }
    
    public FrameworkElement CreateControl(INodeProperty property)
    {
        var provider = GetProvider(property.PropertyType);
        return provider?.CreateControl(property) ?? new TextBox();
    }
} 