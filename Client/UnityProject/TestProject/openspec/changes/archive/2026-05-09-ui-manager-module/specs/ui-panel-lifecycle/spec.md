## ADDED Requirements

### Requirement: UIPanelBase abstract class
系统 SHALL 提供 `UIPanelBase` 抽象类作为所有 UI 面板的基类。该类持有面板状态字段（PanelId、Layer、IsOpen）并提供生命周期虚方法供子类重写。UIPanelBase 不依赖 MonoBehaviour。

#### Scenario: Panel inherits from UIPanelBase
- **WHEN** 开发者创建自定义面板类继承 UIPanelBase
- **THEN** 面板自动拥有 PanelId、Layer、IsOpen 属性，可重写生命周期方法

#### Scenario: Panel does not require MonoBehaviour
- **WHEN** 实例化一个 UIPanelBase 子类
- **THEN** 实例为纯 C# 对象，不挂载到 GameObject，不依赖 Unity 生命周期

### Requirement: OnOpen lifecycle callback
UIPanelBase SHALL 提供 `OnOpen(object param)` 虚方法，在面板被打开（压入栈顶）时由 UIManager 调用。param 参数由调用者传入，可为 null。

#### Scenario: Open panel with parameters
- **WHEN** 调用 `UIManager.Instance.Open(panelId, userData)` 且面板首次创建
- **THEN** 面板的 `OnOpen(userData)` 被调用，面板可从 param 中获取业务数据

#### Scenario: Open panel without parameters
- **WHEN** 调用 `UIManager.Instance.Open(panelId)` 不传参数
- **THEN** 面板的 `OnOpen(null)` 被调用

### Requirement: OnClose lifecycle callback
UIPanelBase SHALL 提供 `OnClose()` 虚方法，在面板被关闭（从栈中移除）时由 UIManager 调用。面板 SHALL 在此方法中清理自身状态。

#### Scenario: Panel is closed by Pop
- **WHEN** UIManager 执行 Pop 操作移除栈顶面板
- **THEN** 被移除面板的 `OnClose()` 被调用

#### Scenario: Panel is closed by cross-layer stack clearing
- **WHEN** 新面板入栈触发跨层级清栈规则
- **THEN** 被清栈的所有面板依次调用 `OnClose()`

### Requirement: OnPause lifecycle callback
UIPanelBase SHALL 提供 `OnPause()` 虚方法，在面板被新入栈的面板遮挡（不再是栈顶）时调用。

#### Scenario: Panel paused by new panel push
- **WHEN** 当前栈顶面板 A，新面板 B 压入同一层级
- **THEN** 面板 A 的 `OnPause()` 被调用，然后面板 B 的 `OnOpen()` 被调用

### Requirement: OnResume lifecycle callback
UIPanelBase SHALL 提供 `OnResume()` 虚方法，在面板从遮挡状态恢复为栈顶时调用。

#### Scenario: Panel resumes after Pop
- **WHEN** 栈顶面板 B 被 Pop 移除，面板 A 成为新栈顶
- **THEN** 面板 A 的 `OnResume()` 被调用

### Requirement: OnDestroy lifecycle callback
UIPanelBase SHALL 提供 `OnDestroy()` 虚方法，在面板资源被彻底释放时调用（如栈容量溢出释放或 UIManager Dispose）。与 OnClose 不同，OnDestroy 表示面板实例将被丢弃。

#### Scenario: Panel destroyed by capacity overflow
- **WHEN** 栈容量超出 MaxCapacity，栈底面板被释放
- **THEN** 被释放面板的 `OnDestroy()` 被调用，其 StackEntry 的 Panel 置为 null

#### Scenario: Panel destroyed by UIManager dispose
- **WHEN** UIManager 的 `Dispose()` 被调用
- **THEN** 所有面板的 `OnDestroy()` 被调用，所有栈被清空

### Requirement: OnSaveState and OnRestoreState for released panels
UIPanelBase SHALL 提供 `OnSaveState()` 和 `OnRestoreState(object state)` 虚方法，用于面板被容量管理释放前保存状态、重新加载后恢复状态。

#### Scenario: Save state before release
- **WHEN** 面板因栈容量溢出即将被释放
- **THEN** UIManager 先调用 `OnSaveState()`，面板返回需要保存的状态对象，该对象与 PanelId 一起存储

#### Scenario: Restore state after reload
- **WHEN** 已释放的面板因出栈操作被重新加载
- **THEN** UIManager 重新创建面板实例后调用 `OnRestoreState(savedState)`，面板恢复之前保存的状态

### Requirement: Panel registration by type
UIManager SHALL 提供 `Register<T>(int panelId)` 方法注册面板类型。注册后通过 `Open(panelId)` 时使用 `Activator.CreateInstance<T>()` 延迟创建面板实例。

#### Scenario: Register and open panel by type
- **WHEN** 调用 `Register<MyPanel>(1001)` 然后调用 `Open(1001)`
- **THEN** UIManager 创建 MyPanel 实例，调用 `OnOpen(null)`，将其压入对应层级栈

#### Scenario: Open unregistered panel
- **WHEN** 调用 `Open(panelId)` 但该 panelId 未注册
- **THEN** 系统 SHALL 抛出异常或输出错误日志，不执行任何操作

### Requirement: Panel registration by instance
UIManager SHALL 提供 `Register(int panelId, UIPanelBase panel)` 方法直接注册已创建的面板实例。

#### Scenario: Register existing panel instance
- **WHEN** 调用 `Register(1001, existingPanel)` 然后调用 `Open(1001)`
- **THEN** UIManager 使用已注册的 existingPanel 实例，调用 `OnOpen(null)`，将其压入栈
