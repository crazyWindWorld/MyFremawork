using UnityEngine;

namespace Manager.UIManager
{
    /// <summary>
    /// Base class for generated UGUINodeProvider components.
    /// Place on the prefab root. UIWindow.OnAwake() will locate it via
    /// ViewObject.GetComponent&lt;T&gt;() and store it in NodeProvider.
    /// All serialized control references live in the generated subclass.
    /// </summary>
    public abstract class UINodeProvider : MonoBehaviour
    {
        [SerializeField, HideInInspector] private bool allowManualReferenceEdit;

        public bool AllowManualReferenceEdit
        {
            get => allowManualReferenceEdit;
            set => allowManualReferenceEdit = value;
        }
    }
}
