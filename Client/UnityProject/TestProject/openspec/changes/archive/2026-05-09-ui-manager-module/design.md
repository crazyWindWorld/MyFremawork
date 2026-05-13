## Context

项目基于 Fuel 框架，采用纯 C# 单例模式管理各类服务（AudioManager 使用 `Singleton<T>`，NetworkManager 使用 `MonoSingleton<T>`）。目前没有任何 UI 管理基础设施。用户需要一个不依赖 MonoBehaviour 的 UI 管理器，支持自定义分层和栈式管理。

关键约束：
- 必须继承 `Singleton<T>`（纯 C# 单例），与 AudioManager 风格一致
- UIManager 不挂载到 GameObject，生命周期由外部驱动（Init / Update / Dispose）
- 面板基类 `UIPanelBase` 同样不应强制依赖 MonoBehaviour，面板可以是纯逻辑对象

## Goals / Non-Goals

**Goals:**
- 提供可自定义的 UI 分层机制（枚举 + 字典映射）
- 每个层级独立维护一个 `List<UIPanelBase>` 模拟栈
- 定义清晰的面板生命周期回调（OnOpen / OnClose / OnPause / OnResume）
- 通过面板 ID 注册/查找/打开/关闭面板
- 支持纯 C# 面板对象（不强制 MonoBehaviour）

**Non-Goals:**
- 不实现具体的 UI 渲染逻辑（渲染由具体面板自行处理）
- 不内置对象池（后续可扩展）
- 不实现动画系统
- 不处理 UI 事件分发（如点击穿透等）

## Decisions

### 1. 继承 `Singleton<T>` 而非 `MonoSingleton<T>`

**选择**: UIManager 继承 `Fuel.Singleton.Singleton<T>`

**理由**: 用户明确要求不与 MonoBehaviour 耦合。AudioManager 已验证此模式可行——通过外部调用 `Init()` / `Update(float dt)` / `Dispose()` 驱动生命周期，无需挂载到 GameObject。

**替代方案**: 继承 `MonoSingleton<T>` 可自动获得 Unity 生命周期回调，但会引入 MonoBehaviour 耦合，违背需求。

### 2. UILayer 使用枚举 + 自定义扩展

**选择**: 定义 `UILayer` 枚举提供默认层级（Background、Normal、Popup、Top），同时 UIManager 内部使用 `Dictionary<int, LayerStack>` 映射，允许用户通过 int 值注册自定义层级。

**理由**: 枚举提供类型安全的默认层级；int 字典映射允许运行时扩展，不需修改枚举即可添加自定义层级。

**替代方案**: 纯字符串层级——灵活但缺乏类型安全，容易拼写错误。

### 3. List 模拟栈

**选择**: 每个层级使用 `List<StackEntry>` 模拟栈，最后一个元素视为栈顶。`StackEntry` 是一个包装结构，持有 `int PanelId` 和 `UIPanelBase Panel`（可为 null）。

**理由**: List 支持随机访问（方便遍历和查找）、中间移除（弹出非栈顶面板）、以及栈语义（Add = Push，RemoveAt(Count-1) = Pop）。相比 Stack 更灵活。使用 StackEntry 而非直接持有 UIPanelBase，是为了支持栈容量管理中"释放面板资源但保留栈位"的需求（见 Decision 7）。

**StackEntry 状态**:
- **活跃**: `Panel != null`，面板实例存在，可直接操作
- **已释放**: `Panel == null`，仅保留 `PanelId`，面板资源已释放，需要重新加载

**操作语义**:
- `Push(panel)` → 跨层级比较：若当前全局栈顶面板的层级高于新面板的层级，则先释放整个栈（所有面板依次 OnClose），再将新面板入栈。否则正常 `list.Add(new StackEntry(panel))`，原栈顶面板收到 OnPause 回调。入栈后检查栈容量，超出则释放栈底面板
- `Pop()` → 移除栈顶 StackEntry。若栈顶面板已释放（Panel == null），则通过 PanelId 从注册表重新加载面板实例后再执行 OnClose。新栈顶若已释放也需重新加载后执行 OnResume
- `Peek()` → 返回栈顶 StackEntry，若面板已释放则先重新加载
- `Remove(panel)` → 从任意位置移除，处理前后面板的回调

### 4. UIPanelBase 为抽象类而非接口

**选择**: `UIPanelBase` 作为抽象类，持有面板 ID、所属层级、是否打开等状态字段，并提供虚方法供子类重写。

**理由**: 面板需要统一的状态管理（ID、Layer、IsOpen），抽象类可以内置这些字段和默认行为，减少子类样板代码。接口无法持有状态。

**生命周期方法**:
- `OnOpen(object param)` — 面板打开时调用，可传入参数
- `OnClose()` — 面板关闭时调用
- `OnPause()` — 被新面板遮挡时调用
- `OnResume()` — 从遮挡恢复时调用
- `OnDestroy()` — 面板销毁时调用

### 5. 跨层级入栈时的栈清理规则

