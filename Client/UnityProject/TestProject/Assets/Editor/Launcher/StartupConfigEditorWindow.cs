using System.IO;
using System.Text.RegularExpressions;
using Fuel.Launcher.Config;
using UnityEditor;
using UnityEngine;

namespace Fuel.Launcher.Editor
{
    public sealed class StartupConfigEditorWindow : EditorWindow
    {
        private const string ConfigPath = "Assets/Resources/StartupConfig.json";
        private static readonly Regex SemVerRegex = new Regex(@"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$", RegexOptions.Compiled);

        private LocalStartupConfig _config;
        private Vector2 _scroll;
        private string _message;

        [MenuItem("Tools/Fuel/Startup Config")]
        public static void Open()
        {
            GetWindow<StartupConfigEditorWindow>("Startup Config");
        }

        private void OnEnable()
        {
            LoadOrCreate();
        }

        private void OnGUI()
        {
            if (_config == null)
                LoadOrCreate();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scroll.scrollPosition;

                EditorGUILayout.LabelField("Local Startup Config", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(ConfigPath, EditorStyles.miniLabel);
                EditorGUILayout.Space();

                _config.appVersion = EditorGUILayout.TextField("App Version", _config.appVersion);
                _config.versionUrl = EditorGUILayout.TextField("Version URL", _config.versionUrl);
                _config.packageName = EditorGUILayout.TextField("Package Name", _config.packageName);
                _config.defaultHostUrl = EditorGUILayout.TextField("Default Host URL", _config.defaultHostUrl);
                _config.fallbackHostUrl = EditorGUILayout.TextField("Fallback Host URL", _config.fallbackHostUrl);
                _config.hotUpdateDllPath = EditorGUILayout.TextField("HotUpdate DLL Path", _config.hotUpdateDllPath);
                _config.configPathPattern = EditorGUILayout.TextField("Config Path Pattern", _config.configPathPattern);
                _config.hotUpdateEntryType = EditorGUILayout.TextField("HotUpdate Entry Type", _config.hotUpdateEntryType);
                _config.hotUpdateEntryMethod = EditorGUILayout.TextField("HotUpdate Entry Method", _config.hotUpdateEntryMethod);

                EditorGUILayout.Space();
                DrawAotDllList();
                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reload"))
                        LoadOrCreate();

                    if (GUILayout.Button("Save"))
                        Save();
                }

                if (!string.IsNullOrEmpty(_message))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.HelpBox(_message, MessageType.Info);
                }
            }
        }

        private void DrawAotDllList()
        {
            EditorGUILayout.LabelField("AOT Metadata DLL Paths", EditorStyles.boldLabel);

            if (_config.aotMetadataDllPaths == null)
                _config.aotMetadataDllPaths = new string[0];

            int removeIndex = -1;
            for (int i = 0; i < _config.aotMetadataDllPaths.Length; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _config.aotMetadataDllPaths[i] = EditorGUILayout.TextField($"Element {i}", _config.aotMetadataDllPaths[i]);
                    if (GUILayout.Button("-", GUILayout.Width(24)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0)
            {
                var list = new System.Collections.Generic.List<string>(_config.aotMetadataDllPaths);
                list.RemoveAt(removeIndex);
                _config.aotMetadataDllPaths = list.ToArray();
            }

            if (GUILayout.Button("Add AOT DLL Path"))
            {
                var list = new System.Collections.Generic.List<string>(_config.aotMetadataDllPaths) { string.Empty };
                _config.aotMetadataDllPaths = list.ToArray();
            }
        }

        private void LoadOrCreate()
        {
            EnsureDirectory();

            if (!File.Exists(ConfigPath))
            {
                _config = CreateDefaultConfig();
                Save();
                _message = "Created default StartupConfig.json.";
                return;
            }

            var json = File.ReadAllText(ConfigPath);
            _config = JsonUtility.FromJson<LocalStartupConfig>(json) ?? CreateDefaultConfig();
            _message = "Loaded StartupConfig.json.";
        }

        private void Save()
        {
            var error = ValidateConfig();
            if (!string.IsNullOrEmpty(error))
            {
                _message = error;
                return;
            }

            EnsureDirectory();
            File.WriteAllText(ConfigPath, JsonUtility.ToJson(_config, true));
            AssetDatabase.ImportAsset(ConfigPath);
            AssetDatabase.Refresh();
            _message = "Saved StartupConfig.json.";
        }

        private string ValidateConfig()
        {
            if (string.IsNullOrWhiteSpace(_config.appVersion)) return "App Version is required.";
            if (!SemVerRegex.IsMatch(_config.appVersion)) return "App Version must be SemVer, for example 1.0.0.";
            if (string.IsNullOrWhiteSpace(_config.versionUrl)) return "Version URL is required.";
            if (string.IsNullOrWhiteSpace(_config.packageName)) return "Package Name is required.";
            if (string.IsNullOrWhiteSpace(_config.hotUpdateDllPath)) return "HotUpdate DLL Path is required.";
            if (string.IsNullOrWhiteSpace(_config.hotUpdateEntryType)) return "HotUpdate Entry Type is required.";
            return null;
        }

        private static void EnsureDirectory()
        {
            var directory = Path.GetDirectoryName(ConfigPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        private static LocalStartupConfig CreateDefaultConfig()
        {
            return new LocalStartupConfig
            {
                appVersion = "1.0.0",
                versionUrl = "https://cdn.example.com/version.json",
                packageName = "Main",
                defaultHostUrl = "https://cdn.example.com/game/Main",
                fallbackHostUrl = "https://backup.example.com/game/Main",
                hotUpdateDllPath = "Assets/AssetsPackage/Main/HotUpdate/HotUpdate.dll.bytes",
                aotMetadataDllPaths = new[]
                {
                    "Assets/AssetsPackage/Main/HotUpdate/mscorlib.dll.bytes",
                    "Assets/AssetsPackage/Main/HotUpdate/System.dll.bytes",
                    "Assets/AssetsPackage/Main/HotUpdate/System.Core.dll.bytes"
                },
                configPathPattern = "Assets/AssetsPackage/Main/Configs/{0}",
                hotUpdateEntryType = "HotUpdate.GameEntry.HotUpdateEntry",
                hotUpdateEntryMethod = "StartAsync"
            };
        }
    }
}
