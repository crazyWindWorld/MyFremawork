using UnityEngine;

namespace Fuel.Launcher.Config
{
    public sealed class ResourcesJsonStartupConfigProvider : IStartupConfigProvider
    {
        private const string ResourcePath = "StartupConfig";

        public LocalStartupConfig Load()
        {
            var asset = UnityEngine.Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new System.IO.FileNotFoundException("StartupConfig.json not found in Resources.");

            var config = JsonUtility.FromJson<LocalStartupConfig>(asset.text);
            if (config == null)
                throw new System.InvalidOperationException("StartupConfig.json parse failed.");
            return config;
        }
    }
}
