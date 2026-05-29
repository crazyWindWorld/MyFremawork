using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;

namespace Fuel.Launcher.Resources
{
    public interface IResourceUpdateService
    {
        UniTask InitializeAsync(LocalStartupConfig localConfig, RemoteVersionInfo remoteInfo, CancellationToken cancellationToken);
        UniTask<ResourceDownloadInfo> CheckUpdateAsync(CancellationToken cancellationToken);
        UniTask DownloadAsync(IProgress<float> progress, CancellationToken cancellationToken);
        UniTask ClearUnusedCacheAsync(CancellationToken cancellationToken);
    }
}
