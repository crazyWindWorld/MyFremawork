## ADDED Requirements

### Requirement: UILayer enum defines default layers
系统 SHALL 提供 `UILayer` 枚举，包含默认层级：Background（0）、Normal（100）、Popup（200）、Top（300）。枚举值为 int 类型，允许用户通过数值大小控制层级优先级。

#### Scenario: Default layers are available
- **WHEN** 开发者引用 UILayer 枚举
- **THEN** 可使用 Background、Normal、Popup、Top 四个默认层级，且 Background < Normal < Popup < Top

#### Scenario: Enum values are ordered by priority
- **WHEN** 比较两个 UILayer 枚举值
- **THEN** 数值较小的层级优先级较低，数值较大的优先级较高

### Requirement: Custom layer registration via int
UIManager SHALL 支持通过 `RegisterLayer(int layerId, string name)` 注册自定义层级。自定义层级使用 int 值标识，可插入到默认层级之间的任意位置。

#### Scenario: Register a custom layer between Normal and Popup
- **WHEN** 调用 `RegisterLayer(150, "Dialog")`
- **THEN** Dialog 层级的优先级高于 Normal（100）且低于 Popup（200），UIManager 内部为其创建独立的 LayerStack

#### Scenario: Register layer with duplicate id
- **WHEN** 调用 `RegisterLayer` 传入已存在的 layerId
- **THEN** 系统 SHALL 抛出异常或忽略注册，并输出警告日志

### Requirement: Layer stack isolation
每个层级 SHALL 拥有独立的 `List<StackEntry>` 栈实例，不同层级的面板互不干扰。UIManager 内部通过 `Dictionary<int, LayerStack>` 映射层级 ID 到对应的栈。

#### Scenario: Panels in different layers are independent
- **WHEN** 在 Normal 层级压入面板 A，在 Popup 层级压入面板 B
- **THEN** Normal 层栈包含 [A]，Popup 层栈包含 [B]，两栈互不影响

#### Scenario: Get panels by layer
- **WHEN** 调用 `GetLayerPanels(int layerId)`
- **THEN** 返回该层级栈中所有活跃面板的列表（仅 Panel != null 的 StackEntry）

### Requirement: Dynamic layer capacity configuration
UIManager SHALL 支持通过 `SetLayerCapacity(int layerId, int capacity)` 动态设置每个层级栈的最大容量。默认容量为 -1（不限制）。

#### Scenario: Set capacity for a layer
- **WHEN** 调用 `SetLayerCapacity(UILayer.Normal, 3)`
- **THEN** Normal 层级栈的最大容量被设置为 3，后续入栈超过 3 个面板时触发栈底面板资源释放

#### Scenario: Remove capacity limit
- **WHEN** 调用 `SetLayerCapacity(UILayer.Normal, -1)`
- **THEN** Normal 层级栈不再有容量限制，面板永不因容量溢出而自动释放

#### Scenario: Adjust capacity dynamically at runtime
- **WHEN** 在运行时调用 `SetLayerCapacity` 修改已存在面板的层级容量
- **THEN** 新容量立即生效，若当前栈已超过新容量则立即释放栈底多余面板
