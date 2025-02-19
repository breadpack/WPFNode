using System.Text.Json;
using System.Text.Json.Serialization;

namespace WPFNode.Models.Serialization;

public class TypeJsonConverter : JsonConverter<Type>
{
    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? assemblyQualifiedName = reader.GetString();
            return assemblyQualifiedName != null ? Type.GetType(assemblyQualifiedName) : null;
        }
        
        throw new JsonException("Expected string value for Type");
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.AssemblyQualifiedName);
    }
} 