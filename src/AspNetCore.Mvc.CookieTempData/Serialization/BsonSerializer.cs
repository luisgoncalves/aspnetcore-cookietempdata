﻿using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace AspNetCore.Mvc.CookieTempData.Serialization
{
    /// <summary>
    /// An <see cref="IBsonSerializer"/> based on JSON.NET.
    /// </summary>
    public class BsonSerializer : IBsonSerializer
    {
        private readonly JsonSerializer _serializer;

        public BsonSerializer()
        {
            _serializer = new JsonSerializer
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            };
        }

        public byte[] Serialize(object obj)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BsonWriter(ms))
                {
                    _serializer.Serialize(writer, obj);
                }
                return ms.ToArray();
            }
        }

        public T Deserialize<T>(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            using (var reader = new BsonReader(ms))
            {
                return _serializer.Deserialize<T>(reader);
            }
        }
    }
}
