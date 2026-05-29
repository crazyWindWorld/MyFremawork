using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;

namespace Fuel.Launcher.Version
{
    public interface IVersionService
    {
        UniTask<RemoteVersionInfo> FetchVersionAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
    }
}
