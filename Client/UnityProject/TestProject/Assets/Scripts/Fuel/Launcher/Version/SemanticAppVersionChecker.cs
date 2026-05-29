using System;
using Fuel.Launcher.Config;

namespace Fuel.Launcher.Version
{
    public sealed class SemanticAppVersionChecker : IAppVersionChecker
    {
        public AppUpdateDecision Check(LocalStartupConfig localConfig, RemoteVersionInfo remoteInfo)
        {
            if (!string.IsNullOrEmpty(remoteInfo.minAppVersion) && Compare(localConfig.appVersion, remoteInfo.minAppVersion) < 0)
                return AppUpdateDecision.ForceUpdate;

            if (!string.IsNullOrEmpty(remoteInfo.latestAppVersion) && Compare(localConfig.appVersion, remoteInfo.latestAppVersion) < 0)
                return AppUpdateDecision.OptionalUpdate;

            return AppUpdateDecision.Continue;
        }

        public static int Compare(string left, string right)
        {
            var leftCore = StripMetadata(left).Split('.');
            var rightCore = StripMetadata(right).Split('.');

            for (int i = 0; i < 3; i++)
            {
                int l = i < leftCore.Length && int.TryParse(leftCore[i], out var lv) ? lv : 0;
                int r = i < rightCore.Length && int.TryParse(rightCore[i], out var rv) ? rv : 0;
                if (l != r) return l.CompareTo(r);
            }

            return 0;
        }

        private static string StripMetadata(string version)
        {
            if (string.IsNullOrEmpty(version)) return "0.0.0";
            int index = version.IndexOfAny(new[] { '-', '+' });
            return index >= 0 ? version.Substring(0, index) : version;
        }
    }
}
