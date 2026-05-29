using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;
using Fuel.Launcher.HybridCLR;
using Fuel.Launcher.Resources;
using Fuel.Launcher.Table;
using Fuel.Launcher.Version;

namespace Fuel.Launcher
{
    public sealed class GameUpdatePipeline
    {
        private readonly IStartupConfigProvider _configProvider;
        private readonly IVersionService _versionService;
        private readonly IAppVersionChecker _versionChecker;
        private readonly IAppUpdateHandler _appUpdateHandler;
        private readonly IResourceUpdateService _resourceUpdateService;
        private readonly IHybridCLRLoader _hybridCLRLoader;
        private readonly IConfigLoader _configLoader;

        public event Action<StartupStep> StepChanged;
        public event Action<float> DownloadProgressChanged;

        public GameUpdatePipeline(
            IStartupConfigProvider configProvider,
            IVersionService versionService,
            IAppVersionChecker versionChecker,
            IAppUpdateHandler appUpdateHandler,
            IResourceUpdateService resourceUpdateService,
            IHybridCLRLoader hybridCLRLoader,
            IConfigLoader configLoader)
        {
            _configProvider = configProvider;
            _versionService = versionService;
            _versionChecker = versionChecker;
            _appUpdateHandler = appUpdateHandler;
            _resourceUpdateService = resourceUpdateService;
            _hybridCLRLoader = hybridCLRLoader;
            _configLoader = configLoader;
        }

        public async UniTask RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                SetStep(StartupStep.LoadLocalConfig);
                var localConfig = _configProvider.Load();

                SetStep(StartupStep.FetchRemoteVersion);
                var remoteInfo = await _versionService.FetchVersionAsync(localConfig, cancellationToken);

                SetStep(StartupStep.CheckAppVersion);
                var decision = _versionChecker.Check(localConfig, remoteInfo);
                if (decision == AppUpdateDecision.ForceUpdate)
                {
                    await _appUpdateHandler.HandleForceUpdateAsync(remoteInfo, cancellationToken);
                    return;
                }

                if (decision == AppUpdateDecision.OptionalUpdate)
                {
                    bool shouldContinue = await _appUpdateHandler.HandleOptionalUpdateAsync(remoteInfo, cancellationToken);
                    if (!shouldContinue) return;
                }

                SetStep(StartupStep.InitAssets);
                await _resourceUpdateService.InitializeAsync(localConfig, remoteInfo, cancellationToken);

                SetStep(StartupStep.UpdateAssets);
                var downloadInfo = await _resourceUpdateService.CheckUpdateAsync(cancellationToken);
                if (downloadInfo.TotalCount > 0)
                    await _resourceUpdateService.DownloadAsync(new Progress<float>(p => DownloadProgressChanged?.Invoke(p)), cancellationToken);
                await _resourceUpdateService.ClearUnusedCacheAsync(cancellationToken);

                SetStep(StartupStep.LoadAotMetadata);
                await _hybridCLRLoader.LoadAotMetadataAsync(localConfig, cancellationToken);

                SetStep(StartupStep.LoadHotUpdateDll);
                Assembly hotUpdateAssembly = await _hybridCLRLoader.LoadHotUpdateAssemblyAsync(localConfig, cancellationToken);

                SetStep(StartupStep.LoadConfigs);
                await _configLoader.LoadAsync(localConfig, cancellationToken);

                SetStep(StartupStep.EnterGame);
                await InvokeHotUpdateEntryAsync(hotUpdateAssembly, localConfig, cancellationToken);
            }
            catch
            {
                SetStep(StartupStep.Failed);
                throw;
            }
        }

        private static async UniTask InvokeHotUpdateEntryAsync(Assembly assembly, LocalStartupConfig config, CancellationToken cancellationToken)
        {
            var type = assembly.GetType(config.hotUpdateEntryType, true);
            var method = type.GetMethod(config.hotUpdateEntryMethod, BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new MissingMethodException(config.hotUpdateEntryType, config.hotUpdateEntryMethod);

            var result = method.Invoke(null, new object[] { cancellationToken });
            if (result is UniTask task)
                await task;
        }

        private void SetStep(StartupStep step)
        {
            StepChanged?.Invoke(step);
        }
    }
}
