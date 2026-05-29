---
id: kd_2a48ed04-fb95-47f9-8054-126debbed956
type: design
path: startup-update-hotfix-pipeline.md
title: startup-update-hotfix-pipeline
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779937301972
updatedAt: 1779937948017
---

# startup-update-hotfix-pipeline

## Summary
启动更新与热更管线设计：本地与远端配置均使用 JSON；本地 JSON 通过编辑器扩展维护，并提供远端版本 URL、App SemVer、YooAsset Main、HybridCLR DLL/AOT DLL、TableKit 配置路径等启动参数。

## Content
# 启动更新与热更管线设计

## 目标
构建一套从游戏启动到进入业务逻辑前的基础管线：
1. 进入游戏启动器。
2. 读取本地内置 JSON 启动配置。
3. 通过本地 JSON 中配置的远端 URL 拉取远端版本 JSON。
4. 将远端 JSON 与本地版本信息做 SemVer 对比。
5. 大版本不兼容时提示用户前往商店/官网重新下载整包。
6. 小版本有差异时通过 YooAsset 下载 `Main` 资源包差异资源。
7. 加载 HybridCLR 热更新代码 DLL 与 AOT 元数据补充 DLL。
8. 加载 Luban/TableKit 配置数据。
9. 进入首个业务场景或热更入口。

目标平台：移动端、PC、Web 平台，且三端都必须支持 HybridCLR 热更。

## 已确认约束
- 本地配置使用 JSON 文件，不使用 ScriptableObject 作为启动配置源。
- 远端版本信息也使用 JSON 文件，通过本地 JSON 中的 URL 拉取。
- 本地需要提供编辑器扩展，用于可视化修改本地 JSON 配置文件。
- App 版本号规则使用 SemVer，例如 `1.2.3`。
- YooAsset 资源包名固定为 `Main`。
- HotUpdate DLL、AOT 元数据 DLL、配置数据路径都由 JSON 定义。
- 移动端、PC、Web 都必须支持 HybridCLR 热更。

## 现有项目基础
- 已接入 YooAsset：`Assets/Scripts/Fuel/Manager/AssetManager/AssetsManager.cs` 中已有编辑器模拟初始化与资源加载封装。
- 已接入 Luban/TableKit：`Assets/HotUpdate/Configs/TableKit.cs` 支持同步/异步配置初始化，配置数据当前位于 `Assets/AssetsPackage/Main/Configs/`。
- 已有 `Assets/HotUpdate/Configs/Test.TableKit.asmdef`，说明项目已有热更侧程序集拆分雏形。
- 当前未发现 HybridCLR 加载入口、启动流程入口、版本检查协议封装。

## 总体架构
新增一个独立启动模块，常驻主工程程序集，负责启动前置流程；热更业务代码和配置入口放入 HotUpdate 程序集。

推荐模块：
- `Launcher`：启动 MonoBehaviour，只存在于首场景，驱动启动状态机。
- `GameUpdatePipeline`：启动更新管线编排器。
- `LocalStartupConfig`：本地 JSON 配置数据，定义当前版本、远端版本 URL、资源包名、DLL 路径、配置路径、热更入口类型等。
- `RemoteVersionInfo`：远端 JSON 版本数据，定义最低兼容版本、最新版本、资源版本、CDN ��址、商店地址等。
- `IStartupConfigProvider`：读取本地 JSON 配置。
- `IVersionService`：根据本地配置请求远端版本 JSON。
- `IAppUpdateHandler`：处理大版本更新提示与跳转。
- `IResourceUpdateService`：基于 YooAsset 完成资源包初始化、版本检查、清单更新、差异下载。
- `IHybridCLRLoader`：加载 AOT 元数据 DLL 与热更 DLL。
- `IConfigLoader`：初始化 Luban/TableKit 配置。
- `IHotUpdateEntry`：热更业务入口约定。
- `StartupConfigEditorWindow`：编辑器窗口，用于修改本地 JSON 配置。

## 本地 JSON 配置
本地 JSON 必须随包内置，启动阶段不能依赖热更资源。推荐放在 `Resources`，方便首包启动时读取。

建议路径：`Assets/Resources/StartupConfig.json`

示例：
```json
{
  "appVersion": "1.0.0",
  "versionUrl": "https://cdn.example.com/version.json",
  "packageName": "Main",
  "defaultHostUrl": "https://cdn.example.com/game/Main",
  "fallbackHostUrl": "https://backup.example.com/game/Main",
  "hotUpdateDllPath": "Assets/AssetsPackage/Main/HotUpdate/HotUpdate.dll.bytes",
  "aotMetadataDllPaths": [
    "Assets/AssetsPackage/Main/HotUpdate/mscorlib.dll.bytes",
    "Assets/AssetsPackage/Main/HotUpdate/System.dll.bytes",
    "Assets/AssetsPackage/Main/HotUpdate/System.Core.dll.bytes"
  ],
  "configPathPattern": "Assets/AssetsPackage/Main/Configs/{0}",
  "hotUpdateEntryType": "HotUpdate.GameEntry.HotUpdateEntry",
  "hotUpdateEntryMethod": "StartAsync"
}
```

