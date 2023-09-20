using Confluent.Kafka;
using System;
using System.Text;
using System.Text.Json;

namespace PlanetoidGen.DataAccess.Repositories.Messaging.Kafka
{
    internal class KafkaSerializer<TMessage> : ISerializer<TMessage>, IDeserializer<TMessage?>
        where TMessage : class
    {
        public byte[] Serialize(TMessage data, SerializationContext context)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
        }

        public TMessage? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            return isNull ? default : JsonSerializer.Deserialize<TMessage>(Encoding.UTF8.GetString(data));
        }
    }
}
