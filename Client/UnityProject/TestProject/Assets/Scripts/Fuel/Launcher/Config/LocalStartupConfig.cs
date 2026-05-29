using System;

namespace Fuel.Launcher.Config
{
    [Serializable]
    public sealed class LocalStartupConfig
    {
        public string appVersion = "1.0.0";
        public string versionUrl;
        public string packageName = "Main";
        public string defaultHostUrl;
        public string fallbackHostUrl;
        public string hotUpdateDllPath;
        public string[] aotMetadataDllPaths = Array.Empty<string>();
        public string configPathPattern;
        public string hotUpdateEntryType;
        public string hotUpdateEntryMethod = "StartAsync";
    }
}
