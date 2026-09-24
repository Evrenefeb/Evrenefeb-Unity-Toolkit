using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Evrenefeb.Toolkit.Persistence
{
    /// <summary>
    /// Storage format
    /// </summary>
    public enum SaveFormat
    {
        Binary,
        JSON,
        PlayerPrefs
    }

    public static class PersistenceManager
    {
        /// <summary>
        /// Saves an object with the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the object to save.</typeparam>
        /// <param name="key">Unique key for the saved data.</param>
        /// <param name="obj">Object to save.</param>
        /// <param name="format">Storage format to use.</param>
        /// <param name="useEncryption">Whether to AES-encrypt the saved data.</param>
        /// <param name="encryptionPassword">Password used when useEncryption is true.</param>
        /// <param name="jsonPrettyPrint">When format is Json, formats the output with indentation for readability. Ignored for other formats.</param>


        public static void Save<T>(
            string key,
            T obj,
            SaveFormat format = SaveFormat.Binary,
            bool useEncryption = false,
            string encryptionPassword = null,
            bool jsonPrettyPrint = false)
        {
            switch (format)
            {
                case SaveFormat.JSON:
                    SaveJson(key, obj, useEncryption, encryptionPassword, jsonPrettyPrint);
                    break;
                case SaveFormat.PlayerPrefs:
                    SaveBinaryPlayerPrefs(key, obj, useEncryption, encryptionPassword);
                    break;
                default:
                    SaveBinary(key, obj, useEncryption, encryptionPassword);
                    break;
            }
        }

        /// <summary>
        /// Loads an object of type T by key.
        /// </summary>
        /// <typeparam name="T">Type of the object to load.</typeparam>
        /// <param name="key">Unique key for the saved data.</param>
        /// <param name="format">Storage format the data was saved with.</param>
        /// <param name="useEncryption">Whether the saved data is AES-encrypted.</param>
        /// <param name="encryptionPassword">Password used when useEncryption is true.</param>
        /// <returns>The loaded object, or default(T) if not found or on error.</returns>
        /// <example>
        
        public static T Load<T>(
            string key,
            SaveFormat format = SaveFormat.Binary,
            bool useEncryption = false,
            string encryptionPassword = null)
        {
            switch (format)
            {
                case SaveFormat.JSON:
                    return LoadJson<T>(key, useEncryption, encryptionPassword);
                case SaveFormat.PlayerPrefs:
                    return LoadBinaryPlayerPrefs<T>(key, useEncryption, encryptionPassword);
                default:
                    return LoadBinary<T>(key, useEncryption, encryptionPassword);
            }
        }

        /// <summary>
        /// Resolves the file path used for a given key when saving to disk
        /// (Binary/Json formats). PlayerPrefs format does not use this.
        /// </summary>
        private static string GetPath(string key, string extension = ".sav") => Path.Combine(Application.persistentDataPath, key + extension);

        private static void SaveBinary<T>(string key, T obj, bool useEncryption, string encryptionPassword)
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(key);
                    WriteObject(obj, writer);
                    byte[] data = ms.ToArray();
                    if (useEncryption && !string.IsNullOrEmpty(encryptionPassword))
                        data = Encrypt(data, encryptionPassword);
                    File.WriteAllBytes(GetPath(key), data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Failed to save binary for key '{key}': {ex.Message}");
            }
        }

        private static T LoadBinary<T>(string key, bool useEncryption, string encryptionPassword)
        {
            string path = GetPath(key);
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] File not found for key '{key}'");
                return default;
            }

            byte[] data = File.ReadAllBytes(path);
            if (useEncryption && !string.IsNullOrEmpty(encryptionPassword))
                data = Decrypt(data, encryptionPassword);
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                string storedKey = reader.ReadString();
                if (storedKey != key)
                {
                    Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Key mismatch: expected '{key}', got '{storedKey}'");
                    return default;
                }

                try
                {
                    return (T)ReadObject(typeof(T), reader);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Failed to load object for key '{key}': {ex.Message}");
                    return default;
                }
            }
        }

        private static void SaveJson<T>(string key, T obj, bool useEncryption, string encryptionPassword, bool prettyPrint = false)
        {
            try
            {
                string json = JsonEncoder.Encode(obj, typeof(T), prettyPrint);
                byte[] data = Encoding.UTF8.GetBytes(json);
                if (useEncryption && !string.IsNullOrEmpty(encryptionPassword))
                    data = Encrypt(data, encryptionPassword);
                File.WriteAllBytes(GetPath(key, ".json"), data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Failed to save JSON for key '{key}': {ex.Message}");
            }
        }

        private static T LoadJson<T>(string key, bool useEncryption, string encryptionPassword)
        {
            string path = GetPath(key, ".json");
            if (!File.Exists(path))
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] JSON file not found for key '{key}'");
                return default;
            }

            byte[] data = File.ReadAllBytes(path);
            if (useEncryption && !string.IsNullOrEmpty(encryptionPassword))
                data = Decrypt(data, encryptionPassword);
            string json = Encoding.UTF8.GetString(data);
            try
            {
                object result = JsonEncoder.Decode(json, typeof(T));
                return (T)result;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Failed to load JSON for key '{key}': {ex.Message}");
                return default;
            }
        }

        private static void SaveBinaryPlayerPrefs<T>(string key, T obj, bool useEncryption, string encryptionPassword)
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(key);
                    WriteObject(obj, writer);
                    byte[] data = ms.ToArray();
                    if (useEncryption && !string.IsNullOrEmpty(encryptionPassword))
                        data = Encrypt(data, encryptionPassword);
                    string base64 = Convert.ToBase64String(data);
                    PlayerPrefs.SetString(key, base64);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Failed to save PlayerPrefs for key '{key}': {ex.Message}");
            }
        }

        private static T LoadBinaryPlayerPrefs<T>(string key, bool useEncryption, string encryptionPassword)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] PlayerPrefs key not found: '{key}'");
                return default;
            }

            string base64 = PlayerPrefs.GetString(key);
            byte[] data = Convert.FromBase64String(base64);
            if (useEncryption && !string.IsNullOrEmpty(encryptionPassword))
                data = Decrypt(data, encryptionPassword);
            using (var ms = new MemoryStream(data))
            using (var reader = new BinaryReader(ms))
            {
                string storedKey = reader.ReadString();
                if (storedKey != key)
                {
                    Debug.LogWarning($"[Evrenefeb.Toolkit.Persistence] Key mismatch in PlayerPrefs: expected '{key}', got '{storedKey}'");
                    return default;
                }

                try
                {
                    return (T)ReadObject(typeof(T), reader);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(
                        $"[Evrenefeb.Toolkit.Persistence] Failed to load object from PlayerPrefs for key '{key}': {ex.Message}");
                    return default;
                }
            }
        }

        // --- BINARY SERIALIZER ---
        // Leaf (primitive/struct) types are delegated to BinaryEncoder (see BinaryEncoder.cs),
        // which uses a small linearly-searched List<IBinaryValueConverter> - no Dictionary lookup table.
        // Collections, custom classes and the null-marker protocol stay here since they are
        // structural concerns, not per-type concerns.
        private static void WriteObject(object obj, BinaryWriter writer)
        {
            if (obj == null)
            {
                writer.Write((byte)0); // null marker
                return;
            }

            Type type = obj.GetType();

            // Structures — do not write null-marker
            if (BinaryEncoder.TryWrite(obj, type, writer))
                return;

            // For classes, write null-marker
            writer.Write((byte)1); // not null marker for class

            if (type == typeof(SerializableTransform))
            {
                var st = (SerializableTransform)obj;
                WriteObject(st.position, writer);
                WriteObject(st.rotation, writer);
                WriteObject(st.scale, writer);
                return;
            }

            if (type == typeof(Transform))
            {
                var st = SerializableTransform.FromTransform((Transform)obj);
                WriteObject(st.position, writer);
                WriteObject(st.rotation, writer);
                WriteObject(st.scale, writer);
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var list = (System.Collections.IList)obj;
                writer.Write(list.Count);
                foreach (var item in list)
                    WriteObject(item, writer);
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var dict = (System.Collections.IDictionary)obj;
                writer.Write(dict.Count);
                foreach (System.Collections.DictionaryEntry kvp in dict)
                {
                    WriteObject(kvp.Key, writer);
                    WriteObject(kvp.Value, writer);
                }

                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var queue = (System.Collections.IEnumerable)obj;
                var list = new List<object>();
                foreach (var item in queue)
                    list.Add(item);
                writer.Write(list.Count);
                foreach (var item in list)
                    WriteObject(item, writer);
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>))
            {
                var stack = (System.Collections.IEnumerable)obj;
                var list = new List<object>();
                foreach (var item in stack)
                    list.Add(item);
                writer.Write(list.Count);
                foreach (var item in list)
                    WriteObject(item, writer);
                return;
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                var arr = (System.Array)obj;
                writer.Write(arr.Length);
                var elemType = type.GetElementType();
                for (int i = 0; i < arr.Length; i++)
                    WriteObject(arr.GetValue(i), writer);
                return;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                var set = (System.Collections.IEnumerable)obj;
                var list = new List<object>();
                foreach (var item in set)
                    list.Add(item);
                writer.Write(list.Count);
                foreach (var item in list)
                    WriteObject(item, writer);
                return;
            }

            // Custom classes
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)) continue;
                if (typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                object value = field.GetValue(obj);
                WriteObject(value, writer);
            }
        }

        private static object ReadObject(Type type, BinaryReader reader)
        {
            // Structures — do not read null-marker
            if (BinaryEncoder.TryRead(type, reader, out object leafValue))
                return leafValue;

            byte nullMarker = reader.ReadByte();
            if (nullMarker == 0) return null;

            if (type == typeof(SerializableTransform))
            {
                var st = new SerializableTransform();
                st.position = (Vector3)ReadObject(typeof(Vector3), reader);
                st.rotation = (Vector3)ReadObject(typeof(Vector3), reader);
                st.scale = (Vector3)ReadObject(typeof(Vector3), reader);
                return st;
            }

            if (type == typeof(Transform))
            {
                var st = (SerializableTransform)ReadObject(typeof(SerializableTransform), reader);
                return st;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                int count = reader.ReadInt32();
                var list = (System.Collections.IList)Activator.CreateInstance(type);
                Type itemType = type.GetGenericArguments()[0];
                for (int i = 0; i < count; i++)
                    list.Add(ReadObject(itemType, reader));
                return list;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                int count = reader.ReadInt32();
                var dict = (System.Collections.IDictionary)Activator.CreateInstance(type);
                Type keyType = type.GetGenericArguments()[0];
                Type valType = type.GetGenericArguments()[1];
                for (int i = 0; i < count; i++)
                {
                    var key = ReadObject(keyType, reader);
                    var val = ReadObject(valType, reader);
                    dict.Add(key, val);
                }

                return dict;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>))
            {
                int count = reader.ReadInt32();
                Type itemType = type.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
                for (int i = 0; i < count; i++)
                    list.Add(ReadObject(itemType, reader));
                var queueObj = Activator.CreateInstance(type, list);
                return queueObj;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>))
            {
                int count = reader.ReadInt32();
                Type itemType = type.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType));
                for (int i = 0; i < count; i++)
                    list.Add(ReadObject(itemType, reader));
                var stackObj = Activator.CreateInstance(type, list);
                return stackObj;
            }

            if (type.IsArray && type.GetArrayRank() == 1)
            {
                int count = reader.ReadInt32();
                Type elemType = type.GetElementType();
                var arr = Array.CreateInstance(elemType, count);
                for (int i = 0; i < count; i++)
                    arr.SetValue(ReadObject(elemType, reader), i);
                return arr;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                int count = reader.ReadInt32();
                Type itemType = type.GetGenericArguments()[0];
                var hashSet = Activator.CreateInstance(type);
                var addMethod = type.GetMethod("Add");
                for (int i = 0; i < count; i++)
                {
                    var item = ReadObject(itemType, reader);
                    addMethod.Invoke(hashSet, new object[] { item });
                }

                return hashSet;
            }

            // Custom classes
            object instance = Activator.CreateInstance(type);
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                if (field.IsNotSerialized) continue;
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)) continue;
                if (typeof(Delegate).IsAssignableFrom(field.FieldType)) continue;
                object val = ReadObject(field.FieldType, reader);
                field.SetValue(instance, val);
            }

            return instance;
        }

        private static byte[] Encrypt(byte[] data, string password)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("PersistenceSalt123"));
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                    return ms.ToArray();
                }
            }
        }

        private static byte[] Decrypt(byte[] data, string password)
        {
            using (var aes = Aes.Create())
            {
                var key = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes("PersistenceSalt123"));
                aes.Key = key.GetBytes(32);
                aes.IV = key.GetBytes(16);

                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(data, 0, data.Length);
                    cs.Close();
                    return ms.ToArray();
                }
            }
        }

        // NOTE: ListWrapper / DictionaryWrapper / QueueWrapper / HashSetWrapper
        // live in JsonEncoder.cs (they are JSON-only concerns).

        /// <summary>
        /// Loads saved transform data and applies it to the specified Transform.
        /// </summary>
        /// <param name="key">Unique key for the saved data.</param>
        /// <param name="target">Target Transform to apply loaded data to.</param>
        /// <param name="format">Storage format the data was saved with.</param>
        /// <param name="useEncryption">Whether the saved data is AES-encrypted.</param>
        /// <param name="encryptionPassword">Password used when useEncryption is true.</param>

        public static void LoadInto(
            string key,
            Transform target,
            SaveFormat format = SaveFormat.Binary,
            bool useEncryption = false,
            string encryptionPassword = null)
        {
            var st = Load<SerializableTransform>(key, format, useEncryption, encryptionPassword);
            if (st != null)
                st.ApplyTo(target);
        }

        /// <summary>
        /// Asynchronously saves an object with the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the object to save.</typeparam>
        /// <param name="key">Unique key for the saved data.</param>
        /// <param name="obj">Object to save.</param>
        /// <param name="format">Storage format to use.</param>
        /// <param name="useEncryption">Whether to AES-encrypt the saved data.</param>
        /// <param name="encryptionPassword">Password used when useEncryption is true.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        /// <example>

        public static async Task SaveAsync<T>(
            string key,
            T obj,
            SaveFormat format = SaveFormat.Binary,
            bool useEncryption = false,
            string encryptionPassword = null,
            bool jsonPrettyPrint = false)
        {
#if UNITY_WEBGL
            Save(key, obj, format, useEncryption, encryptionPassword, jsonPrettyPrint);
#else
            await Task.Run(() => Save(key, obj, format, useEncryption, encryptionPassword, jsonPrettyPrint));
#endif
        }

        /// <summary>
        /// Asynchronously loads an object of type T by key.
        /// </summary>
        /// <typeparam name="T">Type of the object to load.</typeparam>
        /// <param name="key">Unique key for the saved data.</param>
        /// <param name="format">Storage format the data was saved with.</param>
        /// <param name="useEncryption">Whether the saved data is AES-encrypted.</param>
        /// <param name="encryptionPassword">Password used when useEncryption is true.</param>
        /// <returns>The loaded object, or default(T) if not found or on error.</returns>
        /// <example>

        public static async Task<T> LoadAsync<T>(
            string key,
            SaveFormat format = SaveFormat.Binary,
            bool useEncryption = false,
            string encryptionPassword = null)
        {
#if UNITY_WEBGL
            return Load<T>(key, format, useEncryption, encryptionPassword);
#else
            return await Task.Run(() => Load<T>(key, format, useEncryption, encryptionPassword));
#endif
        }
    }
}