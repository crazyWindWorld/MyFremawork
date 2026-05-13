# UI 管理框架 Spec

## Why
当前项目缺乏统一的 UI 管理机制，界面层级混乱，无法精确控制界面的显示层级、栈管理以及资源的动态加载与释放。需要一个与 Monobehaviour 完全解耦的通用 UI 管理框架来提升开发效率。

## What Changes
- 新增 UIManager 核心管理类
- 新增 UILayer 层级配置系统
- 新增 UIWindow 窗口基类（不继承 Monobehaviour）
- 新增 UIWindowData 窗口数据基类（用于界面数据注入）
- 新增 UIStack 栈管理器
- 新增 UIEvent 事件注册机制
- 新增 UIResourceManager 资源动态加载管理
- 新增 UIEditorDebugWindow 编辑器调试窗口
- 新增 UIAutoBindTool 编辑器自动绑定工具

## Impact
- Affected specs: UI 管理系统
- Affected code: 新增框架文件，无现有代码受影响

## ADDED Requirements

### Requirement: 核心管理器
UIManager 负责整个 UI 框架的初始化、更新、销毁，协调各个子系统工作。

#### Scenario: 初始化
- **WHEN** 调用 `UIManager.Initialize()`
- **THEN** 创建 Canvas 根节点、初始化层级配置、初始化栈管理器、初始化资源管理器

### Requirement: Monobehaviour 完全解耦
UIWindow 不继承 Monobehaviour，使用独立的生命周期管理。

#### Scenario: 窗口生命周期
- **WHEN** 窗口被打开时调用 `OnShow()`
- **THEN** 触发 OnShow 事件、执行事件注册
- **WHEN** 窗口被关闭时调用 `OnHide()`
- **THEN** 触发 OnHide 事件、执行事件注销

### Requirement: 自定义层级管理
通过 UILayerConfig（ScriptableObject 单例）定义层级，支持配置多个层级及其 Z 轴间距。

#### Scenario: 层级配置
- **WHEN** 定义 UILayerConfig 时设置层级名称和基础 Z 值
- **THEN** 界面根据层级配置自动设置 Z 轴位置
- **WHEN** 访问 UILayerConfig.Instance 时
- **THEN** 返回全局唯一的层级配置实例

### Requirement: List 栈管理
使用 List 模拟栈，所有打开的界面压入同一个栈。

#### Scenario: 栈操作
- **WHEN** 调用 `Push(window)` 打开界面
- **THEN** 界面加入栈顶
- **WHEN** 调用 `Pop()` 关闭栈顶界面
- **THEN** 栈顶界面移除并触发销毁

#### Scenario: 重复打开栈内窗口
- **WHEN** 打开一个已存在于栈中的界面时
- **THEN** 从栈顶持续出栈直到该界面位于栈顶
- **THEN** 触发该界面的 OnShow 事件

### Requirement: 层级过滤清理
当新界面层级低于栈顶界面时，清空整个栈后再入栈。

#### Scenario: 层级过滤
- **WHEN** 栈顶界面层级为 3，新打开界面层级为 1
- **THEN** 清空整个栈，然后新界面入栈

### Requirement: 动态栈数量控制
可配置最大栈内数量，超过时只保留映射关系并释放资源。

#### Scenario: 栈数量超限
- **WHEN** 栈内数量超过 maxStackCount
- **THEN** 保留 UIWindow 引用映射，释放界面资源（GameObject 或视图）
- **WHEN** 需要从已释放的界面出栈时
- **THEN** 通过映射重新加载资源

### Requirement: 事件注册注销接口
每个面板提供事件注册和注销接口，在显示/隐藏时自动调用。

#### Scenario: 事件生命周期
- **WHEN** 调用 `RegisterEvents()` 时
- **THEN** 注册面板所需的事件监听
- **WHEN** 调用 `UnregisterEvents()` 时
- **THEN** 注销面板的事件监听

### Requirement: 窗口数据基类
提供 UIWindowData 基类用于界面数据注入，子类可通过继承传递任意类型数据。

#### Scenario: 数据注入
- **WHEN** 调用 `OpenWindow<T>(UIWindowData data)` 打开界面时传入数据
- **THEN** 数据通过 OnShow 方法传递给目标界面
- **WHEN** 界面实现 `OnShow(UIWindowData data)` 时
- **THEN** 界面接收并处理传入的数据

### Requirement: 编辑器调试窗口
通过编辑器窗口实时查看 UI 层级配置和栈运行状态。

#### Scenario: 查看层级数据
- **WHEN** 打开 UIEditorDebugWindow 编辑器窗口
- **THEN** 显示所有层级配置信息（层级 ID、名称、Z 值）
- **WHEN** 运行时查看栈数据
- **THEN** 显示当前栈内所有窗口信息及栈深度

### Requirement: 编辑器自动绑定工具
提供 UIAutoBindTool 编辑器扩展，支持手动绑定和自动绑定 View 组件到 Panel 字段。

#### Scenario: 手动绑定
- **WHEN** 在编辑器中选中 Prefab 并打开绑定窗口
- **THEN** 显示所有可绑定字段，支持手动拖拽 GameObject/Component 进行绑定
- **WHEN** 保存绑定配置时
- **THEN** 将绑定信息序列化存储

#### Scenario: 自动绑定
- **WHEN** 触发自动绑定时
- **THEN** 根据绑定配置自动将 View 层组件赋值到 Panel 对应字段
- **WHEN** Panel 字段名称与 View 子节点名称匹配时
- **THEN** 可自动识别并绑定（命名约定绑定）

#### Scenario: 名称规则绑定
- **WHEN** 定义名称规则（如 Btn→Button, Img→Image）时
- **THEN** UI 节点名称前缀匹配规则时自动推断绑定类型
- **WHEN** 运行时进行自动绑定时
- **THEN** 解析 UI 节点名称前缀，匹配规则后绑定对应组件类型
- **WHEN** 规则可自定义配置时
- **THEN** 用户可添加/修改/删除前缀与组件类型的映射关系

#### Scenario: Hierarchy 扩展绑定
- **WHEN** 在 Hierarchy 面板中右键点击 UI 节点时
- **THEN** 显示 "UI Bind" 右键菜单选项
- **WHEN** 选择 "UI Bind" 菜单
- **THEN** 弹出组件类型选择列表和多选界面
- **WHEN** 选择组件类型（如 Image、RectTransform、Button）并输入字段名时
- **THEN** 生成绑定配置，记录节点路径、组件类型和自定义字段名
- **WHEN** 同一节点需要绑定多个组件时
- **THEN** 可多次调用菜单为同一节点添加多个绑定配置

## MODIFIED Requirements

无

## REMOVED Requirements

无
