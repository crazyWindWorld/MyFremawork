---
id: kd_d3191568-9359-48b0-9e65-a27fafcdf79e
type: memory
path: yooasset-resource-manager.md
title: yooasset-resource-manager
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1779242958684
updatedAt: 1779243234643
---

# yooasset-resource-manager

## Summary
YooAsset 桥接资源管理层的位置、初始化方式、支持模式、主要接口，以及 SceneManager 通过它加载/卸载场景。

<!-- locus:maintain-rules:start -->
Record verified resource-management architecture and usage constraints. Keep entries concise and update when API or initialization flow changes.
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
- 项目已接入 `com.tuyoogame.yooasset` 3.0.1-beta。
- 桥接层位于 `Assets/Scripts/Manager/ResourceManager/ResourceManager.cs`，命名空间 `Fuel.Manager.ResourceManager`，继承现有 `Singleton<T>`。
- 默认包名为 `DefaultPackage`，初始化入口：`ResourceManager.Instance.InitializeAsync(packageName, playMode)`。
- 当前支持 `EditorSimulateMode` 和 `OfflinePlayMode`；未配置远端服务前不要使用 Host/Web 模式。
- 加载接口包括：`LoadAssetSync<T>`、`LoadAssetAsync<T>`、`InstantiateSync`、`InstantiateAsync`、`LoadSceneAsync`、`UnloadSceneAsync`、`ReleaseAsset`、`ReleaseAllAssets`、`UnloadUnusedAssetsAsync`。
- 管理器缓存 YooAsset `AssetHandle`，调用方释放资源应通过 `ReleaseAsset(location)` �� `ReleaseAllAssets()`，不要直接释放内部 handle。
- `Assets/Scripts/Manager/SceneManager/SceneManager.Loading.cs` 的场景加载/卸载已改为通过 `ResourceManager.Instance.LoadSceneAsync` 和 `ResourceManager.Instance.UnloadSceneAsync` 执行；场景配置 `SceneInfo.ScenePath` 现在需要是 YooAsset 可识别的场景 location。
<!-- locus:body:end -->
