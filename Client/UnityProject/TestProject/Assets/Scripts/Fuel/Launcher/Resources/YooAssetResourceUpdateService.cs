using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;
using YooAsset;

namespace Fuel.Launcher.Resources
{
    public sealed class YooAssetResourceUpdateService : IResourceUpdateService
    {
        private ResourcePackage _package;
        private ResourceDownloaderOperation _downloader;

        public async UniTask InitializeAsync(LocalStartupConfig localConfig, RemoteVersionInfo remoteInfo, CancellationToken cancellationToken)
        {
            if (!YooAssets.IsInitialized)
                YooAssets.Initialize();

            if (!YooAssets.TryGetPackage(localConfig.packageName, out _package))
                _package = YooAssets.CreatePackage(localConfig.packageName);

#if UNITY_EDITOR
            var buildResult = EditorSimulateBuildInvoker.Build(localConfig.packageName, (int)EBundleType.VirtualAssetBundle);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(buildResult.PackageRootDirectory);
            var editorOptions = new EditorSimulateModeOptions
            {
                EditorFileSystemParameters = editorFileSystem
            };
            var initOperation = _package.InitializePackageAsync(editorOptions);
#else
            var hostUrl = string.IsNullOrEmpty(remoteInfo.resourceHostUrl) ? localConfig.defaultHostUrl : remoteInfo.resourceHostUrl;
            var fallbackHostUrl = string.IsNullOrEmpty(remoteInfo.fallbackResourceHostUrl) ? localConfig.fallbackHostUrl : remoteInfo.fallbackResourceHostUrl;
            var remoteService = new StartupRemoteService(hostUrl, fallbackHostUrl);
#if UNITY_WEBGL
            var webOptions = new WebPlayModeOptions
            {
                WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(),
                WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteService)
            };
            var initOperation = _package.InitializePackageAsync(webOptions);
#else
            var hostOptions = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteService)
            };
            var initOperation = _package.InitializePackageAsync(hostOptions);
#endif
#endif
            await initOperation.ToUniTask(cancellationToken: cancellationToken);
            if (initOperation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException(initOperation.Error);

            var versionOperation = _package.RequestPackageVersionAsync();
            await versionOperation.ToUniTask(cancellationToken: cancellationToken);
            if (versionOperation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException(versionOperation.Error);

            var manifestOperation = _package.LoadPackageManifestAsync(new LoadPackageManifestOptions(versionOperation.PackageVersion, 60));
            await manifestOperation.ToUniTask(cancellationToken: cancellationToken);
            if (manifestOperation.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException(manifestOperation.Error);
        }

        public UniTask<ResourceDownloadInfo> CheckUpdateAsync(CancellationToken cancellationToken)
        {
            _downloader = _package.CreateResourceDownloader(new ResourceDownloaderOptions(10, 3));
            return UniTask.FromResult(new ResourceDownloadInfo(_downloader.TotalDownloadCount, _downloader.TotalDownloadBytes));
        }

        public async UniTask DownloadAsync(IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (_downloader == null || _downloader.TotalDownloadCount == 0)
                return;

            _downloader.DownloadProgressChanged += args =>
            {
                float value = args.TotalDownloadBytes <= 0 ? 0f : args.CurrentDownloadBytes / (float)args.TotalDownloadBytes;
                progress?.Report(value);
            };

            _downloader.StartDownload();
            await _downloader.ToUniTask(cancellationToken: cancellationToken);
            if (_downloader.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException(_downloader.Error);
        }

        public async UniTask ClearUnusedCacheAsync(CancellationToken cancellationToken)
        {
            var operation = _package.ClearCacheAsync(new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles));
            await operation.ToUniTask(cancellationToken: cancellationToken);
        }
    }
}
