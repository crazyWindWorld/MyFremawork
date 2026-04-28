#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using System.Linq;

public class ProtoBatchWindowAsync : EditorWindow
{
    // ------------------ EditorPrefs Keys ------------------
    private const string KeyBatPath = "ProtoBatchWindowAsync.BatPath";
    private const string KeyProtoRoot = "ProtoBatchWindowAsync.ProtoRoot";
    private const string KeyOutRoot = "ProtoBatchWindowAsync.OutRoot";
    private const string KeyProtocPath = "ProtoBatchWindowAsync.ProtocPath";
    private const string KeyImportRoot = "ProtoBatchWindowAsync.ImportRoot";
    private const string KeyEnableGrpc = "ProtoBatchWindowAsync.EnableGrpc";
    private const string KeyGrpcPluginPath = "ProtoBatchWindowAsync.GrpcPluginPath";
    private const string KeyCleanOutput = "ProtoBatchWindowAsync.CleanOutput";
    private const string KeyOnlyChanged = "ProtoBatchWindowAsync.OnlyChanged";
    private const string KeyMd5StatePath = "ProtoBatchWindowAsync.Md5StatePath";

    // ------------------ Config Fields ------------------
    private string batPath;
    private string protoRoot;
    private string outRoot;
    private string protocPath;
    private string importRoot;
    private string grpcPluginPath;
    private bool enableGrpc;
    private bool cleanOutput;
    private bool onlyChanged;
    private string md5StatePath;

    // ------------------ Runtime State ------------------
    private CancellationTokenSource cts;
    private Process runningProcess;
    private bool isRunning;
    private bool cancelRequested;
    private DateTime startTime;

    private int totalCount;
    private int compiledCount;
    private int skippedCount;
    private int failedCount;

    private string currentFile = "-";
    private string statusText = "Idle";
    private float progress01 = 0f;

    // thread-safe log cache（主线程 OnGUI 刷新）
    private readonly object logLock = new object();
    private readonly StringBuilder logBuffer = new StringBuilder(16 * 1024);
    private Vector2 logScroll;
    private bool autoScroll = true;

    // 文件日志支持
    private StreamWriter logFileWriter;
    private readonly string logFilePath = Path.Combine(Application.dataPath, "../Logs/ProtoBatch.log");

    [MenuItem("Tools/Protobuf/Proto Generator (Async)")]
    public static void Open()
    {
        var win = GetWindow<ProtoBatchWindowAsync>("Proto Generator Async");
        win.minSize = new Vector2(760, 540);
        win.Show();
    }

    private void OnEnable()
    {
        LoadPrefs();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        TryCancelProcess(forceKill: false);
        cts?.Dispose();
        cts = null;
    }

    private void OnEditorUpdate()
    {
        // 强制重绘，保证进度条/日志实时刷新
        if (isRunning) Repaint();
    }

