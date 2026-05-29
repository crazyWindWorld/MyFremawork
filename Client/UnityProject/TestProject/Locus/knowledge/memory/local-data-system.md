---
id: kd_d4675c58-f6bb-4936-acf9-3ae257718ed9
type: memory
path: local-data-system.md
title: local-data-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779951266304
updatedAt: 1779951725475
---

# local-data-system

## Summary
本地数据工具位于 `Assets/Scripts/Fuel/LocalData/LocalDataManager.cs`，命名空间 `Fuel.LocalData`，支持三种后端和可选异或混淆。

<!-- locus:body:start -->
# Local Data System

- 本地数据工具入口：`Fuel.LocalData.LocalDataManager.Instance`。
- 代码文件：`Assets/Scripts/Fuel/LocalData/LocalDataManager.cs`。
- 支持 `LocalDataStorageType.PlayerPrefs`、`JsonFile`、`BinaryFile` 三种存储后端。
- 默认后端是 `JsonFile`，文件目录为 `Application.persistentDataPath/LocalData`。
- 切换后端使用 `SetStorageType(LocalDataStorageType type)`。
- 常用接口：`Save<T>`、`TryLoad<T>`、`SaveString`、`TryLoadString`、`Delete`、`HasKey`。
- 可选异或混淆：`SetEncryption(bool enabled, string key = null)`；默认密钥为 `FuelLocalData`，传入非空 key 会替换密钥。
- 开启混淆后，保存前会对字符串做 UTF-8 异或并 Base64，读取后反向还原；三种后端统一生效。
- Json 和 Binary 后端当前都以 UTF-8 保存字符串；Binary 使用 `.bytes` 扩展名。
<!-- locus:body:end -->
