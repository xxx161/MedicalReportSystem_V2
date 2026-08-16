using System.Text.Json;
using System.Text.Json.Serialization;

namespace MedicalReportSystem.Models
{
    public static class JsonConverters
    {
        /// <summary>
        /// 数字转字符串
        /// </summary>
        public class FlexibleIntConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        if (reader.TryGetInt32(out int intValue))
                        {
                            return intValue.ToString();
                        }
                        if (reader.TryGetInt64(out long longValue))
                        {
                            return longValue.ToString();
                        }
                        return reader.GetDouble().ToString();

                    case JsonTokenType.String:
                        return reader.GetString();

                    case JsonTokenType.Null:
                        return null;

                    default:
                        throw new JsonException($"无法将 {reader.TokenType} 转换为字符串");
                }
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }
        }

        /// <summary>
        /// 数字转整型
        /// </summary>
        public class FlexibleStringToIntConverter : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.Number:
                        return reader.GetInt32();

                    case JsonTokenType.String:
                        if (int.TryParse(reader.GetString(), out int result))
                        {
                            return result;
                        }
                        throw new JsonException($"无法将字符串 '{reader.GetString()}' 转换为整型");

                    case JsonTokenType.Null:
                        return default;

                    default:
                        throw new JsonException($"无法将 {reader.TokenType} 转换为整型");
                }
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value);
            }
        }
    }

}
