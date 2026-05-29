using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;
using UnityEngine;

namespace Fuel.Launcher.Version
{
    public sealed class DefaultAppUpdateHandler : IAppUpdateHandler
    {
        public UniTask HandleForceUpdateAsync(RemoteVersionInfo remoteInfo, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(remoteInfo.storeUrl))
                Application.OpenURL(remoteInfo.storeUrl);
            return UniTask.CompletedTask;
        }

        public UniTask<bool> HandleOptionalUpdateAsync(RemoteVersionInfo remoteInfo, CancellationToken cancellationToken)
        {
            return UniTask.FromResult(true);
        }
    }
}
