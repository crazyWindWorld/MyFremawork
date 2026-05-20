
/*
namespace Framework.Pools
{
    public class FairyGUIPool : Singleton<FairyGUIPool>
    {
        private Dictionary<string, Dictionary<string, UnityEngine.Object>> _groupObjcetPools;

        public override void Init()
        {
            base.Init();
            _groupObjcetPools = new Dictionary<string, Dictionary<string, UnityEngine.Object>>();
        }


        public async UniTask<UnityEngine.Object> GetAsync(string path, Type type, string groupName = "")
        {
            //if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.Instance.NomalAssetGroupName;
            if (_groupObjcetPools.TryGetValue(groupName, out var objectPools))
            {
                if (objectPools.TryGetValue(path, out var obj))
                {
                    return obj;
                }
                else
                {
                    obj = await AssetsLoadManager.Instance.LoadAsync(path, type, groupName);
                    objectPools.Add(path, obj);
                    return obj;
                }
            }
            else
            {
                objectPools = new Dictionary<string, UnityEngine.Object>();
                _groupObjcetPools.Add(groupName, objectPools);
                var obj = await AssetsLoadManager.Instance.LoadAsync(path, type, groupName);
                objectPools.Add(path, obj);
                return obj;
            }
        }

        public UnityEngine.Object GetSync(string path, Type type, string groupName = "")
        {
            //if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.Instance.NomalAssetGroupName;
            if (_groupObjcetPools.TryGetValue(groupName, out var objectPools))
            {
                if (objectPools.TryGetValue(path, out var obj))
                {
                    return obj;
                }
                else
                {
                    obj =  AssetsLoadManager.Instance.LoadSync(path, type, groupName);
                    objectPools.Add(path, obj);
                    return obj;
                }
            }
            else
            {
                objectPools = new Dictionary<string, UnityEngine.Object>();
                _groupObjcetPools.Add(groupName, objectPools);
                var obj = AssetsLoadManager.Instance.LoadSync(path, type, groupName);
                objectPools.Add(path, obj);
                return obj;
            }
        }


        public void DestroyByGroup(string groupName)
        {
            //if (string.IsNullOrEmpty(groupName)) groupName = AssetsManager.Instance.NomalAssetGroupName;
            if (_groupObjcetPools != null && _groupObjcetPools.TryGetValue(groupName, out var objectPools))
            {
                objectPools.Clear();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _groupObjcetPools?.Clear();
        }
    }
}*/


