# 游戏服务器架构设计 - 总览

## 完成内容

原项目（.NET Framework 4.7.2 的简单转发服务器）已重构为 **.NET 8 生产级游戏服务器框架**，全部文件可直接用新 `GameServer.csproj` 编译。

---

## 目录结构

```
GameServer.csproj              ← .NET 8 项目文件（替换原 .csproj）
ProtoCmds.cs                   ← 协议命令 ID 常量
appsettings.json               ← 服务器配置（端口、帧大小、心跳）

Core/
  Protocol/
    Packet.cs                  ← 网络帧（msgId + body）
    FrameCodec.cs              ← 帧编解码（ArrayPool 零拷贝，Big-Endian）
  Net/
    GameSession.cs             ← 单连接会话（双 Channel 收发解耦）
    GatewayServer.cs           ← TCP 监听 + Session 池 + 广播
  Routing/
    MsgHandlerAttribute.cs     ← [MsgHandler(msgId)] 标记 Attribute
    HandlerBase.cs             ← Handler 基类
    MessageRouter.cs           ← 反射扫描路由，每消息独立 DI Scope
    HandlerServiceExtensions.cs← 一键注册所有 Handler
  Context/
    PlayerStateCache.cs        ← 在线玩家内存缓存

Data/
  Abstractions/
    IEntity.cs                 ← 实体标记接口
    IRepository<T>.cs          ← 泛型仓储接口（CRUD + 批量）
    IUnitOfWork.cs             ← 工作单元接口（事务抽象）
  InMemory/
    InMemoryRepository<T>.cs   ← 内存实现（开发/测试，线程安全）
    InMemoryUnitOfWork.cs      ← InMemory UoW
    InMemoryDataExtensions.cs  ← DI 注册扩展

Handlers/
  LoginHandler.cs              ← 登录（LoginReq→LoginRsp，接 IRepository）
  PingHandler.cs               ← 心跳（PingReq→PongRsp）

Services/
  GatewayHostedService.cs      ← IHostedService 包装 GatewayServer
  HeartbeatService.cs          ← BackgroundService 定期心跳检测

Program.cs                     ← 启动入口（Generic Host + DI + Serilog）
```

---

## 核心设计亮点

| 特性 | 说明 |
|------|------|
| **并发模型** | 每个 TCP 连接对应一个 `GameSession`，内部两个 `Channel<Packet>` 解耦收发，收发互不阻塞 |
| **消息路由** | 反射扫描 `[MsgHandler]` 自动注册，每条消息创建独立 DI Scope，Handler 内可安全使用 Scoped 服务（如 DbContext） |
| **零拷贝协议** | `FrameCodec` 使用 `ArrayPool<byte>` + `BinaryPrimitives`，避免频繁 GC |
| **DB 抽象** | `IRepository<T>` + `IUnitOfWork` 抽象，当前 InMemory 实现，切换 MySQL/SQLite/MongoDB 只需实现接口 |
| **配置驱动** | 端口、帧大小、心跳间隔全部走 `appsettings.json`，支持环境覆盖（Development/Production） |
| **依赖注入** | 全面使用 `Microsoft.Extensions.DependencyInjection`，组件解耦，易于单元测试 |
| **日志** | Serilog 接入，控制台带颜色格式，可扩展 File Sink / Seq |

---

## 编译运行

```bash
# 进入项目目录（需已安装 .NET 8 SDK）
cd TestTcpServer

# 恢复包（首次）
dotnet restore GameServer.csproj

# 运行
dotnet run --project GameServer.csproj
```

客户端连接端口 **9000**，帧格式：`[4B totalLen BE][4B msgId BE][protobuf body]`。

---

## 后续扩展路线

1. **接入 MySQL**：实现 `MySqlRepository<T>`（用 Dapper 或 EF Core），替换 `AddInMemoryData` 调用
2. **接入 Redis**：缓存玩家 Session Token、排行榜、分布式锁
3. **新增 Handler**：创建继承 `HandlerBase` 的类，加 `[MsgHandler(ProtoCmds.xxx)]`，自动路由
4. **多节点扩展**：GatewayServer 前加 Nginx TCP 负载均衡，Session 共享走 Redis
5. **心跳完善**：在 `GameSession` 中记录 `LastPingAt`，`HeartbeatService` 超时踢人
