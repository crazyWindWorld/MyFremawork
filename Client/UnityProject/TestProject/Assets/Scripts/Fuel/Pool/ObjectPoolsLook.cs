using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fuel.Pools
{
    [Serializable]
    public class EditorPoolInfo : IEquatable<EditorPoolInfo>
    {
        public string Name;
        public RuntimeTypeHandle TypeHandle;
        public int CreateCount;
        public int RecycelCount;
        public int UseCount;

        public void InitData(string name,RuntimeTypeHandle handle)
        {
            TypeHandle = handle;
            Name = name;
            CreateCount = 0;
            RecycelCount = 0;
            UseCount = 0;
        }

        public void Create(bool isAdd)
        {
            CreateCount = isAdd? CreateCount + 1 : CreateCount - 1;
        }

        public void Recycle(bool isAdd)
        {
            RecycelCount = isAdd ? RecycelCount + 1 : RecycelCount - 1;
        }

        public void Use(bool isAdd)
        {
            UseCount = isAdd? UseCount + 1 : UseCount - 1;
        }

        public bool Equals(EditorPoolInfo other)
        {
            return TypeHandle.Equals(other.TypeHandle);
        }

        public override bool Equals(object obj)
        {
            return obj is EditorPoolInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return TypeHandle.GetHashCode();
        }
    }

    public class ObjectPoolsLook : UnityEngine.MonoBehaviour
    {
        private Dictionary<string, EditorPoolInfo> _poolInfo;
        public List<EditorPoolInfo> _lookList;

        public void Init(Dictionary<string, EditorPoolInfo> poolInfo)
        {
            _poolInfo = poolInfo;
            _lookList = new List<EditorPoolInfo>();
        }

        private void Update()
        {
            if (_poolInfo == null) return;
            foreach (var (key, value) in _poolInfo)
            {
                if (!_lookList.Contains(value))
                {
                    _lookList.Add(value);
                }
            }
        }
    }
}