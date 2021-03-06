﻿using System;
using Newtonsoft.Json;
using GodelTech.Owasp.InsecureDeserialization.Interfaces;

namespace GodelTech.Owasp.InsecureDeserialization.Implementations
{
    public class InsecureNewtonSoftJsonDeserializer<T> : IInsecureDeserializer<string, T>
    {
        public T Deserialize(string dataToDeserialize)
        {
            if (dataToDeserialize == null)
            {
                throw new ArgumentNullException(nameof(dataToDeserialize));
            }

            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            };

            return JsonConvert.DeserializeObject<T>(dataToDeserialize, serializerSettings);
        }
    }
}