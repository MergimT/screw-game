# Architecture

Plain C# assemblies with constructor wiring from one composition root. There is no DI container, service locator, or general-purpose framework.

## Assemblies and ownership

| Assembly | Engine refs | Owns | Specialist owner |
| --- | --- | --- | --- |
| Contracts | none | enums, commands, `CommandResult`, ordered `VisualEvent`s, `SettledSnapshot`, `HintResult`, `HelpBalance`, `RewardGrant`, `EntitlementGrant`, `PrivacyState`, `DiagnosticRecord`, `GameConfig`, service interfaces | gameplay architect (changes need review) |
| Core | none | immutable `LevelDefinition` → `CompiledLevel`; mutable `PuzzleState`; `Rules`; `PuzzleEngine` (revision, undo history); `LevelHasher` | rules engineer (Prompt 03) |
| Validation | none | `LevelValidator`, bounded `Solver`, witness replay, `HintService` | solver engineer (Prompt 04) |
| Persistence | none | `ProfileData`, `SaveStore` (checksummed envelope, primary+backup, atomic write, quarantine, migrations), `LevelJson` | persistence engineer (Prompt 05) |
| Progression | none | `Campaign`, `DailyChallenge` | progression engineer (Prompt 11) |
| Session | none | `GameSession`, the single writer of the profile and the active attempt | session owner (Prompt 05) |
| Services | UnityEngine | adapters: clock, analytics, ads, IAP, consent, haptics, crash reporting | integration engineers (Prompts 12–14) |
| Presentation / UI | UnityEngine, Input System, uGUI/TMP | `GameRoot` composition root, `BoardView`, `CameraRig`, `UiKit`, `Localization`, `SoundBank` | renderer/UI engineers (Prompts 06–08); `GameRoot` is owned by the lead |
| Editor | Editor only | `ProjectSetup`, `BuildScripts`, level authoring tools | lead (Prompt 02), authoring (Prompt 09) |
| Tests | per assembly | EditMode tests; also compiled under .NET 8 by `tools/dotnet` | each owner for their area |

Dependency direction: Contracts ← Core ← Validation ← Session → Persistence, Progression. Presentation depends on Session and Services. Nothing depends on Presentation. Core, Validation, Session, Persistence and Progression have `noEngineReferences` and must never use `UnityEngine`, wall-clock time, randomness without a seed, or SDKs.

## Identity and versions

| Identifier | Where | Rule |
| --- | --- | --- |
| Level ID (`L01`…) | `LevelDefinition.Id` | stable forever; never reused for different content |
| Part/screw IDs | definitions | stable within a level; used by saves, hints, witnesses, analytics |
| `SchemaVersion` | level JSON | format of the level file |
| `ContentVersion` | level JSON | bumped for any rules- or geometry-relevant edit |
| `RulesVersion` | `RulesDefaults` | bumped for any change in rule semantics |
| `ContentHash` | `LevelHasher` | SHA-256 over schema, rules version, ID, content version, capacities, queue, parts, screws and camera range |
| `SaveVersion` | `ProfileData` | profile format; `Migrations` upgrades older versions in place, and newer versions open read-only |
| `Solver.Version` | validation reports | reports record rules and solver versions |
| `AttemptId` | `AttemptRecord` | new on every start or restart |
| `Revision` | `PuzzleEngine` | starts at 0 per attempt; +1 per accepted move or undo; never decreases |

## State boundaries

- **Immutable:** `LevelDefinition` and `CompiledLevel`, which are never mutated after compile.
- **Puzzle payload:** `PuzzleState` (attached screws, released parts, trays, queue cursor, buffer slots and sequence numbers, outcome). This is the only thing undo snapshots contain.
- **Attempt record:** payload + revision + history + attempt ID + per-attempt free help + `Closed`/`Terminal`, persisted in `ProfileData.ActiveAttempt`.
- **Durable progression:** completed levels, collection, daily completions and pins.
- **Ledgers:** `LedgerData.FulfilledRewards`, `FulfilledPurchases` and `OwnedProducts`, plus banked help credits. They are append-only by ID and are never restored by undo or restart.
- **Settings and consent:** `SettingsData` and `ConsentData`.

## Session orchestration (single writer)

`GameSession` is the only code that mutates the profile. For every command it follows these steps:

