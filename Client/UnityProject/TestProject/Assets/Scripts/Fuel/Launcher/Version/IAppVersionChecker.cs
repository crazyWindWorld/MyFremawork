using Fuel.Launcher.Config;

namespace Fuel.Launcher.Version
{
    public interface IAppVersionChecker
    {
        AppUpdateDecision Check(LocalStartupConfig localConfig, RemoteVersionInfo remoteInfo);
    }
}
