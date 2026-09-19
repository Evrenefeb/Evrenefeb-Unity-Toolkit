using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Evrenefeb.Toolkit.Persistence
{
    /// <summary>
    /// Writes/reads a single leaf value type to/from the binary stream.
    /// One instance handles exactly one primitive/struct type.
    /// </summary>
    internal interface IBinaryValueConverter
    {
        Type TargetType { get; }
        void Write(object value, BinaryWriter writer);
        object Read(BinaryReader reader);
    }

    internal sealed class BinaryValueConverter<T> : IBinaryValueConverter
    {
        private readonly Action<T, BinaryWriter> write;
        private readonly Func<BinaryReader, T> read;

        public BinaryValueConverter(Action<T, BinaryWriter> write, Func<BinaryReader, T> read)
        {
            this.write = write;
            this.read = read;
        }

        public Type TargetType => typeof(T);
        public void Write(object value, BinaryWriter writer) => write((T)value, writer);
        public object Read(BinaryReader reader) => read(reader);
    }

    /// <summary>
    /// Central binary leaf-type encode/decode entry point. Replaces the old
    /// per-type if/else chains in WriteObject/ReadObject with a small,
    /// linearly-searched List of converters (no Dictionary is used, by design).
    /// Collections, custom classes and the null-marker protocol stay in
    /// SimpleSaver.WriteObject/ReadObject since they are structural, not
    /// type-specific.
    /// </summary>
    internal static class BinaryEncoder
    {
        // Linear list on purpose - no Dictionary<Type, ...> lookup table.
        private static readonly List<IBinaryValueConverter> Converters = new List<IBinaryValueConverter>
        {
            new BinaryValueConverter<int>((v, w) => w.Write(v), r => r.ReadInt32()),
            new BinaryValueConverter<float>((v, w) => w.Write(v), r => r.ReadSingle()),
            new BinaryValueConverter<bool>((v, w) => w.Write(v), r => r.ReadBoolean()),
            new BinaryValueConverter<string>((v, w) => w.Write(v), r => r.ReadString()),

            new BinaryValueConverter<Vector2>(
                (v, w) => { w.Write(v.x); w.Write(v.y); },
                r => new Vector2(r.ReadSingle(), r.ReadSingle())),

            new BinaryValueConverter<Vector2Int>(
                (v, w) => { w.Write(v.x); w.Write(v.y); },
                r => new Vector2Int(r.ReadInt32(), r.ReadInt32())),

            new BinaryValueConverter<Vector3>(
                (v, w) => { w.Write(v.x); w.Write(v.y); w.Write(v.z); },
                r => new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle())),

            new BinaryValueConverter<Vector3Int>(
                (v, w) => { w.Write(v.x); w.Write(v.y); w.Write(v.z); },
                r => new Vector3Int(r.ReadInt32(), r.ReadInt32(), r.ReadInt32())),

            new BinaryValueConverter<Vector4>(
                (v, w) => { w.Write(v.x); w.Write(v.y); w.Write(v.z); w.Write(v.w); },
                r => new Vector4(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle())),

            new BinaryValueConverter<Color>(
                (v, w) => { w.Write(v.r); w.Write(v.g); w.Write(v.b); w.Write(v.a); },
                r => new Color(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle())),

            new BinaryValueConverter<Color32>(
                (v, w) => { w.Write(v.r); w.Write(v.g); w.Write(v.b); w.Write(v.a); },
                r => new Color32(r.ReadByte(), r.ReadByte(), r.ReadByte(), r.ReadByte())),

            new BinaryValueConverter<Quaternion>(
                (v, w) => { w.Write(v.x); w.Write(v.y); w.Write(v.z); w.Write(v.w); },
                r => new Quaternion(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle())),

            new BinaryValueConverter<Rect>(
                (v, w) => { w.Write(v.x); w.Write(v.y); w.Write(v.width); w.Write(v.height); },
                r => new Rect(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle())),

            new BinaryValueConverter<RectInt>(
                (v, w) => { w.Write(v.x); w.Write(v.y); w.Write(v.width); w.Write(v.height); },
                r => new RectInt(r.ReadInt32(), r.ReadInt32(), r.ReadInt32(), r.ReadInt32())),

            new BinaryValueConverter<Bounds>(
                (v, w) =>
                {
                    w.Write(v.center.x); w.Write(v.center.y); w.Write(v.center.z);
                    w.Write(v.size.x); w.Write(v.size.y); w.Write(v.size.z);
                },
                r => new Bounds(
                    new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()),
                    new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle()))),

            new BinaryValueConverter<BoundsInt>(
                (v, w) =>
                {
                    w.Write(v.position.x); w.Write(v.position.y); w.Write(v.position.z);
                    w.Write(v.size.x); w.Write(v.size.y); w.Write(v.size.z);
                },
                r => new BoundsInt(
                    new Vector3Int(r.ReadInt32(), r.ReadInt32(), r.ReadInt32()),
                    new Vector3Int(r.ReadInt32(), r.ReadInt32(), r.ReadInt32()))),

            new BinaryValueConverter<Matrix4x4>(
                (v, w) =>
                {
                    w.Write(v.m00); w.Write(v.m01); w.Write(v.m02); w.Write(v.m03);
                    w.Write(v.m10); w.Write(v.m11); w.Write(v.m12); w.Write(v.m13);
                    w.Write(v.m20); w.Write(v.m21); w.Write(v.m22); w.Write(v.m23);
                    w.Write(v.m30); w.Write(v.m31); w.Write(v.m32); w.Write(v.m33);
                },
                r => new Matrix4x4
                {
                    m00 = r.ReadSingle(), m01 = r.ReadSingle(), m02 = r.ReadSingle(), m03 = r.ReadSingle(),
                    m10 = r.ReadSingle(), m11 = r.ReadSingle(), m12 = r.ReadSingle(), m13 = r.ReadSingle(),
                    m20 = r.ReadSingle(), m21 = r.ReadSingle(), m22 = r.ReadSingle(), m23 = r.ReadSingle(),
                    m30 = r.ReadSingle(), m31 = r.ReadSingle(), m32 = r.ReadSingle(), m33 = r.ReadSingle(),
                }),

            new BinaryValueConverter<DateTime>((v, w) => w.Write(v.Ticks), r => new DateTime(r.ReadInt64())),
            new BinaryValueConverter<TimeSpan>((v, w) => w.Write(v.Ticks), r => new TimeSpan(r.ReadInt64())),
            new BinaryValueConverter<Guid>((v, w) => w.Write(v.ToString()), r => new Guid(r.ReadString())),
            new BinaryValueConverter<Hash128>((v, w) => w.Write(v.ToString()), r => Hash128.Parse(r.ReadString())),
        };

        private static IBinaryValueConverter Find(Type type) => Converters.Find(c => c.TargetType == type);

        /// <summary>Attempts to write a leaf (primitive/struct) value. Returns false if the type is not a leaf type.</summary>
        public static bool TryWrite(object value, Type type, BinaryWriter writer)
        {
            var converter = Find(type);
            if (converter == null) return false;
            converter.Write(value, writer);
            return true;
        }

        /// <summary>Attempts to read a leaf (primitive/struct) value. Returns false if the type is not a leaf type.</summary>
        public static bool TryRead(Type type, BinaryReader reader, out object value)
        {
            var converter = Find(type);
            if (converter == null)
            {
                value = null;
                return false;
            }

            value = converter.Read(reader);
            return true;
        }
    }
}