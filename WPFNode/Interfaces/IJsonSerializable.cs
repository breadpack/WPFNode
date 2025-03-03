using System.Text.Json;

namespace WPFNode.Interfaces;

public interface IJsonSerializable
{
    void WriteJson(Utf8JsonWriter writer);
    void ReadJson(JsonElement     element, JsonSerializerOptions options);
} 