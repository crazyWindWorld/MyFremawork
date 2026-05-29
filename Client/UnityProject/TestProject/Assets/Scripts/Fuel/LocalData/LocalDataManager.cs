using System;
using System.IO;
using System.Text;
using Fuel.Singleton;
using UnityEngine;

namespace Fuel.LocalData
{
    public enum LocalDataStorageType
    {
        PlayerPrefs,
        JsonFile,
        BinaryFile
    }

    public interface ILocalDataStorage
    {
        void SaveString(string key, string value);
        bool TryLoadString(string key, out string value);
        void Delete(string key);
        bool HasKey(string key);
    }

    public sealed class LocalDataManager : Singleton<LocalDataManager>
    {
        private readonly PlayerPrefsLocalDataStorage _playerPrefsStorage = new PlayerPrefsLocalDataStorage();
        private readonly JsonFileLocalDataStorage _jsonFileStorage = new JsonFileLocalDataStorage();
        private readonly BinaryFileLocalDataStorage _binaryFileStorage = new BinaryFileLocalDataStorage();

        public LocalDataStorageType StorageType { get; private set; } = LocalDataStorageType.JsonFile;
        public bool EncryptionEnabled { get; private set; }
        public string EncryptionKey { get; private set; } = "FuelLocalData";

        public void SetStorageType(LocalDataStorageType storageType)
        {
            StorageType = storageType;
        }

        public void SetEncryption(bool enabled, string key = null)
        {
            EncryptionEnabled = enabled;
            if (!string.IsNullOrEmpty(key))
            {
                EncryptionKey = key;
            }
        }

        public void Save<T>(string key, T data)
        {
            SaveString(key, JsonUtility.ToJson(new LocalDataWrapper<T> { data = data }));
        }

        public bool TryLoad<T>(string key, out T data)
        {
            if (TryLoadString(key, out var json))
            {
                var wrapper = JsonUtility.FromJson<LocalDataWrapper<T>>(json);
                data = wrapper.data;
                return true;
            }

            data = default;
            return false;
        }

        public void SaveString(string key, string value)
        {
            GetStorage(StorageType).SaveString(key, EncodeValue(value));
        }

        public bool TryLoadString(string key, out string value)
        {
            if (GetStorage(StorageType).TryLoadString(key, out var storedValue))
            {
                value = DecodeValue(storedValue);
                return true;
            }

            value = null;
            return false;
        }

        public void Delete(string key)
        {
            GetStorage(StorageType).Delete(key);
        }

        public bool HasKey(string key)
        {
            return GetStorage(StorageType).HasKey(key);
        }

        public ILocalDataStorage GetStorage(LocalDataStorageType storageType)
        {
            switch (storageType)
            {
                case LocalDataStorageType.PlayerPrefs:
                    return _playerPrefsStorage;
                case LocalDataStorageType.JsonFile:
                    return _jsonFileStorage;
                case LocalDataStorageType.BinaryFile:
                    return _binaryFileStorage;
                default:
                    return _jsonFileStorage;
            }
        }

        private string EncodeValue(string value)
        {
            return EncryptionEnabled ? XorObfuscator.Encode(value, EncryptionKey) : value;
        }

        private string DecodeValue(string value)
        {
            return EncryptionEnabled ? XorObfuscator.Decode(value, EncryptionKey) : value;
        }

        [Serializable]
        private struct LocalDataWrapper<T>
        {
            public T data;
        }
    }

    public static class XorObfuscator
    {
        public static string Encode(string value, string key)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            Apply(bytes, key);
            return Convert.ToBase64String(bytes);
        }

        public static string Decode(string value, string key)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var bytes = Convert.FromBase64String(value);
            Apply(bytes, key);
            return Encoding.UTF8.GetString(bytes);
        }

        private static void Apply(byte[] bytes, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(key) ? "FuelLocalData" : key);
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ keyBytes[i % keyBytes.Length]);
            }
        }
    }

    public sealed class PlayerPrefsLocalDataStorage : ILocalDataStorage
    {
        public void SaveString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public bool TryLoadString(string key, out string value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetString(key);
                return true;
            }

            value = null;
            return false;
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }
    }

    public abstract class FileLocalDataStorage : ILocalDataStorage
    {
        private readonly string _extension;
        private readonly string _directory;

        protected FileLocalDataStorage(string extension)
        {
            _extension = extension;
            _directory = Path.Combine(Application.persistentDataPath, "LocalData");
        }

        public void SaveString(string key, string value)
        {
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }

            File.WriteAllBytes(GetFilePath(key), Encode(value));
        }

        public bool TryLoadString(string key, out string value)
        {
            var path = GetFilePath(key);
            if (File.Exists(path))
            {
                value = Decode(File.ReadAllBytes(path));
                return true;
            }

            value = null;
            return false;
        }

        public void Delete(string key)
        {
            var path = GetFilePath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public bool HasKey(string key)
        {
            return File.Exists(GetFilePath(key));
        }

        protected abstract byte[] Encode(string value);
        protected abstract string Decode(byte[] bytes);

        private string GetFilePath(string key)
        {
            return Path.Combine(_directory, GetSafeFileName(key) + _extension);
        }

        private static string GetSafeFileName(string key)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(key.Length);
            for (int i = 0; i < key.Length; i++)
            {
                var c = key[i];
                builder.Append(Array.IndexOf(invalidChars, c) >= 0 ? '_' : c);
            }
            return builder.ToString();
        }
    }

    public sealed class JsonFileLocalDataStorage : FileLocalDataStorage
    {
        public JsonFileLocalDataStorage() : base(".json")
        {
        }

        protected override byte[] Encode(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        protected override string Decode(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }

    public sealed class BinaryFileLocalDataStorage : FileLocalDataStorage
    {
        public BinaryFileLocalDataStorage() : base(".bytes")
        {
        }

        protected override byte[] Encode(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        protected override string Decode(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
