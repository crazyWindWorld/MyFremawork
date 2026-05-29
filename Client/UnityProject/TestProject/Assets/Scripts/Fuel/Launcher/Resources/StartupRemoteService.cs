using System.Collections.Generic;
using YooAsset;

namespace Fuel.Launcher.Resources
{
    internal sealed class StartupRemoteService : IRemoteService
    {
        private readonly string _hostUrl;
        private readonly string _fallbackHostUrl;
        private readonly List<string> _urls = new List<string>(2);

        public StartupRemoteService(string hostUrl, string fallbackHostUrl)
        {
            _hostUrl = hostUrl?.TrimEnd('/');
            _fallbackHostUrl = fallbackHostUrl?.TrimEnd('/');
        }

        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            _urls.Clear();
            if (!string.IsNullOrEmpty(_hostUrl))
                _urls.Add($"{_hostUrl}/{fileName}");
            if (!string.IsNullOrEmpty(_fallbackHostUrl))
                _urls.Add($"{_fallbackHostUrl}/{fileName}");
            return _urls;
        }
    }
}
