## 1. 基础结构搭建

- [x] 1.1 创建 `Assets/Scripts/Manager/UIManager/` 目录结构
- [x] 1.2 创建 `UIPanelBase` 抽象基类，包含 PanelId、Layer、IsOpen 状态字段及 OnOpen/OnClose/OnPause/OnResume/OnDestroy 虚方法
- [x] 1.3 创建 `UILayer` 枚举，定义 Background(0)、Normal(100)、Popup(200)、Top(300) 四个默认层级
- [x] 1.4 创建 `StackEntry` 结构，持有 int PanelId 和 UIPanelBase Panel（可为 null）

## 2. UIManager 核心实现

- [x] 2.1 创建 `UIManager` 类，继承 `Singleton<UIManager>`，实现 Init/Update(float dt)/Dispose 生命周期
- [x] 2.2 实现 `RegisterLayer(int layerId, string name)` 自定义层级注册方法
- [x] 2.3 实现 `SetLayerCapacity(int layerId, int capacity)` 动态容量配置方法
- [x] 2.4 实现 `Register<T>(int panelId)` 类型注册方法
- [x] 2.5 实现 `Register(int panelId, UIPanelBase panel)` 实例注册方法

## 3. 栈操作实现

- [x] 3.1 实现 Push 语义：入栈、跨层级比较、清栈逻辑
- [x] 3.2 实现 Pop 语义：栈顶移除、已释放面板懒加载重入、OnResume 回调
- [x] 3.3 实现 Peek 语义：返回栈顶面板，已释放时先重加载
- [x] 3.4 实现 Remove(panel)：从栈中任意位置移除面板
- [x] 3.5 实现全局栈顶追踪（globalTop），在 Push/Pop 时维护

## 4. 容量管理与资源释放

- [x] 4.1 实现 Push 后容量检查：超出 MaxCapacity 时从栈底释放面板
- [x] 4.2 实现释放流程：OnSaveState → OnDestroy → Panel = null，保留 StackEntry
- [x] 4.3 实现 `OnRestoreState` 恢复机制：重新加载面板后恢复之前保存的状态

## 5. 栈查询 API

- [x] 5.1 实现 `GetLayerPanels(int layerId)`：返回层级内活跃面板（排除已释放）
- [x] 5.2 实现 `GetTopPanel(int layerId)`：返回指定层级栈顶（懒加载）
- [x] 5.3 实现 `GetStackDepth(int layerId)`：返回栈条目总数（含已释放）
- [x] 5.4 实现 `IsLayerEmpty(int layerId)`：判断层级栈是否为空

## 6. 跨层级清栈实现

- [x] 6.1 实现 Push 时全局栈顶层级比较逻辑
- [x] 6.2 实现跨层级清栈：栈顶层级 > 新面板层级时，释放整个栈所有面板
- [x] 6.3 处理已释放面板的 OnClose 回调（跨层级清栈命中已释放面板时）

## 7. UIManager Dispose 清理

- [x] 7.1 实现 Dispose 方法：遍历所有层级栈，对活跃面板调用 OnDestroy，清空所有栈
- [x] 7.2 清理注册表和层级映射数据

## 8. 测试与验证

- [x] 8.1 编写测试：验证同层 Push/Pop/Peek 基本流程
- [x] 8.2 编写测试：验证跨层级清栈规则
- [x] 8.3 编写测试：验证容量溢出释放与懒加载恢复
- [x] 8.4 编写测试：验证已释放面板的 OnSaveState/OnRestoreState
