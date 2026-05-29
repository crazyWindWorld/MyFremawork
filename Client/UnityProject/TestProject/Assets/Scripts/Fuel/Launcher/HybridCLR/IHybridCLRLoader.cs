using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;

namespace Fuel.Launcher.HybridCLR
{
    public interface IHybridCLRLoader
    {
        UniTask LoadAotMetadataAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
        UniTask<Assembly> LoadHotUpdateAssemblyAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
    }
}
