using System;
using System.Collections.Generic;
using UnityEngine;

namespace Evrenefeb.Toolkit.Persistence {
    [Serializable]
    public class SerializableTransform
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale;

        public static SerializableTransform FromTransform(Transform t)
        {
            return new SerializableTransform()
            {
                position = t.position,
                rotation = t.eulerAngles,
                scale = t.localScale
            };
        }

        public void ApplyTo(Transform t)
        {
            if (t == null)
            {
                Debug.LogWarning("[SimpleSave] Target Transform is null in SerializableTransform.ApplyTo().");
                return;
            }
            if (float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z) ||
                float.IsNaN(rotation.x) || float.IsNaN(rotation.y) || float.IsNaN(rotation.z) ||
                float.IsNaN(scale.x) || float.IsNaN(scale.y) || float.IsNaN(scale.z))
            {
                Debug.LogWarning("[SimpleSave] Invalid transform data detected in SerializableTransform. Skipping ApplyTo().");
                return;
            }
            t.position = position;
            t.eulerAngles = rotation;
            t.localScale = scale;
        }
    }

    [Serializable]
    public struct SerializableColor
    {
        public float r, g, b, a;
        public SerializableColor(Color c) { r = c.r; g = c.g; b = c.b; a = c.a; }
        public Color ToColor()
        {
            if (float.IsNaN(r) || float.IsNaN(g) || float.IsNaN(b) || float.IsNaN(a))
            {
                Debug.LogWarning("[SimpleSave] Invalid color data detected in SerializableColor. Returning default Color.");
                return default;
            }
            return new Color(r, g, b, a);
        }
    }

    [Serializable]
    public struct SerializableQuaternion
    {
        public float x, y, z, w;
        public SerializableQuaternion(Quaternion q) { x = q.x; y = q.y; z = q.z; w = q.w; }
        public Quaternion ToQuaternion()
        {
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) || float.IsNaN(w))
            {
                Debug.LogWarning("[SimpleSave] Invalid quaternion data detected in SerializableQuaternion. Returning default Quaternion.");
                return default;
            }
            return new Quaternion(x, y, z, w);
        }
    }

    [Serializable]
    public struct SerializableVector2 { public float x, y; public SerializableVector2(Vector2 v) { x = v.x; y = v.y; } public Vector2 ToVector2() { if (float.IsNaN(x) || float.IsNaN(y)) { Debug.LogWarning("[SimpleSave] Invalid vector2 data detected in SerializableVector2. Returning default Vector2."); return default; } return new Vector2(x, y); } }
    [Serializable]
    public struct SerializableVector2Int { public int x, y; public SerializableVector2Int(Vector2Int v) { x = v.x; y = v.y; } public Vector2Int ToVector2Int() { if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue) { Debug.LogWarning("[SimpleSave] Invalid vector2int data detected in SerializableVector2Int. Returning default Vector2Int."); return default; } return new Vector2Int(x, y); } }
    [Serializable]
    public struct SerializableVector3Int { public int x, y, z; public SerializableVector3Int(Vector3Int v) { x = v.x; y = v.y; z = v.z; } public Vector3Int ToVector3Int() { if (x < int.MinValue || x > int.MaxValue || y < int.MinValue || y > int.MaxValue || z < int.MinValue || z > int.MaxValue) { Debug.LogWarning("[SimpleSave] Invalid vector3int data detected in SerializableVector3Int. Returning default Vector3Int."); return default; } return new Vector3Int(x, y, z); } }
    [Serializable]
    public struct SerializableVector4 { public float x, y, z, w; public SerializableVector4(Vector4 v) { x = v.x; y = v.y; z = v.z; w = v.w; } public Vector4 ToVector4() { if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z) || float.IsNaN(w)) { Debug.LogWarning("[SimpleSave] Invalid vector4 data detected in SerializableVector4. Returning default Vector4."); return default; } return new Vector4(x, y, z, w); } }
    [Serializable]
    public struct SerializableColor32 { public byte r, g, b, a; public SerializableColor32(Color32 c) { r = c.r; g = c.g; b = c.b; a = c.a; } public Color32 ToColor32() { if (r > 255 || g > 255 || b > 255 || a > 255) { Debug.LogWarning("[SimpleSave] Invalid color32 data detected in SerializableColor32. Returning default Color32."); return default; } return new Color32(r, g, b, a); } }
    [Serializable]
    public struct SerializableRect { public float x, y, width, height; public SerializableRect(Rect r) { x = r.x; y = r.y; width = r.width; height = r.height; } public Rect ToRect() { if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(width) || float.IsNaN(height)) { Debug.LogWarning("[SimpleSave] Invalid rect data detected in SerializableRect. Returning default Rect."); return default; } return new Rect(x, y, width, height); } }
    [Serializable]
    public struct SerializableRectInt { public int x, y, width, height; public SerializableRectInt(RectInt r) { x = r.x; y = r.y; width = r.width; height = r.height; } public RectInt ToRectInt() { if (width < 0 || height < 0) { Debug.LogWarning("[SimpleSave] Invalid rectInt data detected in SerializableRectInt. Returning default RectInt."); return default; } return new RectInt(x, y, width, height); } }
    [Serializable]
    public struct SerializableBounds { public Vector3 center, size; public SerializableBounds(Bounds b) { center = b.center; size = b.size; } public Bounds ToBounds() { if (float.IsNaN(center.x) || float.IsNaN(center.y) || float.IsNaN(center.z) || float.IsNaN(size.x) || float.IsNaN(size.y) || float.IsNaN(size.z)) { Debug.LogWarning("[SimpleSave] Invalid bounds data detected in SerializableBounds. Returning default Bounds."); return default; } return new Bounds(center, size); } }
    [Serializable]
    public struct SerializableBoundsInt { public Vector3Int position, size; public SerializableBoundsInt(BoundsInt b) { position = b.position; size = b.size; } public BoundsInt ToBoundsInt() { if (size.x < 0 || size.y < 0 || size.z < 0) { Debug.LogWarning("[SimpleSave] Invalid boundsInt data detected in SerializableBoundsInt. Returning default BoundsInt."); return default; } return new BoundsInt(position, size); } }
    [Serializable]
    public struct SerializableMatrix4x4 { public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33; public SerializableMatrix4x4(Matrix4x4 m) { m00 = m.m00; m01 = m.m01; m02 = m.m02; m03 = m.m03; m10 = m.m10; m11 = m.m11; m12 = m.m12; m13 = m.m13; m20 = m.m20; m21 = m.m21; m22 = m.m22; m23 = m.m23; m30 = m.m30; m31 = m.m31; m32 = m.m32; m33 = m.m33; } public Matrix4x4 ToMatrix4x4() { if (float.IsNaN(m00) || float.IsNaN(m01) || float.IsNaN(m02) || float.IsNaN(m03) || float.IsNaN(m10) || float.IsNaN(m11) || float.IsNaN(m12) || float.IsNaN(m13) || float.IsNaN(m20) || float.IsNaN(m21) || float.IsNaN(m22) || float.IsNaN(m23) || float.IsNaN(m30) || float.IsNaN(m31) || float.IsNaN(m32) || float.IsNaN(m33)) { Debug.LogWarning("[SimpleSave] Invalid matrix data detected in SerializableMatrix4x4. Returning default Matrix4x4."); return default; } return new Matrix4x4 { m00 = m00, m01 = m01, m02 = m02, m03 = m03, m10 = m10, m11 = m11, m12 = m12, m13 = m13, m20 = m20, m21 = m21, m22 = m22, m23 = m23, m30 = m30, m31 = m31, m32 = m32, m33 = m33 }; } }
    [Serializable]
    public struct SerializableDateTime { public long ticks; public SerializableDateTime(DateTime dt) { ticks = dt.Ticks; } public DateTime ToDateTime() { if (ticks < 0) { Debug.LogWarning("[SimpleSave] Invalid DateTime data detected in SerializableDateTime. Returning default DateTime."); return default; } return new DateTime(ticks); } }
    [Serializable]
    public struct SerializableTimeSpan { public long ticks; public SerializableTimeSpan(TimeSpan ts) { ticks = ts.Ticks; } public TimeSpan ToTimeSpan() { if (ticks < 0) { Debug.LogWarning("[SimpleSave] Invalid TimeSpan data detected in SerializableTimeSpan. Returning default TimeSpan."); return default; } return new TimeSpan(ticks); } }
    [Serializable]
    public struct SerializableGuid { public string value; public SerializableGuid(Guid g) { value = g.ToString(); } public Guid ToGuid() { if (string.IsNullOrEmpty(value)) { Debug.LogWarning("[SimpleSave] Invalid Guid data detected in SerializableGuid. Returning default Guid."); return default; } return new Guid(value); } }
    [Serializable]
    public struct SerializableHash128 { public string value; public SerializableHash128(UnityEngine.Hash128 h) { value = h.ToString(); } public UnityEngine.Hash128 ToHash128() { if (string.IsNullOrEmpty(value)) { Debug.LogWarning("[SimpleSave] Invalid Hash128 data detected in SerializableHash128. Returning default Hash128."); return default; } return UnityEngine.Hash128.Parse(value); } }
    [Serializable]
    public struct SerializableVector3 { public float x, y, z; public SerializableVector3(Vector3 v) { x = v.x; y = v.y; z = v.z; } public Vector3 ToVector3() { if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(z)) { Debug.LogWarning("[SimpleSave] Invalid vector3 data detected in SerializableVector3. Returning default Vector3."); return default; } return new Vector3(x, y, z); } }

    [Serializable]
    public class SerializableDictionary<TKey, TValue>
    {
        public List<TKey> keys = new List<TKey>();
        public List<TValue> values = new List<TValue>();

        public SerializableDictionary() { }
        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            foreach (var kv in dict)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }
        public Dictionary<TKey, TValue> ToDictionary()
        {
            if (keys == null || values == null || keys.Count != values.Count)
            {
                Debug.LogWarning("[SimpleSave] Invalid dictionary data detected in SerializableDictionary. Returning empty dictionary.");
                return new Dictionary<TKey, TValue>();
            }
            var dict = new Dictionary<TKey, TValue>();
            for (int i = 0; i < keys.Count; i++)
                dict[keys[i]] = values[i];
            return dict;
        }
    }

}