说明：
- `appVersion`：本地 App 版本，按 SemVer 对比。
- `versionUrl`：远端版本 JSON 地址。
- `packageName`：固定为 `Main`。
- `defaultHostUrl` / `fallbackHostUrl`：默认 YooAsset 远端资源地址，远端 JSON 可覆盖。
- `hotUpdateDllPath`：热更主 DLL 的 YooAsset 地址。
- `aotMetadataDllPaths`：HybridCLR AOT 元数据补充 DLL 的 YooAsset 地址列表。
- `configPathPattern`：TableKit 配置加载路径模板。
- `hotUpdateEntryType` / `hotUpdateEntryMethod`：反射调用热更入口。

## 远端 JSON 版本协议
示例：
```json
{
  "minAppVersion": "1.0.0",
  "latestAppVersion": "1.1.0",
  "resourceVersion": "2025-01-01-001",
  "resourceHostUrl": "https://cdn.example.com/game/Main",
  "fallbackResourceHostUrl": "https://backup.example.com/game/Main",
  "storeUrl": "https://example.com/download",
  "notice": "发现新版本"
}
```

说明：
- `minAppVersion`：最低兼容 App 版本，小于此版本必须整包更新。
- `latestAppVersion`：最新 App 版本，大于本地版本时可做弱提示。
- `resourceVersion`：远端资源版本，可用于日志、展示或与 YooAsset 版本流程联动。
- `resourceHostUrl` / `fallbackResourceHostUrl`：远端资源地址，优先覆盖本地默认地址。
- `storeUrl`：强更跳转地址。
- `notice`：版本提示文本。

## 本地配置编辑器扩展
需要新增编辑器窗口，建议菜单：`Tools/Fuel/Startup Config`。

功能要求：
- 读取 `Assets/Resources/StartupConfig.json`。
- 文件不存在时可创建默认 JSON。
- 可编辑 App 版本、远端版本 URL、资源 Host、DLL 路径、AOT DLL 路径列表、配置路径模板、热更入口类型和方法。
- 保存时格式化 JSON 并刷新 AssetDatabase。
- 保存前校验必填字段：`appVersion`、`versionUrl`、`packageName`、`hotUpdateDllPath`、`hotUpdateEntryType`。
- App 版本字段按 SemVer 做格式校验。

## 启动流程
```text
App 启动
  -> Launcher.Awake/Start
  -> 读取本地 StartupConfig.json
  -> 通过 versionUrl 请求远端版本 JSON
  -> SemVer 比对本地 appVersion 与远端 minAppVersion/latestAppVersion
      -> 低于 minAppVersion：显示强更弹窗，跳转 storeUrl，终止流程
      -> 可兼容：继续
  -> 初始化 YooAsset Main Package
  -> 请求 YooAsset PackageVersion
  -> 下载并加载资源 Manifest
  -> 创建下载器并检查差异资源
      -> 无差异：继续
      -> 有差异：显示大小与进度，下载、校验、清理缓存
  -> 按 JSON 加载 HybridCLR AOT 元数据 DLL
  -> 按 JSON 加载 HotUpdate DLL
  -> 按 JSON 初始化 TableKit 配置
  -> 反射调用 HotUpdate 入口
  -> 加载首个业务场景
```

## 版本策略
### App 大版本
App 版本由本地 JSON 的 `appVersion` 表示，远端 JSON 返回 `minAppVersion` 与 `latestAppVersion`。
版本比较按 SemVer 处理。

处理规则：
- `appVersion < minAppVersion`：强制整包更新。
- `appVersion >= minAppVersion`：允许进入资源更新流程。
- `appVersion < latestAppVersion` 但仍兼容：可选弱提示。

### 资源小版本
资源版本交给 YooAsset Package Version 与 Manifest 处理。
远端 JSON 可提供资源 Host 覆盖本地默认 Host。

## 平台差异
### 移动端
- 支持强更跳转 App Store、Google Play、厂商商店或官网下载页。
- 资源差异包写入持久化目录。
- 下载前需要检查网络类型与磁盘空间。

### PC
- 可跳转官网/启动器更新页。
- 资源热更流程与移动端一致。
- 整包更新一般交给外部启动器或安装包。

### Web
- Web 平台必须支持 HybridCLR 热更。
- 需要先验证 Unity 2022.3、HybridCLR、YooAsset、WebGL 的组合限制，尤其是文件持久化、线程、反射、Assembly 加载、浏览器缓存与远端资源访问策略。
- Web 平台应提供独立实现分支，例如 `WebResourceUpdateService` / `WebHybridCLRLoader`，避免影响移动端和 PC。
- 如果 WebGL 环境存在不可规避限制，必须先通过技术验证闭环。

