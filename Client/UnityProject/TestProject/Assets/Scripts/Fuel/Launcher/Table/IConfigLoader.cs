using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;

namespace Fuel.Launcher.Table
{
    public interface IConfigLoader
    {
        UniTask LoadAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
        void Clear();
    }
}
