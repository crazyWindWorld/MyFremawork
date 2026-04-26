using UnityEditor;
using UnityEngine;
using System;

public static class NetHandlerGeneratorEditor
{
    [MenuItem("Tools/Network/Force Regenerate NetHandler")]
    public static void GenerateMenu()
    {
        try
        {
            var handlers = NetHandlerGeneratorUtil.ScanHandlers();
            NetHandlerGeneratorUtil.GenerateCode(handlers);
            NetHandlerGeneratorUtil.SaveSnapshot(handlers);
            AssetDatabase.Refresh();

            Debug.Log($"[NetHandlerGeneratorEditor] ✅ 已强制重新生成注册文件并同步快照（数量: {handlers.Count}）。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[NetHandlerGeneratorEditor] ❌ 手动生成失败: {e}");
        }
    }
}
