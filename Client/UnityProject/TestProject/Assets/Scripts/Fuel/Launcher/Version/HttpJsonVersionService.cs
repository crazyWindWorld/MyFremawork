using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fuel.Launcher.Config;
using UnityEngine;
using UnityEngine.Networking;

namespace Fuel.Launcher.Version
{
    public sealed class HttpJsonVersionService : IVersionService
    {
        public async UniTask<RemoteVersionInfo> FetchVersionAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken)
        {
            using var request = UnityWebRequest.Get(localConfig.versionUrl);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"Fetch remote version failed: {request.error}");

            var info = JsonUtility.FromJson<RemoteVersionInfo>(request.downloadHandler.text);
            if (info == null)
                throw new InvalidOperationException("Remote version json parse failed.");

            return info;
        }
    }
}
