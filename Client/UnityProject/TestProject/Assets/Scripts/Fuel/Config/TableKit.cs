using System;
using Luban;
using Luban.SimpleJSON;
using UnityEngine;

/// <summary>
/// 配置表系统入口类
/// 由 TableKit 工具自动生成，路径配置已嵌入代码
/// </summary>
public static class TableKit
{
    private static cfg.Tables sTables;
    private static Func<string, byte[]> sBinaryLoader;
    private static Func<string, string> sJsonLoader;

    /// <summary>
    /// 是否已初始化
    /// </summary>
    public static bool Initialized { get; private set; }

    /// <summary>
    /// 运行时路径模式（生成时嵌入）
    /// </summary>
    public static string RuntimePathPattern { get; set; } = "ExcelData/{0}";

    /// <summary>
    /// 获取配置表实例
    /// </summary>
    public static cfg.Tables Tables
    {
        get
        {
            if (sTables == null) Init();
            return sTables;
        }
    }

    /// <summary>
    /// 设置二进制数据加载器
    /// </summary>
    public static void SetBinaryLoader(Func<string, byte[]> loader)
    {
        sBinaryLoader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    /// <summary>
    /// 设置 JSON 数据加载器
    /// </summary>
    public static void SetJsonLoader(Func<string, string> loader)
    {
        sJsonLoader = loader ?? throw new ArgumentNullException(nameof(loader));
    }

    /// <summary>
    /// 初始化配置表
    /// </summary>
    public static void Init()
    {
        if (Initialized) return;

        if (sBinaryLoader == null) sBinaryLoader = DefaultBinaryLoader;
        if (sJsonLoader == null) sJsonLoader = DefaultJsonLoader;

        var tablesCtor = typeof(cfg.Tables).GetConstructors()[0];
        var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];

        object loader = loaderReturnType == typeof(ByteBuf)
            ? new Func<string, ByteBuf>(LoadBinary)
            : new Func<string, JSONNode>(LoadJson);

        sTables = (cfg.Tables)tablesCtor.Invoke(new object[] { loader });
        Initialized = true;
    }

    private static JSONNode LoadJson(string fileName)
    {
        var json = sJsonLoader(fileName);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[TableKit] 加载配置表失败: {fileName}");
            return null;
        }
        return JSON.Parse(json);
    }

    private static ByteBuf LoadBinary(string fileName)
    {
        var bytes = sBinaryLoader(fileName);
        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogError($"[TableKit] 加载配置表失败: {fileName}");
            return null;
        }
        return new ByteBuf(bytes);
    }

    #region 默认加载器

    // 默认加载器：使用 Resources
    private static byte[] DefaultBinaryLoader(string fileName)
    {
        var path = string.Format(RuntimePathPattern, fileName);
        var asset = Resources.Load<TextAsset>(path);
        return asset != null ? asset.bytes : null;
    }

    private static string DefaultJsonLoader(string fileName)
    {
        var path = string.Format(RuntimePathPattern, fileName);
        var asset = Resources.Load<TextAsset>(path);
        return asset != null ? asset.text : null;
    }

    #endregion

    /// <summary>
    /// 重新加载配置表
    /// </summary>
    public static void Reload(Action onComplete = null)
    {
        try
        {
            sTables = null;
            Initialized = false;
            Init();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TableKit] Reload failed: {ex.Message}");
        }
        onComplete?.Invoke();
    }

    /// <summary>
    /// 清理所有数据
    /// </summary>
    public static void Clear()
    {
        sTables = null;
        Initialized = false;
#if UNITY_EDITOR
        sTablesEditor = null;
#endif
    }

#if UNITY_EDITOR
    private static cfg.Tables sTablesEditor;

    /// <summary>
    /// 编辑器数据路径（生成时嵌入）
    /// </summary>
    public static string EditorDataPath { get; set; } = "Assets/Resources/ExcelData/";

    /// <summary>
    /// 获取编辑器模式下的配置表实例
    /// </summary>
    public static cfg.Tables TablesEditor
    {
        get
        {
            if (sTablesEditor == null) InitEditor();
            return sTablesEditor;
        }
    }

    private static void InitEditor()
    {
        if (sTablesEditor != null) return;

        var tablesCtor = typeof(cfg.Tables).GetConstructors()[0];
        var loaderReturnType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];

        object loader = loaderReturnType == typeof(ByteBuf)
            ? new Func<string, ByteBuf>(LoadBinaryEditor)
            : new Func<string, JSONNode>(LoadJsonEditor);

        sTablesEditor = (cfg.Tables)tablesCtor.Invoke(new object[] { loader });
    }

    private static JSONNode LoadJsonEditor(string fileName)
    {
        var path = $"{EditorDataPath}{fileName}.json";
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null)
        {
            Debug.LogError($"[TableKit] 编辑器加载配置表失败: {path}");
            return null;
        }
        return JSON.Parse(asset.text);
    }

    private static ByteBuf LoadBinaryEditor(string fileName)
    {
        var path = $"{EditorDataPath}{fileName}.bytes";
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null)
        {
            Debug.LogError($"[TableKit] 编辑器加载配置表失败: {path}");
            return null;
        }
        return new ByteBuf(asset.bytes);
    }

    /// <summary>
    /// 刷新编辑器缓存
    /// </summary>
    public static void RefreshEditor() => sTablesEditor = null;
#endif
}
