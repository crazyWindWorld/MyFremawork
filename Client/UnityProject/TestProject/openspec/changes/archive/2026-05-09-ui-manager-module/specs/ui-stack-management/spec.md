## ADDED Requirements

### Requirement: Stack entry wrapper structure
系统 SHALL 提供 `StackEntry` 结构，持有 `int PanelId` 和 `UIPanelBase Panel`（可为 null）。Panel 为 null 时表示已释放状态，仅保留 PanelId 用于后续重新加载。

#### Scenario: Active stack entry
- **WHEN** 面板被正常压入栈
- **THEN** 创建 StackEntry，PanelId 为面板 ID，Panel 为面板实例（非 null）

#### Scenario: Released stack entry
- **WHEN** 面板因容量溢出被释放
- **THEN** StackEntry 的 Panel 被置为 null，PanelId 保持不变

### Requirement: Push operation with cross-layer clearing
UIManager 的 `Open(panelId, param)` 操作 SHALL 实现 Push 语义。入栈前检查全局栈顶面板的层级：若栈顶面板层级高于新面板层级，则先释放整个栈（所有面板依次 OnClose），再将新面板压入其所属层级栈。

#### Scenario: Push to same layer normally
- **WHEN** 当前 Normal 层栈顶为面板 A，打开 Normal 层面板 B
- **THEN** 面板 A 收到 OnPause，面板 B 压入 Normal 层栈顶，B 的 OnOpen 被调用

#### Scenario: Push triggers cross-layer stack clearing
- **WHEN** 当前全局栈顶为 Popup 层面板 B（层级 200），打开 Normal 层面板 C（层级 100）
- **THEN** 释放整个栈（B.OnClose, A.OnClose），面板 C 压入 Normal 层栈，C 的 OnOpen 被调用

#### Scenario: Push with lower layer does not clear higher layer panels already in stack
- **WHEN** 栈中存在 Background 层面板 A，当前无更高层级面板，打开 Normal 层面板 B
- **THEN** 面板 A 收到 OnPause，面板 B 压入 Normal 层栈顶

### Requirement: Pop operation with lazy reload
UIManager 的 `Close(panelId)` 或 `CloseTop()` 操作 SHALL 实现 Pop 语义。移除栈顶 StackEntry，若栈顶面板已释放（Panel == null），则通过注册表重新加载面板实例后再执行 OnClose。

#### Scenario: Pop active panel
- **WHEN** 栈顶面板为活跃状态（Panel != null），执行 Pop
- **THEN** 面板的 OnClose 被调用，从栈中移除，新栈顶（若有）收到 OnResume

#### Scenario: Pop released panel triggers reload
- **WHEN** 栈顶面板为已释放状态（Panel == null），执行 Pop
- **THEN** UIManager 通过 PanelId 从注册表重新创建面板实例，调用 OnOpen 恢复面板，然后立即调用 OnClose，从栈中移除

#### Scenario: Pop released panel with saved state
- **WHEN** 栈顶面板为已释放状态且之前保存了状态（OnSaveState 返回值非 null）
- **THEN** UIManager 重新创建面板实例后先调用 OnRestoreState(savedState)，再执行 OnClose

#### Scenario: Pop last panel in layer
- **WHEN** 层级栈中仅剩一个面板，执行 Pop
- **THEN** 面板 OnClose 被调用，栈变为空，无 OnResume 触发

### Requirement: Peek operation with lazy reload
UIManager 的 `GetTopPanel()` 或类似 Peek 操作 SHALL 返回栈顶面板。若栈顶面板已释放（Panel == null），则先通过注册表重新加载后再返回。

#### Scenario: Peek active panel
- **WHEN** 栈顶面板为活跃状态
- **THEN** 直接返回该面板实例

#### Scenario: Peek released panel triggers reload
- **WHEN** 栈顶面板为已释放状态
- **THEN** UIManager 重新创建面板实例，若存在 saved state 则调用 OnRestoreState，返回重新加载后的面板

### Requirement: Remove panel from arbitrary position
UIManager SHALL 支持从栈中任意位置移除指定面板（非仅栈顶）。移除后，被移除面板上方的面板保持不动，被移除面板下方紧邻的面板收到 OnResume（若存在）。

#### Scenario: Remove middle panel
- **WHEN** 栈为 [A, B, C]（A 栈底，C 栈顶），移除 B
- **THEN** B 的 OnClose 被调用，栈变为 [A, C]，A 不收到 OnResume（A 不在 B 上方）

#### Scenario: Remove panel below current top
- **WHEN** 栈为 [A, B, C]，移除 A
- **THEN** A 的 OnClose 被调用，栈变为 [B, C]，B 不收到特殊回调

### Requirement: Capacity-based resource release
当层级栈中面板数量超过 MaxCapacity 时，UIManager SHALL 从栈底开始释放超出的面板。释放流程：调用面板的 `OnSaveState()` 保存状态 → 调用 `OnDestroy()` 释放资源 → 将 StackEntry 的 Panel 置为 null → 保留 StackEntry 在栈中。

#### Scenario: Push exceeds capacity
- **WHEN** Normal 层 MaxCapacity 为 3，当前栈为 [A, B, C]，新面板 D 入栈
- **THEN** 栈底面板 A 被释放（A.OnSaveState → A.OnDestroy → A.Panel = null），栈变为 [A(释放), B, C, D]

#### Scenario: Capacity overflow releases multiple panels
- **WHEN** MaxCapacity 为 2，当前栈为 [A, B]，一次性打开面板 C 和 D
- **THEN** A 和 B 依次被释放（按 FIFO 顺序），栈变为 [A(释放), B(释放), C, D]

#### Scenario: Released panel remains in stack
- **WHEN** 面板被容量溢出释放
- **THEN** StackEntry 仍保留在 List 中，PanelId 不变，仅 Panel 字段为 null，栈结构完整

### Requirement: Global stack top tracking
UIManager SHALL 维护全局栈顶引用，用于跨层级入栈时的层级比较。全局栈顶定义为所有层级中最后被 Push 的面板。

#### Scenario: Track global top across layers
- **WHEN** 先压入 Normal 层面板 A，再压入 Popup 层面板 B
- **THEN** 全局栈顶为 Popup 层的 B

#### Scenario: Global top updates on Pop
- **WHEN** 全局栈顶面板 B 被 Pop
- **THEN** 全局栈顶更新为之前次栈顶面板（可能是同层或不同层的面板）

### Requirement: Stack query operations
UIManager SHALL 提供以下栈查询方法：
- `GetLayerPanels(int layerId)` — 返回指定层级的所有活跃面板（Panel != null）
- `GetTopPanel(int layerId)` — 返回指定层级的栈顶面板（懒加载）
- `GetStackDepth(int layerId)` — 返回指定层级栈中的条目数量（含已释放）
- `IsLayerEmpty(int layerId)` — 判断指定层级栈是否为空

#### Scenario: Get active panels excludes released
- **WHEN** Normal 层栈为 [A(释放), B, C]，调用 `GetLayerPanels(Normal)`
- **THEN** 返回 [B, C]，不包含已释放的 A

#### Scenario: Get stack depth includes released entries
- **WHEN** Normal 层栈为 [A(释放), B, C]，调用 `GetStackDepth(Normal)`
- **THEN** 返回 3（含已释放的条目）
