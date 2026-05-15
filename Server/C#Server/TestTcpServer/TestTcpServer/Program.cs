using GameServer.Core.Context;
using GameServer.Core.Net;
using GameServer.Core.Routing;
using GameServer.Data.InMemory;
using GameServer.Handlers;
using GameServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// ───────── Serilog ─────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        // ── 网络 ──
        services.AddSingleton<PlayerStateCache>();
        services.AddSingleton<GatewayServer>();

        // ── 数据层（内存实现，生产替换为 EF/MongoDB） ──
        services.AddInMemoryData(typeof(PlayerEntity));

        // ── 消息 Handler（扫描当前程序集） ──
        services.AddMsgHandlers(typeof(Program).Assembly);

        // ── 路由 ──
        services.AddSingleton<MessageRouter>();

        // ── 后台服务 ──
        services.AddHostedService<GatewayHostedService>();
        services.AddHostedService<HeartbeatService>();
    });

var app = builder.Build();

// ── 种子数据：写入测试账号 ──
await SeedDataAsync(app.Services);

await app.RunAsync();

// ─────────────────────────────────────────────
static async Task SeedDataAsync(IServiceProvider rootSp)
{
    await using var scope = rootSp.CreateAsyncScope();
    var uow = scope.ServiceProvider.GetRequiredService<GameServer.Data.Abstractions.IUnitOfWork>();
    var repo = uow.Repository<PlayerEntity>();

    // 正常测试账号
    if (await repo.FindByPredicateAsync(p => p.Username == "testuser") is null)
    {
        await repo.AddAsync(new PlayerEntity
        {
            Username = "testuser",
            PasswordHash = GameServer.Handlers.LoginHandler.HashPassword("test123"),
            Nickname = "TestUser",
            CreatedAt = DateTime.UtcNow
        });
    }

    // 封禁账号（测试用）
    if (await repo.FindByPredicateAsync(p => p.Username == "banneduser") is null)
    {
        await repo.AddAsync(new PlayerEntity
        {
            Username = "banneduser",
            PasswordHash = GameServer.Handlers.LoginHandler.HashPassword("ban123"),
            Nickname = "BannedUser",
            IsBanned = true,
            BanExpireTime = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
    }

    await uow.CommitAsync();
}
