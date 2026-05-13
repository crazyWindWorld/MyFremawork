## Why

项目目前没有 UI 管理框架，UI 相关代码直接耦合在 MonoBehaviour 中（如 TestSocket.cs），无法统一管理 UI 的打开/关闭/层级/生命周期。需要一个轻量、可扩展的 UI 管理器，遵循现有框架的纯 C# 单例模式（与 AudioManager 一致），不依赖 MonoBehaviour，并支持自定义 UI 分层和基于栈的 UI 流程管理。

## What Changes

- 新增 `UIManager` 纯 C# 单例，继承 `Singleton<UIManager>`，与 AudioManager 保持一致的架构风格
- 新增 `UILayer` 分层枚举和可扩展的分层配置机制，支持自定义 UI 层级（如 Background、Normal、Popup、Top 等）
- 新增 `UIPanelBase` 抽象基类，定义 UI 面板的通用生命周期接口（OnOpen、OnClose、OnPause、OnResume 等），不依赖 MonoBehaviour
- 使用 `List<UIPanelBase>` 模拟栈来管理每个层级内的 UI 面板，支持 Push/Pop 语义
- 新增面板注册与加载机制，通过面板 ID 注册和打开 UI，支持 Resources 或自定义加载方式

## Capabilities

### New Capabilities
- `ui-layer-management`: 自定义 UI 分层系统，支持枚举定义层级、按层级组织 UI 面板
- `ui-panel-lifecycle`: UI 面板基类与生命周期管理，定义面板的打开/关闭/暂停/恢复等回调接口
- `ui-stack-management`: 基于 List 模拟栈的 UI 面板管理，支持 Push/Pop/Peek 等操作，管理面板的显示层级和遮挡关系

### Modified Capabilities
（无现有能力需要修改）

## Impact

- 新增目录：`Assets/Scripts/Manager/UIManager/`（与 AudioManager 平级）
- 新增命名空间：`Fuel.Manager.UIManager`
- 依赖现有 `Fuel.Singleton.Singleton<T>` 基类
- 面板加载层依赖 Unity `Resources` API，但 UIManager 核心逻辑与 MonoBehaviour 解耦
- 后续可扩展为 Addressables 加载或 AssetBundle 加载
