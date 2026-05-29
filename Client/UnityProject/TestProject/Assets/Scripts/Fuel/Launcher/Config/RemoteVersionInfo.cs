using System;

namespace Fuel.Launcher.Config
{
    [Serializable]
    public sealed class RemoteVersionInfo
    {
        public string minAppVersion;
        public string latestAppVersion;
        public string resourceVersion;
        public string resourceHostUrl;
        public string fallbackResourceHostUrl;
        public string storeUrl;
        public string notice;
    }
}