**选择**: 当新面板入栈时，UIManager 检查当前全局栈顶面板（即所有层级中最后入栈的面板）的层级。若栈顶面板的层级值高于新面板的层级值，则先释放当前栈中的所有面板（依次调用 OnClose），再将新面板压入其所属层级的栈中。

**规则**: `newPanel.Layer < globalTop.Layer` → 清栈再入栈

**理由**: 高层级（如 Popup）代表更高的显示优先级。当从高层级面板切换到低层级面板时（如从 Popup 回到 Normal），说明高层级流程已结束，应当完整清理栈状态，避免残留的高层级面板遮挡或干扰新的低层级面板。

**示例**:
- 栈状态: `[Normal:A] [Popup:B]`，栈顶是 Popup:B（层级高于 Normal）
- 打开 Normal:C → Popup:B 的层级 > Normal:C 的层级 → 释放整个栈（B.OnClose, A.OnClose）→ 入栈 Normal:C
- 最终栈状态: `[Normal:C]`

### 7. 栈容量动态管理与资源释放

**选择**: 每个层级栈支持设置最大容量（`MaxCapacity`），可通过 `SetLayerCapacity(layer, capacity)` 动态调整。当栈中面板数量超过容量时，从栈底开始释放超出的面板：调用面板的 `OnDestroy()` 释放资源，将 StackEntry 的 Panel 置为 null，仅保留 PanelId。

**规则**: 栈溢出时按 FIFO 顺序释放栈底面板（最早入栈的最先释放）

**出栈时的懒加载**: 当 Pop 或 Peek 操作命中一个已释放的 StackEntry（Panel == null）时，UIManager 通过注册表（`Dictionary<int, Type>` 或 `Dictionary<int, Func<UIPanelBase>>`）中的映射关系重新创建面板实例，恢复其状态后再执行正常的生命周期回调。

**理由**: 游戏中 UI 面板可能累积大量栈（如多层弹窗、多级菜单），每个面板持有 Prefab 引用或渲染资源。通过容量限制 + 懒加载，可以在内存紧张时自动释放不可见的面板资源，同时保持栈结构完整，用户回退时自动重新加载，对业务层透明。

**数据流**:
```
入栈 → Push(panel) → list.Add(entry)
  → list.Count > MaxCapacity?
    → 是: 取栈底 entry → entry.Panel.OnDestroy() → entry.Panel = null
      → 保留 entry 在 list 中（仅 PanelId）

出栈 → Pop() → 取栈顶 entry
  → entry.Panel == null?
    → 是: entry.Panel = CreateFromRegistry(entry.PanelId) → entry.Panel.OnOpen()
  → entry.Panel.OnClose() → list.Remove(entry)
```

**容量配置**:
- 默认容量为 -1（不限制，面板永不自动释放）
- 每个层级可独立设置容量
- `SetLayerCapacity(layer, -1)` 可取消限制

### 8. 面板注册与加载分离

**选择**: UIManager 提供 `Register<T>(panelId)` 注册面板类型，`Open(panelId, param)` 时通过 `Activator.CreateInstance<T>()` 创建实例。同时提供 `Register(panelId, panel)` 直接注册已有实例。

**理由**: 类型注册支持延迟创建（首次打开时才实例化），直接注册支持外部创建的面板实例（如从 Prefab 加载后包装）。

**面板加载策略**: 默认使用 `Activator.CreateInstance` 创建纯 C# 面板。如需加载 Unity Prefab，面板子类可在 `OnOpen` 中自行处理，或通过扩展 `IPanelLoader` 接口支持。

## Risks / Trade-offs

- **[纯 C# 面板无法直接操作 Unity UI 元素]** → 面板子类可持有对 GameObject 的引用，在 OnOpen 中自行实例化 Prefab。UIManager 不感知渲染层，保持解耦。
- **[List 模拟栈的中间移除是 O(n)]** → UI 面板数量通常很少（< 50），性能无实际影响。
- **[Activator.CreateInstance 要求无参构造函数]** → 提供 `Register(panelId, panel)` 重载，支持有参构造或工厂模式扩展。
- **[无内置对象池]** → 频繁开关面板可能产生 GC。后续可在 UIManager 中加入 `Dictionary<int, Stack<UIPanelBase>>` 缓存已关闭的面板实例。
- **[跨层级清栈可能导致意外关闭]** → 若业务需要同时保留高低层级面板（如 Popup 上再弹 Normal），可通过自定义 UILayer 枚举值调整优先级顺序，或在面板子类中重写关闭逻辑阻止释放。
- **[已释放面板的状态丢失]** → 面板被释放后仅保留 PanelId，其运行时状态（输入内容、滚动位置等）会丢失。面板子类可通过重写 `OnSaveState()` / `OnRestoreState()` 自定义状态持久化，或通过设置容量为 -1 禁用自动释放。
- **[懒加载延迟]** → 出栈时重新加载已释放面板可能产生短暂卡顿（尤其是 Prefab 较大时）。可通过异步加载或预加载策略缓解。
