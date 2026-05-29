using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;
using UnityEngine;
using YooAsset;

namespace Fuel.Launcher.Table
{
    public sealed class TableKitConfigLoader : IConfigLoader
    {
        private readonly Dictionary<string, byte[]> _binaryCache = new Dictionary<string, byte[]>();
        private readonly Dictionary<string, string> _jsonCache = new Dictionary<string, string>();

        public async UniTask LoadAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken)
        {
            TableKit.RuntimePathPattern = localConfig.configPathPattern;
            TableKit.SetBinaryLoader(fileName => _binaryCache.TryGetValue(fileName, out var bytes) ? bytes : null);
            TableKit.SetJsonLoader(fileName => _jsonCache.TryGetValue(fileName, out var json) ? json : null);
            await PreloadKnownTablesAsync(localConfig, cancellationToken);
            TableKit.Init();
        }

        public void Clear()
        {
            _binaryCache.Clear();
            _jsonCache.Clear();
            TableKit.Clear();
        }

        private async UniTask PreloadKnownTablesAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken)
        {
            await CacheIfExistsAsync(localConfig, "test_textitems", cancellationToken);
        }

        private async UniTask CacheIfExistsAsync(LocalStartupConfig localConfig, string fileName, CancellationToken cancellationToken)
        {
            var asset = await LoadTextAssetAsync(localConfig, fileName, cancellationToken);
            if (asset == null)
                return;

            _binaryCache[fileName] = asset.bytes;
            _jsonCache[fileName] = asset.text;
        }

        private static async UniTask<TextAsset> LoadTextAssetAsync(LocalStartupConfig localConfig, string fileName, CancellationToken cancellationToken)
        {
            var path = string.Format(localConfig.configPathPattern, fileName);
            var package = YooAssets.GetPackage(localConfig.packageName);
            var handle = package.LoadAssetAsync<TextAsset>(path);
            await handle.ToUniTask(cancellationToken: cancellationToken);
            return handle.GetAssetObject<TextAsset>();
        }
    }
}
