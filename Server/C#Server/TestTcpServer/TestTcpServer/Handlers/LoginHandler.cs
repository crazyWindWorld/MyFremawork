using GameServer.Core.Context;
using GameServer.Core.Net;
using GameServer.Core.Protocol;
using GameServer.Core.Routing;
using GameServer.Data.Abstractions;
using Google.Protobuf;
using LoginPB;
using Microsoft.Extensions.Logging;

namespace GameServer.Handlers;

/// <summary>
/// 登录请求处理器。
/// 完整流程：
///   1. 解析 LoginReq
///   2. 参数校验（账号为空）
///   3. 查询账号
///   4. 账号不存在 → 自动注册（仅 Account 类型）
///   5. 封禁检测
///   6. 密码校验
///   7. 重复登录 → 踢旧连接
///   8. 当前 Session 已登录其他账号 → 先注销
///   9. 标记登录 + 注册在线缓存
///  10. 返回 LoginRsp
/// </summary>
[MsgHandler(ProtoCmds.LoginReq, RequireLogin = false)]
public sealed class LoginHandler : HandlerBase
{
    private readonly IRepository<PlayerEntity> _playerRepo;
    private readonly PlayerStateCache _playerCache;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        IRepository<PlayerEntity> playerRepo,
        PlayerStateCache playerCache,
        ILogger<LoginHandler> logger)
    {
        _playerRepo  = playerRepo;
        _playerCache = playerCache;
        _logger      = logger;
    }

    public override async ValueTask HandleAsync(
        GameSession session,
        Packet packet,
        CancellationToken ct)
    {
        // ── 1. 解析请求 ─────────────────────────────────────────────
        LoginReq req;
        try
        {
            req = LoginReq.Parser.ParseFrom(packet.Body.Span);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[Login] 解析失败: {Msg}", ex.Message);
            await session.SendAsync(BuildRsp(LoginResult.LoginFailed), ct);
            return;
        }

        // ── 2. 参数校验 ─────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(req.Account))
        {
            _logger.LogWarning("[Login] 账号为空");
            await session.SendAsync(BuildRsp(LoginResult.LoginFailed), ct);
            return;
        }

        session.MarkActive();

        _logger.LogInformation("[Login] Account={Account}, Type={Type}",
            req.Account, req.Type);

        // ── 3. 查询账号 ─────────────────────────────────────────────
        var player = await _playerRepo.FindByPredicateAsync(
            p => p.Username == req.Account, ct);

        // ── 4. 账号不存在 → 自动注册 ────────────────────────────────
        if (player is null)
        {
            if (req.Type == LoginType.Account)
            {
                player = await RegisterNewAccountAsync(req, ct);
                _logger.LogInformation("[Login] 自动注册账号 Account={Account}, Id={Id}",
                    req.Account, player.Id);
            }
            else
            {
                _logger.LogWarning("[Login] 账号不存在 Account={Account}", req.Account);
                await session.SendAsync(BuildRsp(LoginResult.LoginFailed), ct);
                return;
            }
        }

        // ── 5. 封禁检测 ─────────────────────────────────────────────
        if (player.IsBanned)
        {
            bool stillBanned = player.BanExpireTime == null
                || player.BanExpireTime > DateTime.UtcNow;

            if (stillBanned)
            {
                long limitTs = player.BanExpireTime.HasValue
                    ? new DateTimeOffset(player.BanExpireTime.Value, TimeSpan.Zero)
                        .ToUnixTimeSeconds()
                    : 0;

                _logger.LogWarning("[Login] 账号被封禁 Account={Account}, Until={Until}",
                    req.Account, player.BanExpireTime);

                var banRsp = new LoginRsp
                {
                    Result    = LoginResult.LoginLimit,
                    RoleId    = player.Id,
                    LimitTime = limitTs,
                    Type      = LoginType.Account
                };
                await session.SendAsync(
                    new Packet(ProtoCmds.LoginRsp, banRsp.ToByteArray()), ct);
                return;
            }

            // 封禁已过期，自动解封
            player.IsBanned       = false;
            player.BanExpireTime  = null;
            await _playerRepo.UpdateAsync(player, ct);
            _logger.LogInformation("[Login] 封禁已过期，自动解封 Account={Account}", req.Account);
        }

        // ── 6. 密码校验 ─────────────────────────────────────────────
        if (player.PasswordHash != HashPassword(req.Pwd))
        {
            _logger.LogWarning("[Login] 密码错误 Account={Account}", req.Account);
            await session.SendAsync(BuildRsp(LoginResult.LoginFailed, player.Id), ct);
            return;
        }

        // ── 7. 重复登录踢旧连接 ─────────────────────────────────────
        if (_playerCache.IsOnline(player.Id))
        {
            _logger.LogWarning("[Login] 重复登录，踢旧连接 PlayerId={Id}", player.Id);
            await _playerCache.TryKickAsync(player.Id, ct);
        }

        // ── 8. 当前 Session 已登录其他账号，先注销 ──────────────────
        if (session.IsLoggedIn && session.PlayerId != player.Id)
        {
            _playerCache.Unregister(session.PlayerId);
        }

        // ── 9. 标记登录成功 + 注册在线缓存 ──────────────────────────
        session.MarkLogin(player.Id);
        _playerCache.Register(session);

        // ── 10. 返回成功响应 ─────────────────────────────────────────
        var rsp = new LoginRsp
        {
            Result = LoginResult.LoginSuccess,
            RoleId = player.Id,
            Type   = LoginType.Account,
        };
        await session.SendAsync(new Packet(ProtoCmds.LoginRsp, rsp.ToByteArray()), ct);

        _logger.LogInformation(
            "[Login] 登录成功 Account={Account}, PlayerId={Id}, Session={Sid}",
            req.Account, player.Id, session.SessionId);
    }

    // ─── 私有辅助方法 ─────────────────────────────────────────────

    private async Task<PlayerEntity> RegisterNewAccountAsync(
        LoginReq req, CancellationToken ct)
    {
        var player = new PlayerEntity
        {
            Username     = req.Account.Trim(),
            PasswordHash = HashPassword(req.Pwd),
            Nickname     = $"Player_{DateTime.UtcNow:MMddHHmmss}",
            CreatedAt    = DateTime.UtcNow,
            LastLoginAt  = DateTime.UtcNow
        };
        await _playerRepo.AddAsync(player, ct);
        return player;
    }

    private static Packet BuildRsp(LoginResult result, long roleId = 0)
    {
        var rsp = new LoginRsp
        {
            Result = result,
            RoleId = roleId,
            Type   = LoginType.Account
        };
        return new Packet(ProtoCmds.LoginRsp, rsp.ToByteArray());
    }

    /// <summary>密码哈希 SHA256（生产建议升级为 Argon2/BCrypt）</summary>
    internal static string HashPassword(string raw)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

// ─── 玩家实体 ─────────────────────────────────────────────────────────────

/// <summary>玩家数据库实体（与 IRepository 配套）</summary>
public sealed class PlayerEntity : IEntity
{
    public long      Id           { get; set; }
    public string    Username     { get; set; } = string.Empty;
    public string    PasswordHash { get; set; } = string.Empty;
    public string    Nickname     { get; set; } = string.Empty;
    public DateTime  CreatedAt    { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt  { get; set; }

    /// <summary>是否被封禁</summary>
    public bool IsBanned { get; set; }

    /// <summary>封禁到期时间（null = 永久封禁）</summary>
    public DateTime? BanExpireTime { get; set; }
}
