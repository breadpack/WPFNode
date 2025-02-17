using System.Runtime.Serialization;
using WPFNode.Constants;

namespace WPFNode.Exceptions;

[Serializable]
public class NodePluginException : NodeException
{
    public string? PluginPath { get; }
    public string? AssemblyName { get; }
    public Version? RequiredVersion { get; }
    public Version? ActualVersion { get; }

    public NodePluginException(string message) 
        : base(message, LoggerCategories.Plugin, "PluginOperation") { }
    
    public NodePluginException(string message, string pluginPath) 
        : base(message, LoggerCategories.Plugin, "PluginOperation")
    {
        PluginPath = pluginPath;
    }
    
    public NodePluginException(string message, string pluginPath, string assemblyName) 
        : base(message, LoggerCategories.Plugin, "PluginOperation")
    {
        PluginPath = pluginPath;
        AssemblyName = assemblyName;
    }

    public NodePluginException(string message, string pluginPath, Version requiredVersion, Version actualVersion) 
        : base(message, LoggerCategories.Plugin, "PluginOperation")
    {
        PluginPath = pluginPath;
        RequiredVersion = requiredVersion;
        ActualVersion = actualVersion;
    }
    
    public NodePluginException(string message, Exception inner) 
        : base(message, inner, LoggerCategories.Plugin, "PluginOperation") { }
    
    public NodePluginException(string message, string pluginPath, Exception inner) 
        : base(message, inner, LoggerCategories.Plugin, "PluginOperation")
    {
        PluginPath = pluginPath;
    }

    protected NodePluginException(SerializationInfo info, StreamingContext context) 
        : base(info, context)
    {
        PluginPath = info.GetString(nameof(PluginPath));
        AssemblyName = info.GetString(nameof(AssemblyName));
        RequiredVersion = (Version?)info.GetValue(nameof(RequiredVersion), typeof(Version));
        ActualVersion = (Version?)info.GetValue(nameof(ActualVersion), typeof(Version));
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null) throw new ArgumentNullException(nameof(info));
        
        info.AddValue(nameof(PluginPath), PluginPath);
        info.AddValue(nameof(AssemblyName), AssemblyName);
        info.AddValue(nameof(RequiredVersion), RequiredVersion);
        info.AddValue(nameof(ActualVersion), ActualVersion);
        base.GetObjectData(info, context);
    }
} 