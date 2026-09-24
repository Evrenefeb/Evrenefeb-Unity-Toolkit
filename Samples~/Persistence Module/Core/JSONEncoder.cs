using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Evrenefeb.Toolkit.Persistence {
    /// <summary>
    /// Converts a single Unity value type to/from its JSON-serializable wrapper,
    /// implemented as a real Newtonsoft.Json JsonConverter&lt;T&gt;. One instance
    /// handles exactly one (TOriginal -> TWrapper) pair.
    /// </summary>
    internal sealed class UnityValueJsonConverter<TOriginal, TWrapper> : JsonConverter<TOriginal>
        where TWrapper : struct
    {
        private readonly Func<TOriginal, TWrapper> wrap;
        private readonly Func<TWrapper, TOriginal> unwrap;

        public UnityValueJsonConverter(Func<TOriginal, TWrapper> wrap, Func<TWrapper, TOriginal> unwrap)
        {
            this.wrap = wrap;
            this.unwrap = unwrap;
        }

        public override void WriteJson(JsonWriter writer, TOriginal value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, wrap(value));
        }

        public override TOriginal ReadJson(
            JsonReader reader,
            Type objectType,
            TOriginal existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            var wrapper = serializer.Deserialize<TWrapper>(reader);
            return unwrap(wrapper);
        }
    }

    /// <summary>
    /// Central JSON encode/decode entry point, backed by Newtonsoft.Json.
    ///
    /// Unity value types (Vector3, Color, Quaternion, ...) are handled by a
    /// small, linearly-searched List of JsonConverter instances registered on
    /// JsonSerializerSettings.Converters (no Dictionary&lt;Type,...&gt; lookup
    /// table is used, by design - this mirrors Newtonsoft's own internal
    /// converter-list mechanism).
    ///
    /// List&lt;T&gt;, Dictionary&lt;TKey,TValue&gt;, Queue&lt;T&gt;, Stack&lt;T&gt;, HashSet&lt;T&gt;
    /// and arrays are all supported natively by Newtonsoft.Json, so the old
    /// ListWrapper/DictionaryWrapper/QueueWrapper/HashSetWrapper JsonUtility
    /// workarounds are gone entirely.
    /// </summary>
    internal static class JsonEncoder
    {
        // Linear list on purpose - no Dictionary<Type, ...> lookup table.
        // Guid, DateTime and TimeSpan are NOT listed here: Newtonsoft.Json
        // already serializes them natively (and more standard-compliant
        // than the old SerializableGuid/SerializableDateTime/SerializableTimeSpan
        // wrappers did), so no custom converter is needed for them.
        private static readonly List<JsonConverter> Converters = new List<JsonConverter>
        {
            new UnityValueJsonConverter<Vector2, SerializableVector2>(v => new SerializableVector2(v), s => s.ToVector2()),
            new UnityValueJsonConverter<Vector2Int, SerializableVector2Int>(v => new SerializableVector2Int(v), s => s.ToVector2Int()),
            new UnityValueJsonConverter<Vector3, SerializableVector3>(v => new SerializableVector3(v), s => s.ToVector3()),
            new UnityValueJsonConverter<Vector3Int, SerializableVector3Int>(v => new SerializableVector3Int(v), s => s.ToVector3Int()),
            new UnityValueJsonConverter<Vector4, SerializableVector4>(v => new SerializableVector4(v), s => s.ToVector4()),
            new UnityValueJsonConverter<Quaternion, SerializableQuaternion>(v => new SerializableQuaternion(v), s => s.ToQuaternion()),
            new UnityValueJsonConverter<Color, SerializableColor>(v => new SerializableColor(v), s => s.ToColor()),
            new UnityValueJsonConverter<Color32, SerializableColor32>(v => new SerializableColor32(v), s => s.ToColor32()),
            new UnityValueJsonConverter<Rect, SerializableRect>(v => new SerializableRect(v), s => s.ToRect()),
            new UnityValueJsonConverter<RectInt, SerializableRectInt>(v => new SerializableRectInt(v), s => s.ToRectInt()),
            new UnityValueJsonConverter<Bounds, SerializableBounds>(v => new SerializableBounds(v), s => s.ToBounds()),
            new UnityValueJsonConverter<BoundsInt, SerializableBoundsInt>(v => new SerializableBoundsInt(v), s => s.ToBoundsInt()),
            new UnityValueJsonConverter<Matrix4x4, SerializableMatrix4x4>(v => new SerializableMatrix4x4(v), s => s.ToMatrix4x4()),
            new UnityValueJsonConverter<Hash128, SerializableHash128>(v => new SerializableHash128(v), s => s.ToHash128()),
        };

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = Converters,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public static string Encode(object obj, Type objType, bool prettyPrint = false)
        {
            var formatting = prettyPrint ? Formatting.Indented : Formatting.None;

            // Transform can't be instantiated directly, always special-cased.
            if (objType == typeof(Transform))
                return JsonConvert.SerializeObject(SerializableTransform.FromTransform((Transform)obj), formatting, Settings);

            return JsonConvert.SerializeObject(obj, formatting, Settings);
        }

        public static object Decode(string json, Type targetType)
        {
            if (targetType == typeof(Transform))
                // Transform cannot be created directly - caller receives the SerializableTransform.
                return JsonConvert.DeserializeObject<SerializableTransform>(json, Settings);

            return JsonConvert.DeserializeObject(json, targetType, Settings);
        }
    }
}