1. Reject when `_busy` (re-entrancy, double taps), the attempt is closed, or `ExpectedRevision` is stale.
2. Snapshot the profile, puzzle state, revision and history.
3. Apply the command in the engine. The engine commits logically and stabilizes, then returns ordered events.
4. Copy the attempt into the profile, including victory recording (`Closed`, `Terminal=Won`, campaign/daily progress) and help spending.
5. Write the whole profile atomically with `SaveStore.TrySave`: temp file, then replace, with the previous file kept as backup.
6. On success, flush deferred analytics and return the events. On failure, restore every snapshot and return `SaveFailed`, which means no events, no audio and no progress.

Ownership:
- **Victory recording:** the session records it inside the same atomic write as the winning move. Analytics fire only after the write succeeds.
- **Help consumption:** undo spends a credit inside the same transaction as the undo. A hint spends one only in `ApplyHint`, after the binding is re-checked.
- **External reward fulfillment:** the ad adapter only reports `AdShowResult.Earned` with an operation ID. `GameSession.GrantReward(fulfillmentId)` is the sole grantor. It is idempotent through `FulfilledRewards`, so duplicate or late callbacks grant at most once, including across relaunches.
- **Purchases:** the purchase adapter reports store-verified `EntitlementGrant`s. The session appends them to `OwnedProducts`/`FulfilledPurchases` idempotently. Restore re-applies them without duplicating.

## Commit before animation, and settled reconstruction

The logical state and the durable write complete before any `VisualEvent` reaches the renderer. The renderer disables colliders for removed screws immediately and animates only from events. It never reads animation state back into the rules. On pause, focus loss, an interrupted animation, undo, restart or relaunch, the renderer receives `StateReplacedEvent` (or calls `BoardView.Rebuild`) and rebuilds the settled scene from `PuzzleState` alone. Running tweens are discarded. Because state is committed first, a crash mid-animation loses only presentation.

## Composition root, consent and SDK defaults

`GameRoot` is the only composition root. It constructs the storage, `SaveStore`, profile, session, campaign and adapters, then initializes them in a fixed order:

1. Before any SDK starts, the native defaults are set to **collection off**: Firebase Analytics `firebase_analytics_collection_enabled=false`, Crashlytics `firebase_crashlytics_collection_enabled=false`, and `google_analytics_default_allow_*` denied. These go in AndroidManifest meta-data and the iOS Info.plist, and they are added by the integration prompts.
2. Load the profile. Offline play is available immediately.
3. Run the UMP consent flow. It decides `CanRequestAds` only.
4. On iOS, ask for App Tracking Transparency after UMP when personalized ads are configured. It decides the tracking identifier only.
5. The analytics/crash opt-in is a separate player setting (`ConsentData.AnalyticsAllowed`). `SetCollectionEnabled(true)` is called only when it is `true`.
6. Only then initialize the ads SDK, IAP, and Remote Config (whose values `GameConfig.FromRemote` clamps).

Feature code never initializes an SDK or changes a consent setting. The three decisions (`AdConsent`, `Tracking`, `AnalyticsAllowed`) are stored and exposed separately in `PrivacyState`, and none implies another. A failed or offline SDK never blocks campaign play.

## Compatibility policy

- An active attempt resumes only when `ContentHash` and `RulesVersion` match. Otherwise it is marked `Replaced` and a fresh attempt starts (`StartKind.ReplacedIncompatible`), and help already spent is not refunded.
- Shipped level IDs are never deleted or reused. A content update either keeps the old definition available (same ID, same hash) or bumps `ContentVersion` and accepts the replacement above. Completion records are keyed by level ID and survive content edits.
- Daily challenges pin the level ID per UTC date on first view, so content and clock changes can't swap a day's level.
- Save migrations are explicit, step-by-step (`Migrations`), and tested. A newer-than-supported save opens read-only and is never overwritten.

## Test adapters and release exclusion

Fakes include `TestRewardedAds`, test purchases, `MemoryStorage` and `SequentialIds`. `MemoryStorage` and `SequentialIds` live in test assemblies or engine-free code with no fulfillment power. `TestRewardedAds` and any fake store are compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD`, and they report `IsTestAdapter = true`. Release builds construct only the real adapters or the `Unavailable*` adapters. `BuildScripts` release methods build without `DEVELOPMENT_BUILD`, and the release audit (Prompt 16) checks that no `IsTestAdapter` type is present in a release player.
