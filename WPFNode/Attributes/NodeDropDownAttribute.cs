using System;

namespace WPFNode.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class NodeDropDownAttribute : Attribute
{
    /// <summary>
    /// Method name to get the elements for the dropdown that has no parameters.
    /// </summary>
    /// <code>
    /// List&lt;string&gt; GetElements() { ... }
    /// IEnumerable&lt;int&gt; GetElements() { ... }
    /// IReadOnlyList&lt;CustomType&gt; GetElements() { ... }
    /// </code>
    public string ElementsMethodName { get; }
    
    /// <summary>
    /// Method name to convert the element to a string for display that has one parameter of the element type.
    /// </summary>
    /// <code>
    /// string ConvertToString(CustomType element) { ... }
    /// string ConvertToString(int element) { ... }
    /// string ConvertToString(string element) { ... }
    /// </code>
    public string NameConverterMethodName { get; set; }

    public NodeDropDownAttribute(string elementsMethodName)
    {
        ElementsMethodName = elementsMethodName;
        NameConverterMethodName = string.Empty;
    }
} 