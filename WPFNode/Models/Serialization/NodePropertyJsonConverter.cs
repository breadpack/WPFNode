using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Constants;
using WPFNode.Interfaces;

namespace WPFNode.Models.Serialization;

public class NodePropertyJsonConverter : JsonConverter<INodeProperty>
{
    public override INodeProperty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        var propertyData = JsonDocument.ParseValue(ref reader).RootElement;
        
        var name = propertyData.GetProperty("Name").GetString()!;
        var displayName = propertyData.GetProperty("DisplayName").GetString()!;
        var controlType = (NodePropertyControlType)propertyData.GetProperty("ControlType").GetInt32();
        var canConnectToPort = propertyData.GetProperty("CanConnectToPort").GetBoolean();
        var format = propertyData.GetProperty("Format").GetString();
        var propertyType = Type.GetType(propertyData.GetProperty("PropertyType").GetString()!);
        var value = propertyData.TryGetProperty("Value", out var valueElement) ? 
            JsonSerializer.Deserialize(valueElement.GetString()!, propertyType!) : null;

        return new PropertySerializationInfo
        {
            Name = name,
            DisplayName = displayName,
            ControlType = controlType,
            CanConnectToPort = canConnectToPort,
            Format = format,
            PropertyType = propertyType!,
            Value = value
        };
    }

    public override void Write(Utf8JsonWriter writer, INodeProperty value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString("Name", value.DisplayName);
        writer.WriteString("DisplayName", value.DisplayName);
        writer.WriteNumber("ControlType", (int)value.ControlType);
        writer.WriteBoolean("CanConnectToPort", value.CanConnectToPort);
        writer.WriteString("Format", value.Format);
        writer.WriteString("PropertyType", value.PropertyType.AssemblyQualifiedName);
        writer.WriteString("Value", JsonSerializer.Serialize(value.Value, value.PropertyType));
        
        writer.WriteEndObject();
    }
}

public class PropertySerializationInfo : INodeProperty, IJsonSerializable
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required NodePropertyControlType ControlType { get; init; }
    public required bool CanConnectToPort { get; set; }
    public required string? Format { get; init; }
    public required Type PropertyType { get; init; }
    public Type? ElementType => null;
    public object? Value { get; set; }
    public bool IsVisible { get; set; } = true;
    
    public IInputPort? ConnectedPort => null;
    public bool IsConnectedToPort => false;
    
    public void ConnectToPort(IInputPort port) { }
    public void DisconnectFromPort() { }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public void WriteJson(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        
        writer.WriteString("Name", Name);
        writer.WriteString("DisplayName", DisplayName);
        writer.WriteNumber("ControlType", (int)ControlType);
        writer.WriteBoolean("CanConnectToPort", CanConnectToPort);
        writer.WriteString("Format", Format);
        writer.WriteString("PropertyType", PropertyType.AssemblyQualifiedName);
        writer.WriteString("Value", JsonSerializer.Serialize(Value, PropertyType));
        writer.WriteBoolean("IsVisible", IsVisible);
        
        writer.WriteEndObject();
    }

    public void ReadJson(JsonElement element)
    {
        if (element.TryGetProperty("Value", out var valueElement))
        {
            Value = JsonSerializer.Deserialize(valueElement.GetString()!, PropertyType);
        }
        
        if (element.TryGetProperty("IsVisible", out var isVisibleElement))
        {
            IsVisible = isVisibleElement.GetBoolean();
        }
        
        if (element.TryGetProperty("CanConnectToPort", out var canConnectElement))
        {
            CanConnectToPort = canConnectElement.GetBoolean();
        }
    }
} 