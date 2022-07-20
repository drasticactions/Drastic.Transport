// <copyright file="MessageJsonConverter.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;
using System.Text.Json;

namespace Drastic.Transport
{
    public class MessageJsonConverter : JsonConverter<Message>
    {
        public override bool CanConvert(Type type)
        {
            return type.IsAssignableFrom(typeof(Message));
        }

        public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (JsonDocument.TryParseValue(ref reader, out var doc))
            {
                if (doc.RootElement.TryGetProperty("Type", out var type))
                {
                    var typeValue = type.GetString();
                    var rootElement = doc.RootElement.GetRawText();

                    return typeValue switch
                    {
                        nameof(RuntimeHostMessage) => JsonSerializer.Deserialize<RuntimeHostMessage>(rootElement, options),
                        nameof(ConnectMessage) => JsonSerializer.Deserialize<ConnectMessage>(rootElement, options),
                        nameof(DisconnectMessage) => JsonSerializer.Deserialize<DisconnectMessage>(rootElement, options),
                        nameof(LogMessageMessage) => JsonSerializer.Deserialize<LogMessageMessage>(rootElement, options),
                        _ => throw new JsonException($"{typeValue} has not been mapped to a custom type")
                    };
                }

                throw new JsonException("Failed to extract type property.");
            }

            throw new JsonException("Failed to parse JsonDocument");
        }

        public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, options);
        }
    }
}
