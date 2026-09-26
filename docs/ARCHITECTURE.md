# Architecture

Assemblies (`Assets/ScrewGame/*/*.asmdef`):

| Assembly | Engine refs | Owns |
| --- | --- | --- |
| Contracts | none | enums, `VisualEvent`, command/hint/result records, service interfaces (`IRewardedAds`, `IAnalytics`, `IHaptics`, `IClock`, `IDurableStorage`) |
| Core | none | immutable `LevelDefinition` → `CompiledLevel`; mutable `PuzzleState`; `Rules`; `PuzzleEngine` (revision, undo history) ; `LevelHasher` (content hash) |
| Validation | none | `LevelValidator`, `Solver` (bounded, canonical keys, witness replay), `HintService` |
| Persistence | none | `ProfileData` (settings, progress, daily, help, ledger, consent, active attempt), `SaveStore` (checksummed envelope, atomic write, backup, quarantine, migration), `LevelJson` |
| Progression | none | `Campaign`, `DailyChallenge` (UTC key, pinned per date) |
| Session | none | `GameSession`: single writer; serializes commands; commits profile durably **before** returning events; owns victory recording, help consumption, reward fulfillment (idempotent by fulfillment ID); hint binding to attempt/revision/hash |
| Services | UnityEngine | adapters: clock, local analytics, unavailable/test rewarded ads, haptics |
| Presentation | UnityEngine, Input System, uGUI/TMP | `GameRoot` composition root (startup + consent policy, SDK init order), `BoardView`, `CameraRig`, `UiKit`, `Localization`, `SoundBank` |
| Editor | Editor only | `ProjectSetup`, `BuildScripts` |

Flow: input → `GameRoot.TryRemove(id, revision)` → `GameSession.Remove` → `PuzzleEngine` applies + stabilizes → profile saved (rollback on failure) → ordered `VisualEvent`s → `BoardView` animates (colliders disabled immediately). On pause/focus loss or interruption, `BoardView.Rebuild(state)` reconstructs the settled view from state alone.

Versions: `RulesVersion`, level `ContentHash` (SHA-256 of canonical JSON), `ProfileData.SaveVersion`. A saved attempt resumes only if hash and rules version match; otherwise a new attempt starts. Newer save versions open read-only.

Undo restores the prior puzzle payload with a **new** revision; it never touches help balances, ledgers, settings, or progression. Victory closes the attempt and is recorded before analytics.

Test doubles (`TestRewardedAds`, `MemoryStorage`, `SequentialIds`) are compiled only in tests or `UNITY_EDITOR || DEVELOPMENT_BUILD`.
