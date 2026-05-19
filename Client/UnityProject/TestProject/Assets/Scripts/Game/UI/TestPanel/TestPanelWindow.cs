using Manager.UIManager;

namespace Game.UI.TestPanel
{
    /// <summary>
    /// TestPanel 业务逻辑窗口
    /// 通过 Nodes.xxx 访问绑定的 UI 组件引用
    /// </summary>
    public class TestPanelWindow : UIWindow<TestPanelNodeProvider>
    {
        public override string WindowId => "TestPanel";
        public override UILayer LayerId => UILayer.Normal;

        public override void OnAwake()
        {
            base.OnAwake();
            // 初始化逻辑
        }

        public override void OnShow(UIWindowData data = null)
        {
            base.OnShow(data);
            // 显示逻辑
        }

        public override void OnHide()
        {
            base.OnHide();
            // 隐藏逻辑
        }

        public override void RegisterEvents()
        {
            // 注册事件，例如：
            // Nodes.BtnClose.onClick.AddListener(OnCloseClick);
        }

        public override void UnregisterEvents()
        {
            // 注销事件，例如：
            // Nodes.BtnClose.onClick.RemoveListener(OnCloseClick);
        }
    }
}