## 关键接口草案
```csharp
public enum StartupStep
{
    None,
    LoadLocalConfig,
    FetchRemoteVersion,
    CheckAppVersion,
    InitAssets,
    UpdateAssets,
    LoadAotMetadata,
    LoadHotUpdateDll,
    LoadConfigs,
    EnterGame,
    Failed
}

public enum AppUpdateDecision
{
    Continue,
    OptionalUpdate,
    ForceUpdate
}

public sealed class LocalStartupConfig
{
    public string AppVersion;
    public string VersionUrl;
    public string PackageName;
    public string DefaultHostUrl;
    public string FallbackHostUrl;
    public string HotUpdateDllPath;
    public string[] AotMetadataDllPaths;
    public string ConfigPathPattern;
    public string HotUpdateEntryType;
    public string HotUpdateEntryMethod;
}

public sealed class RemoteVersionInfo
{
    public string MinAppVersion;
    public string LatestAppVersion;
    public string ResourceVersion;
    public string ResourceHostUrl;
    public string FallbackResourceHostUrl;
    public string StoreUrl;
    public string Notice;
}

public sealed class ResourceDownloadInfo
{
    public int TotalCount;
    public long TotalBytes;
}

public interface IStartupConfigProvider
{
    LocalStartupConfig Load();
}

public interface IVersionService
{
    UniTask<RemoteVersionInfo> FetchVersionAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
}

public interface IAppVersionChecker
{
    AppUpdateDecision Check(LocalStartupConfig localConfig, RemoteVersionInfo remoteInfo);
}

public interface IAppUpdateHandler
{
    UniTask HandleForceUpdateAsync(RemoteVersionInfo remoteInfo, CancellationToken cancellationToken);
    UniTask<bool> HandleOptionalUpdateAsync(RemoteVersionInfo remoteInfo, CancellationToken cancellationToken);
}

public interface IResourceUpdateService
{
    UniTask InitializeAsync(LocalStartupConfig localConfig, RemoteVersionInfo remoteInfo, CancellationToken cancellationToken);
    UniTask<ResourceDownloadInfo> CheckUpdateAsync(CancellationToken cancellationToken);
    UniTask DownloadAsync(IProgress<float> progress, CancellationToken cancellationToken);
    UniTask ClearUnusedCacheAsync(CancellationToken cancellationToken);
}

public interface IHybridCLRLoader
{
    UniTask LoadAotMetadataAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
    UniTask<Assembly> LoadHotUpdateAssemblyAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
}

public interface IConfigLoader
{
    UniTask LoadAsync(LocalStartupConfig localConfig, CancellationToken cancellationToken);
    void Clear();
}

public interface IHotUpdateEntry
{
    UniTask StartAsync(CancellationToken cancellationToken);
}
```

## 推荐文件结构
```text
Assets/Scripts/Fuel/Launcher/
  Launcher.cs
  GameUpdatePipeline.cs
  StartupStep.cs
  Config/
    LocalStartupConfig.cs
    RemoteVersionInfo.cs
    IStartupConfigProvider.cs
    ResourcesJsonStartupConfigProvider.cs
  Version/
    IVersionService.cs
    HttpJsonVersionService.cs
    IAppVersionChecker.cs
    SemanticAppVersionChecker.cs
    IAppUpdateHandler.cs
  Resources/
    IResourceUpdateService.cs
    YooAssetResourceUpdateService.cs
  HybridCLR/
    IHybridCLRLoader.cs
    HybridCLRLoader.cs
  Table/
    IConfigLoader.cs
    TableKitConfigLoader.cs

Assets/Editor/Launcher/
  StartupConfigEditorWindow.cs

Assets/HotUpdate/GameEntry/
  HotUpdateEntry.cs
```

## 实现原则
- 启动管线本体放主工程，保证没有热更 DLL 时也能提示错误或修复。
- 本地 JSON 配置必须随包内置，且启动阶段不依赖 YooAsset 热更资源。
- 业务入口放热更程序集，主工程只通过接口或反射调用。
- 资源更新必须先于 DLL 和配置加载，因为 DLL 与配置都应作为 YooAsset 资源管理。
- 配置加载不再直接依赖 `Resources.Load`，应通过 TableKit 自定义 loader 从 YooAsset 加载 TextAsset。
- 下载器进度、错误、重试、取消由 UI 层订阅启动管线事件，不写死 UI。
- 移动端、PC、Web 的平台差异通过接口实现分支隔离。

## 待技术验证事项
1. WebGL + HybridCLR 当前版本链路是否支持运行时加载热更 DLL。
2. WebGL 下 YooAsset 资源更新的缓存、跨域、压缩格式、持久化行为。
3. HybridCLR AOT 元数据 DLL 列表应由构建流程生成，启动配置只引用构建产物路径。
4. 热更 DLL 是否需要加密/签名校验。
