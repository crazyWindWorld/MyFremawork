# MEMORY.md - 项目长期记忆

## 项目：GameServer（游戏服务器）
- 路径：`E:\UnityProject\MyFremawork\Server\C#Server\TestTcpServer\TestTcpServer`
- 框架：.NET 8，项目文件：`GameServer.csproj`
- 原项目：TestTcpServer（.NET Framework 4.7.2，已保留 Proto/ 目录）

## 架构约定
- **协议帧格式**：`[4B totalLen BE][4B msgId BE][body protobuf]`，totalLen = msgId(4) + body.Length
- **命令 ID**：见 `ProtoCmds.cs`（自动生成 + 手动追加）。原自动生成：PING=4000/PONG=4001, LoginReq=10003/LoginRsp=10004/LogoutPush=10005。GameServer 追加别名：PingReq=PING(4000), PongRsp=PONG(4001), LogoutReq=10008, LogoutRsp=10009, KickPush=10012
- **Proto 字段**：LoginReq 使用 `.Account`（非 Username）和 `.Pwd`（非 Password），命名空间 `LoginPB`
- **Handler 注册**：`[MsgHandler(ProtoCmds.XXX)]` Attribute 标记类，继承 `HandlerBase`，DI Scope 级别
- **RequireLogin**：`[MsgHandler]` 有 `RequireLogin` 属性（默认 true），LoginReq/PingReq 设为 false
- **数据层**：当前使用 `InMemoryRepository<T>`，接口为 `IRepository<T>` + `IUnitOfWork`，切换 DB 只需实现接口并在 Program.cs 替换注册

## 登录模块
- 完整流程：参数校验 → 账号查询 → 自动注册(Account类型) → 封禁检测 → 密码校验 → 重复登录踢旧连接 → 注册在线缓存 → 返回 LoginRsp
- PlayerEntity 字段：Id, Username, PasswordHash, Nickname, CreatedAt, LastLoginAt, IsBanned, BanExpireTime
- SHA256 密码哈希（生产建议 Argon2/BCrypt）
- 踢人：PlayerStateCache.TryKickAsync() → 发 KickPush(10012) → session.Close()

## 心跳模块
- PingHandler 收到 Ping 时调用 session.MarkActive()
- HeartbeatService 每 30s 遍历 GatewayServer.GetAllSessions()，超时 90s 未活跃则踢人
- GameSession.MarkActive() 使用 Volatile.Read/Write 保证线程安全
- 断连时 GatewayServer finally 块调用 _playerCache.Unregister()

## 配置项（appsettings.json）
- `Gateway:Port` = 9000
- `Gateway:MaxFrameSize` = 10MB
- `Heartbeat:CheckIntervalSeconds` = 30
- `Heartbeat:TimeoutSeconds` = 90

## 待办/扩展点
- [ ] 接入真实 DB（MySQL/SQLite/MongoDB），实现 `IRepository<T>`
- [ ] Redis 接入（Session 共享、排行榜、分布式锁）
- [ ] 密码哈希从 SHA256 升级到 Argon2/BCrypt
- [ ] 多节点分布式支持（消息队列中间件如 RabbitMQ/NATS）
- [ ] 登录限流（防暴力破解）
- [ ] 日志审计（登录/登出/踢人事件持久化）
