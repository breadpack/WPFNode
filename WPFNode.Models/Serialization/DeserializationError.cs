namespace WPFNode.Models.Serialization;

public class DeserializationError
{
    public string    ElementType { get; set; } // "Node", "Connection", "Group" 등
    public string    ElementId   { get; set; }
    public string    Message     { get; set; }
    public string    Details     { get; set; }
    public Exception Exception   { get; set; }

    public DeserializationError(string elementType, string elementId, string message, string details, Exception exception)
    {
        ElementType = elementType;
        ElementId   = elementId;
        Message     = message;
        Details     = details;
        Exception   = exception;
    }
}