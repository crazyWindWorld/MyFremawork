using Manager.UIManager;
using UnityEditor;

namespace Manager.UIManager.Editor
{
    [CustomEditor(typeof(UINodeProvider), true)]
    public class UINodeProviderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var provider = (UINodeProvider)target;

            EditorGUI.BeginChangeCheck();
            bool allowManualEdit = EditorGUILayout.Toggle("允许手动修改引用", provider.AllowManualReferenceEdit);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(provider, "Toggle Manual Reference Edit");
                provider.AllowManualReferenceEdit = allowManualEdit;
                EditorUtility.SetDirty(provider);
            }

            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(!provider.AllowManualReferenceEdit);
            DrawPropertiesExcluding(serializedObject, "m_Script", "allowManualReferenceEdit");
            EditorGUI.EndDisabledGroup();
        }
    }
}
