using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;

namespace Fuel.Launcher.Version
{
    public interface IAppUpdateHandler
    {
        UniTask HandleForceUpdateAsync(RemoteVersionInfo remoteInfo, CancellationToken cancellationToken);
        UniTask<bool> HandleOptionalUpdateAsync(RemoteVersionInfo remoteInfo, CancellationToken cancellationToken);
    }
}
