using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Fuel.AssetManager;
using Fuel.AssetManager.AssetsPools;
using HotFarmework.AssetManager;
using UnityEngine;

namespace HotFramework.AssetManager.AssetsPools
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class SpritePools : ReferencePools<Sprite>
    {
        protected override Sprite LoadSync(string path, string groupName)
        {
            return AssetsLoadManager.Instance.LoadSpriteSync(path, groupName);
        }

        protected override async UniTask<Sprite> LoadAsync(string path, string groupName)
        {
            return await AssetsLoadManager.Instance.LoadSpriteByMacro(path, path, groupName);
        }
    }
}


