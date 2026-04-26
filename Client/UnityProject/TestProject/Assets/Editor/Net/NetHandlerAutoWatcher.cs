using UnityEditor;
using UnityEngine;
using UnityEditor.Callbacks;
using System;
using System.Linq;

[InitializeOnLoad]
public static class NetHandlerAutoWatcher
{
    private static DateTime lastCheck = DateTime.MinValue;
    private const int CooldownSeconds = 30;

    static NetHandlerAutoWatcher()
    {
        EditorApplication.delayCall += TryCheckChange;
    }

    [DidReloadScripts]
    private static void OnReload()
    {
        TryCheckChange();
    }

    private static void TryCheckChange()
    {
        if ((DateTime.Now - lastCheck).TotalSeconds < CooldownSeconds)
            return;

        lastCheck = DateTime.Now;

        try
        {
            var current = NetHandlerGeneratorUtil.ScanHandlers();
            var old = NetHandlerGeneratorUtil.LoadSnapshot();
            bool changed = !old.SequenceEqual(current);

            if (!changed) 
            {
                Debug.Log("[NetHandlerAutoWatcher] 无变化，无需重新生成。");
                return;
            }

            NetHandlerGeneratorUtil.GenerateCode(current);
            NetHandlerGeneratorUtil.SaveSnapshot(current);
            AssetDatabase.Refresh();

            Debug.Log($"[NetHandlerAutoWatcher] 检测到变化，已自动重建注册文件（数量: {current.Count}）。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetHandlerAutoWatcher] 运行错误: {e}");
        }
    }
}
