using System.Resources;
using System.Globalization;

namespace WPFNode.Core.Resources;

public static class ExceptionMessages
{
    private static readonly ResourceManager ResourceManager = 
        new ResourceManager("WPFNode.Core.Resources.ExceptionMessages", typeof(ExceptionMessages).Assembly);

    public static string GetMessage(string key) =>
        ResourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    // 노드 연결 관련
    public const string SourceMustBeOutputPort = "SOURCE_MUST_BE_OUTPUT_PORT";
    public const string TargetMustBeInputPort = "TARGET_MUST_BE_INPUT_PORT";
    public const string PortsCannotBeConnected = "PORTS_CANNOT_BE_CONNECTED";
    public const string PortsMustBeAttachedToNode = "PORTS_MUST_BE_ATTACHED_TO_NODE";
    public const string PortsAlreadyConnected = "PORTS_ALREADY_CONNECTED";
    public const string ConnectionNotFound = "CONNECTION_NOT_FOUND";

    // 노드 검증 관련
    public const string NodeIsNull = "NODE_IS_NULL";
    public const string NodeMustInheritNodeBase = "NODE_MUST_INHERIT_NODEBASE";
    public const string NodeNotFound = "NODE_NOT_FOUND";
    public const string NodeListIsNull = "NODE_LIST_IS_NULL";

    // 그룹 관련
    public const string GroupIsNull = "GROUP_IS_NULL";
    public const string GroupNotFound = "GROUP_NOT_FOUND";
    public const string GroupAlreadyExists = "GROUP_ALREADY_EXISTS";
} 