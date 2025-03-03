using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using WPFNode.Models;
using WPFNode.Models.Serialization;

namespace WPFNode.Demo.Models.Serialization
{
    public class NodeCanvasMigrationConverter : JsonConverter<NodeCanvas>
    {
        public override NodeCanvas Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // 기본 NodeCanvasJsonConverter를 사용하여 역직렬화
            var defaultConverter = new NodeCanvasJsonConverter();
            return defaultConverter.Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, NodeCanvas value, JsonSerializerOptions options)
        {
            // 기본 NodeCanvasJsonConverter를 사용하여 직렬화
            var defaultConverter = new NodeCanvasJsonConverter();
            defaultConverter.Write(writer, value, options);
        }
    }
} 