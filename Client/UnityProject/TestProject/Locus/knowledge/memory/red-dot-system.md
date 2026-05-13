---
id: kd_ffad5236-6732-4e4d-a1be-025f288eeb24
type: memory
path: red-dot-system.md
title: red-dot-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
aiMaintained: true
explicitMaintenanceRules: true
createdAt: 1778654795747
updatedAt: 1778656170026
---

# red-dot-system

## Summary
RedDot system structure, applied runtime/editor optimizations, and remaining known issues.

<!-- locus:maintain-rules:start -->
- Keep only durable and reusable project memory
- Consolidate duplicates or conflicts into the latest conclusion
- Remove temporary context, one-off tasks, and unsupported guesses
<!-- locus:maintain-rules:end -->

<!-- locus:body:start -->
# RedDot system snapshot

Observed in `Assets/Scripts/RedDot`; last updated after editor/runtime optimization on 2026-05-13.

- Core singleton: `Fuel.RedDot.RunTime.RedDotTree`; root is a `RedDotNumberNode` named `RedDotTreeRoot`.
- Config asset type: `RedDotConfigAsset`; current loader has the runtime asset-loading line commented out, so external initialization or assigning `m_instance` is required before `RedDotConfigAsset.Instance` is usable.
- Main runtime files: `RedDotTree.cs`, `RedDotNodeBase.cs`, `RedDotNumberNode.cs`, `RedDotViewNode.cs`, `RedDotViewBase.cs`, `RedDotConfigAsset.cs`.
- Editor tools load config from hard-coded `Assets/AssetsPackage/Main/RedDot/RedDotConfigAsset.asset`, while the only indexed example asset is `Assets/Scripts/RedDot/Example~/Resource/RedDotConfig.asset`.
- Runtime optimizations already applied:
  - `RedDotNumberNode.SetStateByAccumulation` now clamps after applying delta by checking `RedDotCount < 0`.
  - View red-dot date checks now use local time consistently through `ShouldShowViewRedDot`: day compares `Date`, week compares Monday week start, month compares year + month.
  - `RedDotNodeBase.InitNode` and `GetRedDotNode` now split the path once per public call and recurse by array index, avoiding per-level `Split` and `Remove` allocations.
- `RedDotConfigEditorOdin.cs` layout improvements already applied:
  - Main window uses left content area + right detail/creation panel.
  - List/page/search views use compact table rows; detailed editing moved to the right-side selected-item panel.
  - Header includes save + enum generation; tree mode has expand/collapse all controls.
  - Search matches `Path`, `Id`, and `Alias` case-insensitively.
  - Most config edits now mark dirty and rely on explicit save instead of saving after every small operation.
- Remaining notable issues:
  - `RedDotViewBase.Init()` is never called by Unity lifecycle in the base class.
  - `RedDotConfigAsset.Instance` can return null unless another system initializes the private static instance.
  - `ResetStatus` for `RedDotViewNode` sets `Viewed = false`, meaning reset makes view red dots visible and deletes local save. User explicitly deferred this semantic risk.
<!-- locus:body:end -->
