// -----------------------------------------------------------------------
//   <copyright file="FieldInfoSerializerFactory.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using Wire.Extensions;
using Wire.ValueSerializers;

namespace Wire.SerializerFactories
{
    public class FieldInfoSerializerFactory : ValueSerializerFactory
    {
        public override bool CanSerialize(Serializer serializer, Type type)
        {
            return type.IsSubclassOf(typeof(FieldInfo));
        }

        public override bool CanDeserialize(Serializer serializer, Type type)
        {
            return CanSerialize(serializer, type);
        }

        public override ValueSerializer BuildSerializer(Serializer serializer, Type type,
            ConcurrentDictionary<Type, ValueSerializer> typeMapping)
        {
            var os = new ObjectSerializer(type);
            typeMapping.TryAdd(type, os);

            static object Reader(Stream stream, DeserializerSession session)
            {
                var name = stream.ReadString(session);
                var owner = stream.ReadObject(session) as Type;


                var field = owner
                    .GetField(name,
                        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return field;
            }

            static void Writer(Stream stream, object obj, SerializerSession session)
            {
                var field = (FieldInfo) obj;
                var name = field.Name;
                var owner = field.DeclaringType;
                StringSerializer.WriteValueImpl(stream, name, session);
                stream.WriteObjectWithManifest(owner, session);
            }

            os.Initialize(Reader, Writer);

            return os;
        }
    }
}