---
id: kd_5a1dae20-5009-41f9-b87e-12bd3e59e8df
type: memory
path: log-system.md
title: log-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778723396656
updatedAt: 1778723396657
---

# log-system

## Summary
Project log manager uses `Fuel.Log.LogManager` as a static Unity Debug wrapper with sender labels, enable switches, Unity context overloads, and rich text helpers.

<!-- locus:body:start -->
# Log System

- Log manager entry: `Fuel.Log.LogManager` in `Assets/Scripts/Log/LogManager.cs`.
- Current API is static and wraps Unity `Debug.Log`, `Debug.LogWarning`, and `Debug.LogError`.
- Supports manually specified sender names through `Log(string sender, object message)` style overloads.
- Supports Unity object sender/context through `Log(Object sender, object message)` style overloads, passing the object as Unity Console context.
- Switches: `EnableLog`, `EnableWarning`, `EnableError`, `EnableRichText`.
- Rich text helpers: `Color`, `Bold`, `Italic`.
<!-- locus:body:end -->