    private void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space(6);
        DrawConfigPanel();
        EditorGUILayout.Space(8);
        DrawRunPanel();
        EditorGUILayout.Space(8);
        DrawProgressPanel();
        EditorGUILayout.Space(8);
        DrawLogPanel();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Protobuf BAT Runner (Async)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "支持：异步执行、进度显示、取消任务、日志实时输出。\n" +
            "路径可填写项目相对路径（推荐）或绝对路径。", MessageType.Info);
    }

    private void DrawConfigPanel()
    {
        using (new EditorGUI.DisabledScope(isRunning))
        {
            DrawPathField(ref batPath, "BAT Path", "选择 bat 文件", false);
            DrawPathField(ref protoRoot, "Proto Root", "选择 proto 根目录", true);
            DrawPathField(ref outRoot, "Output Root", "选择输出目录", true);
            DrawPathField(ref protocPath, "protoc.exe Path (可空)", "选择 protoc.exe", false);
            DrawPathField(ref importRoot, "Import Root(proto依赖)", "选择 import 根目录", true);
            DrawPathField(ref md5StatePath, "MD5 State Path(增量构建)", "选择 md5 状态文件路径", true);
            //TODO: 暂时不提供GRPC的导出功能
            /*             enableGrpc = EditorGUILayout.ToggleLeft("Enable gRPC (--grpc_out)", enableGrpc);
                        using (new EditorGUI.DisabledScope(!enableGrpc))
                        {
                            DrawPathField(ref grpcPluginPath, "grpc_csharp_plugin Path (可空)", "选择 grpc_csharp_plugin.exe", false);
                        } */

            cleanOutput = EditorGUILayout.ToggleLeft("Clean Output (*.cs) before build", cleanOutput);
            onlyChanged = EditorGUILayout.ToggleLeft("Only Build Changed .proto", onlyChanged);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存配置", GUILayout.Height(26)))
                {
                    SavePrefs();
                    ShowNotification(new GUIContent("配置已保存"));
                }

                if (GUILayout.Button("恢复默认", GUILayout.Height(26)))
                {
                    if (EditorUtility.DisplayDialog("恢复默认", "确定恢复默认配置吗？", "确定", "取消"))
                    {
                        ResetDefaults();
                        SavePrefs();
                    }
                }

                if (GUILayout.Button("清空日志", GUILayout.Height(26)))
                {
                    lock (logLock) logBuffer.Clear();
                    Repaint();
                }
                if (GUILayout.Button("读取日志", GUILayout.Height(26)))
                {
                    if (logBuffer.Length == 0 && File.Exists(logFilePath))
                    {
                        try
                        {
                            string content = File.ReadAllText(logFilePath, Encoding.UTF8);
                            lock (logLock)
                            {
                                logBuffer.Clear();
                                logBuffer.Append(content);
                            }
                            AppendLog($"[INFO] 从 {logFilePath} 读取上次日志成功。");
                        }
                        catch (Exception ex)
                        {
                            AppendLog($"[ERROR] 读取上次日志失败: {ex.Message}");
                        }
                    }
                    else
                    {
                        AppendLog($"[WARN] 暂无上次日志文件: {logFilePath}");
                    }
                    Repaint();
                }
            }
        }
    }

    private void DrawRunPanel()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.backgroundColor = isRunning ? Color.gray : new Color(0.35f, 0.9f, 0.35f);
            if (GUILayout.Button("开始生成", GUILayout.Height(32)))
            {
                if (!isRunning)
                {
                    lock (logLock) logBuffer.Clear();
                    SavePrefs();
                    _ = RunAsync();
                }
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = !isRunning ? Color.gray : new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("取消", GUILayout.Height(32)))
            {
                if (isRunning) TryCancelProcess(forceKill: true);
            }
            GUI.backgroundColor = Color.white;

            autoScroll = GUILayout.Toggle(autoScroll, "日志自动滚动", GUILayout.Width(110));
        }
    }

    private void DrawProgressPanel()
    {
        EditorGUILayout.LabelField("运行状态", EditorStyles.boldLabel);

        Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(r, Mathf.Clamp01(progress01), $"{statusText}  ({progress01 * 100f:0.0}%)");

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Current: {currentFile}");
        EditorGUILayout.LabelField($"Total: {totalCount} | Compiled: {compiledCount} | Skipped: {skippedCount} | Failed: {failedCount}");

        if (isRunning)
        {
            var elapsed = DateTime.Now - startTime;
            EditorGUILayout.LabelField($"Elapsed: {elapsed:hh\\:mm\\:ss}");
        }
    }

    private void DrawLogPanel()
    {
        EditorGUILayout.LabelField("日志输出", EditorStyles.boldLabel);

        string text;
        lock (logLock) text = logBuffer.ToString();

        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.ExpandHeight(true));
        EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        if (autoScroll && Event.current.type == EventType.Repaint)
        {
            logScroll.y = float.MaxValue;
        }
    }

    // ------------------ Core Run ------------------

    private async Task RunAsync()
    {
        try
        {
            if (!ValidateInputs(out string err))
            {
                EditorUtility.DisplayDialog("配置错误", err, "确定");
                return;
            }

            PrepareRunState();

            string projectRoot = GetProjectRoot();
            string batAbs = ResolvePath(batPath);
            string protoAbs = ResolvePath(protoRoot);
            string outAbs = ResolvePath(outRoot);
            string importAbs = ResolvePath(importRoot);
            string protocAbsOrEmpty;
            if (string.IsNullOrWhiteSpace(protocPath))
            {
                // 默认在项目根目录查找 protoc.exe
                protocAbsOrEmpty = Path.Combine(projectRoot, "protoc.exe");
                if (!File.Exists(protocAbsOrEmpty))
                {
                    AppendLog($"[WARN] protocPath 未配置，且项目根目录未找到 protoc.exe: {protocAbsOrEmpty}");
                    protocAbsOrEmpty = "";
                }
                else
                {
                    AppendLog($"[INFO] 自动检测到 protoc.exe: {protocAbsOrEmpty}");
                }
            }
            else
            {
                protocAbsOrEmpty = ResolvePath(protocPath);
            }
            string grpcAbsOrEmpty = string.IsNullOrWhiteSpace(grpcPluginPath) ? "" : ResolvePath(grpcPluginPath);
            string md5Abs = ResolvePath(md5StatePath);

            Directory.CreateDirectory(outAbs);

            // 先统计总 proto 数，用于进度估算
            totalCount = Directory.Exists(protoAbs) ? Directory.GetFiles(protoAbs, "*.proto", SearchOption.AllDirectories).Length : 0;
            AppendLog($"[INFO] Found proto count = {totalCount}");

            string args =
                $"\"{protoAbs}\" \"{outAbs}\" " +
                $"\"{protocAbsOrEmpty}\" \"{importAbs}\" " +
                $"{(enableGrpc ? 1 : 0)} " +
                $"\"{grpcAbsOrEmpty}\" " +
                $"{(cleanOutput ? 1 : 0)} " +
                $"{(onlyChanged ? 1 : 0)} " +
                $"\"{md5Abs}\"";

            cts = new CancellationTokenSource();
            await RunProcessAsync(batAbs, args, projectRoot, cts.Token);

            if (cancelRequested)
            {
                statusText = "Canceled";
                AppendLog("[INFO] Canceled by user.");
                EditorUtility.DisplayDialog("已取消", "任务已取消。", "确定");
            }
            else if (failedCount > 0)
            {
                statusText = "Completed With Errors";
                EditorUtility.DisplayDialog("完成（有错误）", "执行结束，但存在错误日志，请查看 Console/窗口日志。", "确定");
            }
            else
            {
                statusText = "Completed";
                progress01 = 1f;
                AssetDatabase.Refresh();
                ShowNotification(new GUIContent("Protobuf C# 生成成功。"));
                await GenerateProtoCmdsAsync();
            }
        }
        catch (Exception ex)
        {
            failedCount++;
            statusText = "Failed";
            AppendLog("[EXCEPTION] " + ex);
            UnityEngine.Debug.LogError("[ProtoBatchWindowAsync] " + ex);
            EditorUtility.DisplayDialog("异常", ex.Message, "确定");
        }
        finally
        {
            isRunning = false;
            runningProcess = null;
            cts?.Dispose();
            cts = null;
            try
            {
                // 写出日志内容到 .GenProtoLog.txt（覆盖旧文件）
                if (!string.IsNullOrEmpty(logFilePath))
                {
                    string dir = Path.GetDirectoryName(logFilePath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    string text;
                    lock (logLock)
                    {
                        text = logBuffer.ToString();
                    }

                    File.WriteAllText(logFilePath, text, Encoding.UTF8);
                    Debug.Log($"[PROTO] 已写出日志文件: {logFilePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PROTO] 写日志文件失败: {ex.Message}");
            }
            Repaint();
        }
    }

    private async Task RunProcessAsync(string batAbs, string args, string workingDir, CancellationToken token)
    {
        var psi = new ProcessStartInfo
        {
            FileName = batAbs,          // 直接执行 bat
            Arguments = args,
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        AppendLog("[CMD] " + psi.FileName + " " + psi.Arguments);

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        runningProcess = p;

        var tcs = new TaskCompletionSource<int>();

        p.OutputDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            HandleOutputLine(e.Data, isError: false);
        };

        p.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            HandleOutputLine(e.Data, isError: true);
        };

        p.Exited += (_, __) =>
        {
            try { tcs.TrySetResult(p.ExitCode); }
            catch { tcs.TrySetResult(-1); }
        };

        if (!p.Start())
            throw new Exception("启动进程失败。");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        using (token.Register(() =>
        {
            try
            {
                cancelRequested = true;
                if (!p.HasExited) p.Kill();
            }
            catch { }
        }))
        {
            int exitCode = await tcs.Task;
            AppendLog($"[EXIT] code={exitCode}");
            if (!cancelRequested && exitCode == 0)
            {
                progress01 = 1f;
                statusText = "Completed";
            }
            if (!cancelRequested && exitCode != 0)
            {
                failedCount++;
                throw new Exception($"BAT 执行失败，ExitCode={exitCode}");
            }
        }
    }


    // ------------------ Parse & Progress ------------------

    private void HandleOutputLine(string line, bool isError)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        if (isError) AppendLog("[ERR_STREAM] " + line);
        else AppendLog(line);

        string s = line.Trim();

        // ---------- 1) 先识别“当前文件” ----------
        // 中文
        if (s.StartsWith("[编译]"))
        {
            currentFile = s.Substring("[编译]".Length).Trim();
            statusText = "Compiling";
            return;
        }
        if (s.StartsWith("[跳过]"))
        {
            currentFile = s.Substring("[跳过]".Length).Trim();
            statusText = "Skipping";
            return;
        }
        if (s.StartsWith("[失败]"))
        {
            currentFile = s.Substring("[失败]".Length).Trim();
            statusText = "Failed";
            return;
        }

        // ---------- 2) 识别“完成一个文件”的事件并推进进度 ----------
        bool doneOne = false;

        if (s.StartsWith("[GEN]") || s.StartsWith("[GEN ]"))
        {
            compiledCount++;
            statusText = "Generating";
            doneOne = true;
        }
        else if (s.StartsWith("[跳过]"))
        {
            skippedCount++;
            statusText = "Skipping";
            doneOne = true;
        }
        else if (s.StartsWith("[失败]"))
        {
            failedCount++;
            statusText = "Failed";
            doneOne = true;
        }
        else if (s.StartsWith("[STATE_HIT"))
        {
            // ONLY_CHANGED 命中可视作跳过
            skippedCount++;
            statusText = "Skipping";
            doneOne = true;
        }
        else if (s.StartsWith("[ERR ]") || s.StartsWith("[ERR]"))
        {
            failedCount++;
            statusText = "Failed";
            doneOne = true;
        }

        if (doneOne)
        {
            UpdateProgress();
            return;
        }

        // ---------- 3) 收尾状态 ----------
        if (s.StartsWith("[完成]") || s.StartsWith("[DONE]") || s.StartsWith("[SUMMARY]"))
        {
            statusText = "Finishing";
            progress01 = Mathf.Max(progress01, 0.98f);
        }
    }


    private void UpdateProgress()
    {
        int done = compiledCount + skippedCount + failedCount;
        if (totalCount <= 0)
        {
            // 无法估算时给一个缓慢增长
            progress01 = Mathf.Clamp01(progress01 + 0.01f);
        }
        else
        {
            progress01 = Mathf.Clamp01((float)done / totalCount);
        }
    }

    // ------------------ Helpers ------------------

    private bool ValidateInputs(out string err)
    {
        string batAbs = ResolvePath(batPath);
        string protoAbs = ResolvePath(protoRoot);

        if (string.IsNullOrWhiteSpace(batPath))
        {
            err = "BAT Path 不能为空。";
            return false;
        }
        if (!File.Exists(batAbs))
        {
            err = $"找不到 bat 文件：\n{batAbs}";
            return false;
        }
        if (string.IsNullOrWhiteSpace(protoRoot))
        {
            err = "Proto Root 不能为空。";
            return false;
        }
        if (!Directory.Exists(protoAbs))
        {
            err = $"找不到 Proto Root 目录：\n{protoAbs}";
            return false;
        }
        if (enableGrpc && !string.IsNullOrWhiteSpace(grpcPluginPath))
        {
            string grpcAbs = ResolvePath(grpcPluginPath);
            if (!File.Exists(grpcAbs))
            {
                err = $"gRPC 插件路径不存在：\n{grpcAbs}";
                return false;
            }
        }
        err = null;
        return true;
    }

    private void PrepareRunState()
    {
        isRunning = true;
        cancelRequested = false;
        startTime = DateTime.Now;

        totalCount = 0;
        compiledCount = 0;
        skippedCount = 0;
        failedCount = 0;

        currentFile = "-";
        statusText = "Starting";
        progress01 = 0.01f;

        AppendLog("\n================ RUN START ================");
        AppendLog("[TIME] " + DateTime.Now);
    }

    private void TryCancelProcess(bool forceKill)
    {
        try
        {
            cancelRequested = true;
            cts?.Cancel();

            if (forceKill && runningProcess != null && !runningProcess.HasExited)
                runningProcess.Kill();
        }
        catch (Exception ex)
        {
            AppendLog("[WARN] Cancel failed: " + ex.Message);
        }
    }

    private void AppendLog(string msg)
    {
        lock (logLock)
        {
            logBuffer.AppendLine(msg);
            // 防止日志无限膨胀（保留最后约 20000 行的粗略策略）
            if (logBuffer.Length > 2_000_000)
            {
                logBuffer.Remove(0, 500_000);
            }
        }
        // 同步到 Console（可按需关闭）
        Debug.Log("[PROTO] " + msg);
    }

    private static void DrawPathField(ref string value, string label, string panelTitle, bool isFolder)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            value = EditorGUILayout.TextField(label, value, GUILayout.Width(800));

            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string projectRoot = GetProjectRoot();
                string currentAbs = ResolvePath(value);

                if (isFolder)
                {
                    string selected = EditorUtility.OpenFolderPanel(panelTitle, Directory.Exists(currentAbs) ? currentAbs : projectRoot, "");
                    if (!string.IsNullOrEmpty(selected))
                        value = ToRelativeIfUnderProject(selected, projectRoot);
                }
                else
                {
                    string dir = Directory.Exists(Path.GetDirectoryName(currentAbs) ?? "") ? Path.GetDirectoryName(currentAbs) : projectRoot;
                    string ext = Path.GetExtension(currentAbs);
                    string selected = EditorUtility.OpenFilePanel(panelTitle, dir, string.IsNullOrEmpty(ext) ? "*" : ext.TrimStart('.'));
                    if (!string.IsNullOrEmpty(selected))
                        value = ToRelativeIfUnderProject(selected, projectRoot);
                }
            }
        }
    }

    private static string GetProjectRoot()
    {
        return Directory.GetParent(Application.dataPath)!.FullName;
    }

    private static string ResolvePath(string p)
    {
        if (string.IsNullOrWhiteSpace(p)) return "";
        if (Path.IsPathRooted(p)) return Path.GetFullPath(p);
        return Path.GetFullPath(Path.Combine(GetProjectRoot(), p));
    }

    private static string ToRelativeIfUnderProject(string absPath, string projectRoot)
    {
        string full = Path.GetFullPath(absPath).Replace("\\", "/");
        string root = Path.GetFullPath(projectRoot).Replace("\\", "/");
        if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return full.Substring(root.Length).TrimStart('/');
        return absPath;
    }

    private void LoadPrefs()
    {
        batPath = EditorPrefs.GetString(KeyBatPath, "Tools/gen_proto_cs_advanced.bat");
        protoRoot = EditorPrefs.GetString(KeyProtoRoot, "Proto");
        outRoot = EditorPrefs.GetString(KeyOutRoot, "Assets/Scripts/Generated/Proto");
        md5StatePath = EditorPrefs.GetString(KeyMd5StatePath, "Assets/Scripts/Generated/.MD5");
        protocPath = EditorPrefs.GetString(KeyProtocPath, "");
        importRoot = EditorPrefs.GetString(KeyImportRoot, "Proto");
        enableGrpc = EditorPrefs.GetBool(KeyEnableGrpc, false);
        grpcPluginPath = EditorPrefs.GetString(KeyGrpcPluginPath, "");
        cleanOutput = EditorPrefs.GetBool(KeyCleanOutput, false);
        onlyChanged = EditorPrefs.GetBool(KeyOnlyChanged, true);
    }

    private void SavePrefs()
    {
        EditorPrefs.SetString(KeyBatPath, batPath ?? "");
        EditorPrefs.SetString(KeyProtoRoot, protoRoot ?? "");
        EditorPrefs.SetString(KeyOutRoot, outRoot ?? "");
        EditorPrefs.SetString(KeyMd5StatePath, md5StatePath ?? "");
        EditorPrefs.SetString(KeyProtocPath, protocPath ?? "");
        EditorPrefs.SetString(KeyImportRoot, importRoot ?? "");
        EditorPrefs.SetBool(KeyEnableGrpc, enableGrpc);
        EditorPrefs.SetString(KeyGrpcPluginPath, grpcPluginPath ?? "");
        EditorPrefs.SetBool(KeyCleanOutput, cleanOutput);
        EditorPrefs.SetBool(KeyOnlyChanged, onlyChanged);
    }

    private void ResetDefaults()
    {
        batPath = "Tools/gen_proto_cs_advanced.bat";
        protoRoot = "Proto";
        outRoot = "Assets/Scripts/Generated/Proto";
        md5StatePath = "Assets/Scripts/Generated/.MD5";
        protocPath = "";
        importRoot = "Proto";
        enableGrpc = false;
        grpcPluginPath = "";
        cleanOutput = false;
        onlyChanged = true;
    }

    private async Task GenerateProtoCmdsAsync()
    {
        string protoPath = ResolvePath(protoRoot);
        if (!Directory.Exists(protoPath))
        {
            Debug.LogError($"Proto 文件目录不存在: {protoPath}");
            return;
        }
        statusText = "Generating Proto Cmds";
        string outDir = ResolvePath(outRoot);
        string cmdsPath = Path.Combine(outDir, "ProtoCmds.cs");
        string lookupPath = Path.Combine(outDir, "ProtoCmdsLookup.cs");

        if (File.Exists(cmdsPath)) File.Delete(cmdsPath);
        if (File.Exists(lookupPath)) File.Delete(lookupPath);
        Directory.CreateDirectory(outDir);

        // Exclude google well-known types
        string[] excludedFolders = new[] { "google" };

        var allProtoFiles = Directory.GetFiles(protoPath, "*.proto", SearchOption.AllDirectories);
        var protoFiles = allProtoFiles
            .Where(f => !excludedFolders.Any(ex =>
                f.Replace("\\", "/").Contains($"/{ex}/")))
            .ToArray();

        Debug.Log($"Proto files: total={allProtoFiles.Length}, filtered={protoFiles.Length}");

        // ============ 1. Generate ProtoCmds.cs (constants only) ============
        var sbCmds = new StringBuilder();
        sbCmds.AppendLine("// Auto-generated by ProtoBatchWindowAsync");
        sbCmds.AppendLine("// DO NOT EDIT MANUALLY");
        sbCmds.AppendLine("using System;");
        sbCmds.AppendLine();
        sbCmds.AppendLine("public static partial class ProtoCmds {");

        // ============ 2. Generate ProtoCmdsLookup.cs (reverse lookup) ============
        var sbLookup = new StringBuilder();
        sbLookup.AppendLine("// Auto-generated by ProtoBatchWindowAsync");
        sbLookup.AppendLine("// DO NOT EDIT MANUALLY");
        sbLookup.AppendLine("using System;");
        sbLookup.AppendLine("using System.Collections.Generic;");
        sbLookup.AppendLine();
        sbLookup.AppendLine("public static partial class ProtoCmds {");
        sbLookup.AppendLine();
        sbLookup.AppendLine("    public static void RegisterAll()");
        sbLookup.AppendLine("    {");

        ushort baseCmd = 1000;
        progress01 = 0.01f;
        int index = 0;

        foreach (var file in protoFiles)
        {
            index++;
            progress01 = Mathf.Clamp01((float)index / protoFiles.Length);
            string fileName = Path.GetFileNameWithoutExtension(file);
            sbCmds.AppendLine($"    // From {fileName}.proto");
            string namespaceStr = "";
            string[] lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                string trim = line.Trim();
                if (string.IsNullOrEmpty(trim)) continue;

                if (string.IsNullOrEmpty(namespaceStr) && trim.StartsWith("package"))
                {
                    namespaceStr = trim.Substring("package ".Length).Trim().TrimEnd(';');
                    if (!string.IsNullOrEmpty(namespaceStr))
                        namespaceStr = char.ToUpper(namespaceStr[0]) + namespaceStr.Substring(1);
                }

                if (trim.StartsWith("message "))
                {
                    string cls = trim.Substring("message ".Length).Trim();
                    int braceIndex = cls.IndexOfAny(new[] { '{', ' ', '\t' });
                    if (braceIndex >= 0) cls = cls.Substring(0, braceIndex);
                    cls = new string(cls.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray());

                    ushort cmd = baseCmd++;
                    sbCmds.AppendLine($"    public const ushort {cls} = {cmd};");

                    // Reverse lookup entry
                    string typeExpr = string.IsNullOrEmpty(namespaceStr)
                        ? $"typeof({cls})"
                        : $"typeof({namespaceStr}.{cls})";
                    sbLookup.AppendLine($"        TypeCmdMap.Add(typeof({namespaceStr}.{cls}), {cmd});");
                }
            }

            baseCmd = (ushort)(((baseCmd / 1000) + 1) * 1000);
            sbCmds.AppendLine();
        }

        sbCmds.AppendLine("}");
        sbLookup.AppendLine("    }");
        sbLookup.AppendLine("}");

        File.WriteAllText(cmdsPath, sbCmds.ToString(), new UTF8Encoding(true));
        File.WriteAllText(lookupPath, sbLookup.ToString(), new UTF8Encoding(true));
        AssetDatabase.Refresh();
        ShowNotification(new GUIContent($"Proto Cmds generated -> {cmdsPath}"));
    }
}


#endif