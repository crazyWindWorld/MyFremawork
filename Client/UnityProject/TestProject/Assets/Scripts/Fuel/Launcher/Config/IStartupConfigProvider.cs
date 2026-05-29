namespace Fuel.Launcher.Config
{
    public interface IStartupConfigProvider
    {
        LocalStartupConfig Load();
    }
}
