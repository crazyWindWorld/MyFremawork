#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Manager.UIManager;

namespace Manager.UIManager.Editor
{
    public class UIEditorDebugWindow : EditorWindow
    {
        private Vector2 _layerScrollPos;
        private Vector2 _stackScrollPos;
        private bool _showLayers = true;
        private bool _showStack = true;
        private float _updateInterval = 0.5f;
        private float _lastUpdateTime = 0f;

        [MenuItem("Window/UI Manager/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIEditorDebugWindow>("UI Debug");
            window.minSize = new Vector2(300, 200);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10);
            DrawHeader();
            EditorGUILayout.Space(5);

            if (Application.isPlaying)
            {
                if (Time.realtimeSinceStartup - _lastUpdateTime > _updateInterval)
                {
                    _lastUpdateTime = Time.realtimeSinceStartup;
                    Repaint();
                }
            }

            _showLayers = EditorGUILayout.Foldout(_showLayers, "Layer Config", true);
            if (_showLayers)
            {
                DrawLayerConfig();
            }

            EditorGUILayout.Space(5);

            _showStack = EditorGUILayout.Foldout(_showStack, "Stack Info", true);
            if (_showStack)
            {
                DrawStackInfo();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("UI Manager Debug", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", GUILayout.Width(60)))
            {
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Mode: {(Application.isPlaying ? "Playing" : "Editting")}");
            GUILayout.FlexibleSpace();
            if (Application.isPlaying && UIManager.Instance != null)
            {
                GUILayout.Label($"Stack Count: {UIManager.Instance.Stack.Count}");
                GUILayout.Label($"Max: {UIManager.Instance.MaxStackCount}");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawLayerConfig()
        {
            EditorGUI.indentLevel++;
            _layerScrollPos = EditorGUILayout.BeginScrollView(_layerScrollPos, GUILayout.Height(200));

            EditorGUILayout.LabelField("UILayer Enum Values:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            foreach (UILayer layer in System.Enum.GetValues(typeof(UILayer)))
            {
                EditorGUILayout.BeginHorizontal();
                int layerValue = (int)layer;
                EditorGUILayout.LabelField($"[{layerValue}] {layer}", GUILayout.Width(150));
                EditorGUILayout.LabelField($"Z: {UILayerHelper.GetZ(layer)}", GUILayout.Width(80));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUI.indentLevel--;
        }

        private void DrawStackInfo()
        {
            EditorGUI.indentLevel++;
            _stackScrollPos = EditorGUILayout.BeginScrollView(_stackScrollPos, GUILayout.Height(200));

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see stack info.", MessageType.Info);
            }
            else
            {
                var manager = UIManager.Instance;
                if (manager != null)
                {
                    var stack = manager.Stack;
                    EditorGUILayout.LabelField($"Stack Count: {stack.Count}");
                    EditorGUILayout.LabelField($"Max Stack Size: {manager.MaxStackCount}");
                    EditorGUILayout.Space(5);

                    if (stack.Count == 0)
                    {
                        EditorGUILayout.HelpBox("Stack is empty.", MessageType.None);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Stack Contents (Top to Bottom):", EditorStyles.boldLabel);
                        for (int i = stack.Count - 1; i >= 0; i--)
                        {
                            var window = stack.Stack[i];
                            EditorGUILayout.BeginHorizontal();

                            string status = window.IsRelease ? " [Released]" : (window.IsShow ? " [Active]" : " [Hidden]");
                            string layerName = window.LayerId.ToString();

                            EditorGUILayout.LabelField($"[{i}] {window.WindowId}{status}", GUILayout.Width(200));
                            EditorGUILayout.LabelField($"Layer: {layerName}");

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("UIManager not initialized!", MessageType.Error);
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUI.indentLevel--;
        }
    }
}
#endif
