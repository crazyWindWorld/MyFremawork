using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;
using UnityEngine;
using YooAsset;

#if !UNITY_EDITOR
using HybridCLR;
#endif

namespace Fuel.Launcher.HybridCLR
{
    public sealed class HybridCLRLoader : IHybridCLRLoader
    {
        public async UniTask LoadAotMetadataAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken)
        {
            if (localConfig.aotMetadataDllPaths == null)
                return;

            for (int i = 0; i < localConfig.aotMetadataDllPaths.Length; i++)
            {
                var path = localConfig.aotMetadataDllPaths[i];
                if (string.IsNullOrEmpty(path))
                    continue;

                var bytes = await LoadBytesAsync(localConfig.packageName, path, cancellationToken);
#if !UNITY_EDITOR
                RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet);
#endif
            }
        }

        public async UniTask<Assembly> LoadHotUpdateAssemblyAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken)
        {
            var bytes = await LoadBytesAsync(localConfig.packageName, localConfig.hotUpdateDllPath, cancellationToken);
#if UNITY_EDITOR
            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(localConfig.hotUpdateDllPath);
            assemblyName = assemblyName.Replace(".dll", string.Empty).Replace(".bytes", string.Empty);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == assemblyName)
                    return assembly;
            }
#endif
            return Assembly.Load(bytes);
        }

        private static async UniTask<byte[]> LoadBytesAsync(string packageName, string path, CancellationToken cancellationToken)
        {
            var package = YooAssets.GetPackage(packageName);
            var handle = package.LoadAssetAsync<TextAsset>(path);
            await handle.ToUniTask(cancellationToken: cancellationToken);
            var asset = handle.GetAssetObject<TextAsset>();
            if (asset == null)
                throw new InvalidOperationException($"Load bytes failed: {path}");
            return asset.bytes;
        }
    }
}
