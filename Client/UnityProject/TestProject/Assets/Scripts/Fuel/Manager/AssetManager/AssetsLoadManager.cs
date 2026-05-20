using System;
using Cysharp.Threading.Tasks;
using HotFarmework.AssetManager;
using HotFramework.AssetManager.AssetsPools;
using Fuel.Singleton;
using UnityEngine;
using UnityEngine.Events;
using YooAsset;
using Fuel.AssetManager.AssetsPools;
using Fuel.Log;

namespace Fuel.AssetManager
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AssetsLoadManager : Singleton<AssetsLoadManager>
    {
        private UnityEvent _destroyEvent;
        private UnityEvent _disposeEvent;
        public static bool UseAsyncLoad { get; set; }

        protected void RegistrationLife()
        {
            _destroyEvent = new UnityEvent();
            _disposeEvent = new UnityEvent();
        }

        protected void OnEnable()
        {
            UseAsyncLoad = true;
            //UseAsyncLoad = Application.platform == RuntimePlatform.WebGLPlayer;
        }

        #region 程序集内部调用加载接口
        internal async UniTask<T> InternalLoadAsync<T>(string path, string groupName = "") where T : UnityEngine.Object
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var obj = await group.LoadAsync<T>(path);
            return obj;
        }

        internal async UniTask<UnityEngine.Object> InternalLoadAsync(string path, Type type, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var obj = await group.LoadAsync(path, type);
            return obj;
        }

        internal async UniTask<AssetHandle> LoadAsyncHandle<T>(string path, string groupName = "") where T : UnityEngine.Object
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var handle = await group.LoadAsyncHandle<T>(path);
            return handle;
        }

        public async UniTask<AssetHandle> LoadAsyncHandle(string path, Type type, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var handle = await group.LoadAsyncHandle(path, type);
            return handle;
        }

        internal async UniTask<Sprite> LoadSpriteAsync(string path, string groupName = "")
        {
            var sprite = await LoadSpriteAsync(path, path, groupName);
            return sprite;
        }

        internal async UniTask<Sprite> LoadSpriteAsync(string mainPath, string path, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var sprite = await group.LoadSpriteAsync(mainPath, path);
            return sprite;
        }

        public UniTask<Sprite> LoadSpriteByMacro(string path)
        {
            if (UseAsyncLoad)
            {
                return LoadSpriteAsync(path, "");
            }
            return UniTask.FromResult(LoadSpriteSync(path, ""));
        }

        public UniTask<Sprite> LoadSpriteByMacro(string path, string groupName)
        {
            if (UseAsyncLoad)
            {
                return LoadSpriteAsync(path, groupName);
            }
            return UniTask.FromResult(LoadSpriteSync(path, groupName));
        }

        public UniTask<Sprite> LoadSpriteByMacro(string mainPath, string path, string groupName = "")
        {
            if (UseAsyncLoad)
            {
                return LoadSpriteAsync(mainPath, path, groupName);
            }
            return UniTask.FromResult(LoadSpriteSync(mainPath, path, groupName));
        }

        internal T InternalLoadSync<T>(string path, string groupName = "") where T : UnityEngine.Object
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var obj = group.LoadSync<T>(path);
            return obj;
        }

        internal UnityEngine.Object InternalLoadSync(string path, Type type, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var obj = group.LoadSync(path, type);
            return obj;
        }

        internal AssetHandle LoadSyncHandle<T>(string path, string groupName = "") where T : UnityEngine.Object
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var handle = group.LoadSyncHandle<T>(path);
            return handle;
        }

        public AssetHandle LoadSyncHandle(string path, Type type, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var handle = group.LoadSyncHandle(path, type);
            return handle;
        }

        internal Sprite LoadSpriteSync(string path, string groupName = "")
        {
            return LoadSpriteSync(path, path, groupName);
        }

        private Sprite LoadSpriteSync(string mainPath, string path, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            var sprite = group.LoadSprite(mainPath, path);
            return sprite;
        }
        #endregion

        #region 提供的资源管理接口

        public GameObject GetSyncByPrefab(GameObject prefab, string groupName = "")
        {
            return GameObjectPools.Instance.GetSysnByPrefab(prefab, groupName);
        }

        public void RecycleByPrefab(GameObject go, GameObject prefab, string groupName = "")
        {
            GameObjectPools.Instance.RecycleByPrefab(go,prefab,groupName);
        }

        public void ChangeObjectName(GameObject obj, string name)
        {
            GameObjectPools.Instance.ChangeObjectName(obj,name);
        }

        public T LoadSync<T>(string path, string groupName = "")
            where T : UnityEngine.Object
        {
            var type = typeof(T);
            T obj = default;
            if (type == typeof(GameObject))
            {
                obj = GameObjectPools.Instance.GetSync(path, groupName) as T;
            }
            else if (type == typeof(Material))
            {
                obj = MaterialPools.Instance.GetSync(path, groupName) as T;
            }
            else if (type == typeof(Shader))
            {
                obj = ShaderPools.Instance.GetSync(path, groupName) as T;
            }
            else if (type == typeof(Texture2D))
            {
                obj = TexturePools.Instance.GetSync(path, groupName) as T;
            }
            else if (type == typeof(Sprite))
            {
                obj = SpritePools.Instance.GetSync(path, groupName) as T;
            }
            else if (type == typeof(AnimationClip))
            {
                obj = AnimationClipPools.Instance.GetSync(path, groupName) as T;
            }
            else if (type == typeof(AudioClip))
            {
                obj = AudioClipPools.Instance.GetSync(path, groupName) as T;
            }
            else
            {
                obj = InternalLoadSync<T>(path, groupName);
            }
            return obj;
        }

        public UniTask<T> LoadByMacro<T>(string path, string groupName = "") where T : UnityEngine.Object
        {
            if (UseAsyncLoad)
            {
                return LoadAsync<T>(path, groupName);
            }
            return UniTask.FromResult(LoadSync<T>(path, groupName));
        }

        public UniTask<T> LoadByMacro<T>(string path, long code, string groupName = "") where T : UnityEngine.Object
        {
            if (UseAsyncLoad)
            {
                return LoadAsync<T>(path, code, groupName);
            }
            return UniTask.FromResult(LoadSync<T>(path, groupName));
        }

        public UniTask<UnityEngine.Object> LoadByMacro(string path, Type type, string groupName = "")
        {
            if (UseAsyncLoad)
            {
                return LoadAsync(path, type, groupName);
            }
            return UniTask.FromResult(LoadSync(path, type, groupName));
        }

        public UniTask<AssetHandle> LoadHandleByMacro<T>(string path, string groupName = "") where T : UnityEngine.Object
        {
            if (UseAsyncLoad)
            {
                return LoadAsyncHandle<T>(path, groupName);
            }
            return UniTask.FromResult(LoadSyncHandle<T>(path, groupName));
        }

        public UniTask<AssetHandle> LoadHandleByMacro(string path, Type type, string groupName = "")
        {
            if (UseAsyncLoad)
            {
                return LoadAsyncHandle(path, type, groupName);
            }
            return UniTask.FromResult(LoadSyncHandle(path, type, groupName));
        }

        public static void SetUseAsyncLoad(bool enabled)
        {
            UseAsyncLoad = enabled;
        }

        public void LoadAsync<T>(string path, long code, Action<T> action,string groupName = "")
            where T : UnityEngine.Object
        {
            var type = typeof(T);
            if (type == typeof(GameObject))
            {
                void Action(GameObject obj)
                {
                    action?.Invoke(obj as T);
                }

                GameObjectPools.Instance.GetAsyncAction(path, code,Action
                ,groupName);
            }
            else if (type == typeof(Material))
            {
                void Action(Material material)
                {
                    action?.Invoke(material as T);
                }
                MaterialPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(Shader))
            {
                void Action(Shader material)
                {
                    action?.Invoke(material as T);
                }
                ShaderPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(Texture2D))
            {
                void Action(Texture2D material)
                {
                    action?.Invoke(material as T);
                }
                TexturePools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(Sprite))
            {
                void Action(Sprite sprite)
                {
                    action?.Invoke(sprite as T);
                }
                SpritePools.Instance.GetAsyncAction(path,code, Action,groupName);
            }
            else if (type == typeof(AnimationClip))
            {
                void Action(AnimationClip material)
                {
                    action?.Invoke(material as T);
                }
                AnimationClipPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(AudioClip))
            {
                void Action(AudioClip material)
                {
                    action?.Invoke(material as T);
                }
                AudioClipPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else
            {
                DebugLogger.LogError("{0} Load No implementation please implement in the framework",typeof(T).ToString());
                action?.Invoke(null);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="code">同一个异步对象，多次加载，有可能出现返回顺序问题</param>
        /// <param name="groupName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public UniTask<T> LoadAsync<T>(string path,long code,string groupName = "")
            where T : UnityEngine.Object
        {
            var taskCompletionSource = new UniTaskCompletionSource<T>();
            var type = typeof(T);
            if (type == typeof(GameObject))
            {
                void Action(GameObject obj)
                {
                    taskCompletionSource.TrySetResult(obj as T);
                }
                GameObjectPools.Instance.GetAsyncAction(path,code, Action ,groupName);
            }
            else if (type == typeof(Material))
            {
                void Action(Material Sprite)
                {
                    taskCompletionSource.TrySetResult(Sprite as T);
                }
                MaterialPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(Shader))
            {
                void Action(Shader Sprite)
                {
                    taskCompletionSource.TrySetResult(Sprite as T);
                }
                ShaderPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(Texture2D))
            {
                void Action(Texture2D Sprite)
                {
                    taskCompletionSource.TrySetResult(Sprite as T);
                }
                TexturePools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(Sprite))
            {
                void Action(Sprite Sprite)
                {
                    taskCompletionSource.TrySetResult(Sprite as T);
                }
                SpritePools.Instance.GetAsyncAction(path,code, Action,groupName);
            }
            else if (type == typeof(AnimationClip))
            {
                void Action(AnimationClip Sprite)
                {
                    taskCompletionSource.TrySetResult(Sprite as T);
                }
                AnimationClipPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else if (type == typeof(AudioClip))
            {
                void Action(AudioClip Sprite)
                {
                    taskCompletionSource.TrySetResult(Sprite as T);
                }
                AudioClipPools.Instance.GetAsyncAction(path,code,Action,groupName);
            }
            else
            {
                DebugLogger.LogError("{0} Load No implementation please implement in the framework",typeof(T).ToString());
                taskCompletionSource.TrySetResult(null);
            }
            return taskCompletionSource.Task;
        }
        
      
        public async UniTask<T> LoadAsync<T>(string path,string groupName = "") where T : UnityEngine.Object
        {
            var type = typeof(T);
            T obj;
            if (type == typeof(GameObject))
            {
                obj = await GameObjectPools.Instance.GetAsync(path, groupName) as T;
            }
            else if (type == typeof(Material))
            {
                obj = await MaterialPools.Instance.GetAsync(path, groupName) as T;
            }
            else if (type == typeof(Shader))
            {
                obj = await ShaderPools.Instance.GetAsync(path, groupName) as T;
            }
            else if (type == typeof(Texture2D))
            {
                obj = await TexturePools.Instance.GetAsync(path, groupName) as T;
            }
            else if (type == typeof(Sprite))
            {
                obj = await SpritePools.Instance.GetAsync(path, groupName) as T;
            }
            else if (type == typeof(AnimationClip))
            {
                obj = await AnimationClipPools.Instance.GetAsync(path, groupName) as T;
            }
            else if (type == typeof(AudioClip))
            {
                obj = await AudioClipPools.Instance.GetAsync(path, groupName) as T;
            }
            else
            {
                obj = await InternalLoadAsync<T>(path, groupName);
            }
            return obj;
        }

        public UnityEngine.Object LoadSync(string path, Type type, string groupName = "")
        {
            UnityEngine.Object obj;
            if (type == typeof(GameObject))
            {
                obj = GameObjectPools.Instance.GetSync(path, groupName);
            }
            else if (type == typeof(Material))
            {
                obj = MaterialPools.Instance.GetSync(path, groupName);
            }
            else if (type == typeof(Shader))
            {
                obj = ShaderPools.Instance.GetSync(path, groupName);
            }
            else if (type == typeof(Texture2D))
            {
                obj = TexturePools.Instance.GetSync(path, groupName);
            }
            else if (type == typeof(Sprite))
            {
                obj = SpritePools.Instance.GetSync(path, groupName);
            }
            else if (type == typeof(AnimationClip))
            {
                obj = AnimationClipPools.Instance.GetSync(path, groupName);
            }
            else if (type == typeof(AudioClip))
            {
                obj = AudioClipPools.Instance.GetSync(path, groupName);
            }
            else
            {
                obj = InternalLoadSync(path, type, groupName);
            }

            return obj;
        }
        
        public async UniTask<UnityEngine.Object> LoadAsync(string path, Type type, string groupName = "", Action<UnityEngine.Object> action = default)
        {
            UnityEngine.Object obj = null;
            
            if (type == typeof(GameObject))
            {
                obj = await GameObjectPools.Instance.GetAsync(path, groupName);
            }
            else if (type == typeof(Material))
            {
                obj = await MaterialPools.Instance.GetAsync(path, groupName);
            }
            else if (type == typeof(Shader))
            {
                obj = await ShaderPools.Instance.GetAsync(path, groupName);
            }
            else if (type == typeof(Texture2D))
            {
                obj = await TexturePools.Instance.GetAsync(path, groupName);
            }
            else if (type == typeof(Sprite))
            {
                obj = await SpritePools.Instance.GetAsync(path, groupName);
            }
            else if (type == typeof(AnimationClip))
            {
                obj =  await AnimationClipPools.Instance.GetAsync(path, groupName);
            }
            else if (type == typeof(AudioClip))
            {
                obj =  await AudioClipPools.Instance.GetAsync(path, groupName);
            }
            else
            {
                obj = await InternalLoadAsync(path, type, groupName);
            }
            
            return obj;
        }
        
        public void Recycle<T>(T obj, string groupName = "") where T : UnityEngine.Object
        {
            if (obj == null) return;
            var type = obj.GetType();
            if (type == typeof(GameObject))
            {
                var go = obj as GameObject;
                GameObjectPools.Instance.Recycle(go, groupName);
            }
            else if (type == typeof(Material))
            {
                var mat = obj as Material;
                MaterialPools.Instance.Recycle(mat, groupName);
            }
            else if (type == typeof(AnimationClip))
            {
                var clip = obj as AnimationClip;
                AnimationClipPools.Instance.Recycle(clip,groupName);
            }
            else if(type == typeof(AudioClip))
            {
                var clip = obj as AudioClip;
                AudioClipPools.Instance.Recycle(clip, groupName);
            }
        }

        public void RecycleByGroup<T>(string groupName = "") where T : UnityEngine.Object
        {
            var type = typeof(T);
            if (type == typeof(GameObject))
            {
                GameObjectPools.Instance.RecycleByGroup(groupName);
            }
            else if (type == typeof(Material))
            {
                MaterialPools.Instance.RecycleByGroup(groupName);
            }
            else if(type == typeof(AnimationClip))
            {
                AnimationClipPools.Instance.RecycleByGroup(groupName);
            }
            else if(type == typeof(AudioClip))
            {
                AudioClipPools.Instance.RecycleByGroup(groupName);
            }
        }
        
        public void RecycleAllByGroup(string groupName = "")
        {
            GameObjectPools.Instance.RecycleByGroup(groupName);
            MaterialPools.Instance.RecycleByGroup(groupName);
            AnimationClipPools.Instance.RecycleByGroup(groupName);
            AudioClipPools.Instance.RecycleByGroup(groupName);
        }
        public void StopLoadByGroup(string groupName = "")
        {
            AssetsGroupManager.Instance.StopLoadByGroup(groupName);
        }
        
        public void ReleaseAllByGroup(string groupName = "")
        {
            if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.NomalAssetGroupName;
            if (string.IsNullOrEmpty(groupName))
            {
                DebugLogger.LogWarning("ReleaseAllByGroup groupName is null or empty");
                return;
            }
            StopLoadByGroup(groupName);
            TexturePools.Instance.DestroyByGroup(groupName);
            SpritePools.Instance.DestroyByGroup(groupName);
            ShaderPools.Instance.DestroyByGroup(groupName);
            MaterialPools.Instance.DestroyByGroup(groupName);
            GameObjectPools.Instance.DestroyByGroup(groupName);
            AnimationClipPools.Instance.DestroyByGroup(groupName);
            AudioClipPools.Instance.DestroyByGroup(groupName);
            AssetsGroupManager.Instance.DestoryByGroup(groupName, true);
        }
        public void Release(string path, string groupName = "")
        {
            var group = AssetsGroupManager.Instance.GetAssetGroup(groupName);
            group.Release(path);
        }

        #endregion

        #region 生命周期

        internal void AddDestoryEvent(UnityAction callBack)
        {
            if (callBack != null)
            {
                _destroyEvent?.AddListener(callBack);
            }
        }

        internal void RemoveDestoryEvent(UnityAction callBack,bool isAll = false)
        {
            if (isAll)
            {
                _destroyEvent?.RemoveAllListeners();
                return;
            }

            if (callBack != null)
            {
                _destroyEvent?.RemoveListener(callBack);
            }

        }

        internal void AddDisPoseEvent(UnityAction callBack)
        {
            if (callBack != null)
            {
                _disposeEvent?.AddListener(callBack);
            }
        }

        internal void RemoveDisPoseEvent(UnityAction callBack,bool isAll = false)
        {
            if (isAll)
            {
                _disposeEvent?.RemoveAllListeners();
                return;
            }

            if (callBack != null)
            {
                _disposeEvent?.RemoveListener(callBack);
            }
        }


        #endregion


        protected void OnDestroy()
        {
            _destroyEvent?.Invoke();
        }

        protected  void OnDispose()
        {
            _disposeEvent?.Invoke();
            _destroyEvent?.RemoveAllListeners();
            _disposeEvent?.RemoveAllListeners();
            _destroyEvent = null;
            _disposeEvent = null;
        }
    }
}