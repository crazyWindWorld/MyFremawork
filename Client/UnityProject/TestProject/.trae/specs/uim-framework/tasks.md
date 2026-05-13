# Tasks

- [x] Task 1: 创建 UILayerConfig 层级配置类（ScriptableObject 单例）
  - [x] SubTask 1.1: 继承 ScriptableObject，定义层级列表（层级名称、基础 Z 值、层级 ID）
  - [x] SubTask 1.2: 实现全局单例 Instance 属性
  - [x] SubTask 1.3: 实现层级比较接口

- [x] Task 2: 创建 UIWindow 窗口基类
  - [x] SubTask 2.1: 定义窗口基础属性（窗口 ID、层级、名称）
  - [x] SubTask 2.2: 实现生命周期方法（OnShow、OnHide、OnDestroy）
  - [x] SubTask 2.3: 实现事件注册注销接口（RegisterEvents、UnregisterEvents）
  - [x] SubTask 2.4: 实现资源引用（GameObject 或视图对象）

- [x] Task 3: 创建 UIStack 栈管理器
  - [x] SubTask 3.1: 使用 List 实现栈结构
  - [x] SubTask 3.2: 实现 Push 入栈方法
  - [x] SubTask 3.3: 实现 Pop 出栈方法
  - [x] SubTask 3.4: 实现 Clear 清空方法
  - [x] SubTask 3.5: 实现层级比较逻辑（低于栈顶时清空栈）
  - [x] SubTask 3.6: 实现重复窗口检测和出栈到指定位置逻辑

- [x] Task 4: 创建 UIResourceManager 资源管理器
  - [x] SubTask 4.1: 实现资源加载接口
  - [x] SubTask 4.2: 实现资源释放接口
  - [x] SubTask 4.3: 实现资源缓存机制
  - [x] SubTask 4.4: 实现映射关系管理

- [x] Task 5: 创建 UIManager 核心管理器
  - [x] SubTask 5.1: 实现单例模式
  - [x] SubTask 5.2: 实现初始化方法（创建 Canvas、初始化子系统）
  - [x] SubTask 5.3: 实现 OpenWindow 打开界面方法
  - [x] SubTask 5.4: 实现 CloseWindow 关闭界面方法
  - [x] SubTask 5.5: 实现栈数量动态调整逻辑
  - [x] SubTask 5.6: 实现自动事件注册注销调用

- [x] Task 6: 创建 UIEditorDebugWindow 编辑器调试窗口
  - [x] SubTask 6.1: 继承 EditorWindow，实现层级配置展示
  - [x] SubTask 6.2: 实现栈数据实时显示
  - [x] SubTask 6.3: 添加刷新和可视化界面

- [x] Task 7: 创建 UIAutoBindTool 编辑器自动绑定工具
  - [x] SubTask 7.1: 创建 UIAutoBindData 绑定配置数据类（序列化存储）
  - [x] SubTask 7.2: 实现 EditorWindow 手动绑定界面
  - [x] SubTask 7.3: 实现字段扫描和自动匹配绑定
  - [x] SubTask 7.4: 实现运行时自动绑定逻辑
  - [x] SubTask 7.5: 实现自定义名称规则配置（支持添加/修改/删除前缀映射）
  - [x] SubTask 7.6: 实现按名称前缀自动推断组件类型并绑定
  - [x] SubTask 7.7: 扩展 Hierarchy 右键菜单，提供 "UI Bind" 选项
  - [x] SubTask 7.8: 实现组件类型多选和自定义字段名输入界面
