# Implementation status — Project Screw (native Unity)

Evidence states: **implemented** (code exists) · **automatically verified** (tests/tools executed, output recorded) · **device verified** (ran on a physical phone) · **blocked** (named external gate). Implementation and verification are tracked in separate columns and are not interchangeable.

Last updated: 2026-09-26. Owner of every row: lead agent (Devin session) unless noted.

| Stage | Acceptance criteria (abridged from the pack) | Implementation | Verification evidence | Blockers |
| --- | --- | --- | --- | --- |
| 01 Architecture & contracts | GAME_SPEC/ARCHITECTURE/DEPENDENCIES; shared commands/results/events/snapshots/save/hint/help/reward/purchase/diagnostic contracts; ownership rules | implemented (`Contracts/`, `docs/`) | automatically verified: contracts compile under .NET 8 (`dotnet test tools/dotnet/ScrewGame.Tests`) | Unity compile of asmdefs blocked (license) |
| 02 Unity project & builds | Pinned editor/packages, URP, Input System, bootstrap scene, safe area, composition root, idempotent setup, build entry points, docs | implemented: `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, `Editor/ProjectSetup.cs`, `Editor/BuildScripts.cs`, `Presentation/GameRoot.cs`, `SafeArea` | Unity 6000.3.24f1 + iOS/Android modules installed; `Unity -version` → `6000.3.24f1`. Package resolve / compile / scene start **not run** | **blocked**: no Unity license entitlement (`com.unity.editor.headless` not found); iOS archive needs macOS+Xcode |
| 03 Deterministic rules | Plain C# rules, ordered events, rejected no-ops, revisions, resolver fixture, invariants, trapped/solvable tests | implemented (`Core/`) | automatically verified: 96/96 NUnit tests pass (rules, invariants, replay) | — |
| 04 Validator/solver/hints | Structural validation, bounded solver with SOLVED/UNSOLVABLE/INCONCLUSIVE, witness replay, current-state hints, stale rejection, batch reports | implemented (`Validation/`, `tools/dotnet/ScrewGame.Cli`) | automatically verified: tests + batch CLI → L01–L10 `LOGIC_OK`, `SOLVED`, replay `WIN` (`reports/level-validation.json`) | geometry visibility pending Unity (Prompt 09) |
| 05 Saves/session/lifecycle | Versioned profile, atomic write + backup, corruption/migration handling, commit-before-animate, durable help, idempotent rewards | implemented (`Persistence/`, `Session/`) | automatically verified: fault-injected storage tests (write failure rollback, corrupt primary/backup, checksum, newer version read-only, duplicate reward) | real app lifecycle (pause/kill) needs Unity player/device |
| 06 Board/camera/picking | Build views from state, orbit with clamped tilt, tap vs drag, nearest-hit picking through occluders, settled rebuild | implemented (`Presentation/BoardView.cs`, `CameraRig.cs`, `GameRoot.cs`) | not verified — never compiled by Unity | blocked: Unity license |
| 07 Art/audio/haptics | Original look, motion, reduced motion, sounds, haptics | partially implemented: procedural primitives, palette + symbols, synthesized `SoundBank`, `DeviceHaptics` | not verified | blocked: Unity license; art pass pending |
| 08 UI/tutorial/a11y/loc | Home, level list, HUD, win/loss, settings, tutorial, EN/FR, symbols, reduced motion | partially implemented (`UiKit`, `Localization`, `GameRoot` screens); tutorial overlay minimal | not verified | blocked: Unity license |
| 09 Level editor & geometry | In-Editor authoring, deterministic export, geometric visibility check | not started (Python authoring script `tools/levels/author_levels.py` is interim) | — | Unity license |
| 10 Ten-level prototype → campaign | 10 handmade validated levels, then reviewed campaign toward 100 | 10 levels authored | logically verified (see 04); not playtested; no human playtest claimed | Unity license, human review |
| 11 Collection & daily | Campaign unlocks, collection shelf, UTC daily pinned per date | implemented (`Progression/Campaign.cs`) | automatically verified: daily determinism/pinning/UTC tests | UI integration unverified |
| 12 Analytics/crash/remote | Firebase Analytics + Crashlytics adapters, consent gating | stub only (`LocalAnalytics`, sends nothing) | — | Firebase project files (owner) |
| 13 Consent & rewarded ads | UMP + Google Mobile Ads, test units, idempotent rewards | contracts + honest `UnavailableRewardedAds`; editor/dev `TestRewardedAds` | reward idempotence tested at session layer | AdMob app IDs (owner), SDK not yet added |
| 14 Purchases | Unity IAP, one non-consumable cosmetic, restore | contracts only | — | store products (owner) |
| 15 Performance & builds | Profiling, repeatable signed builds | build scripts written | — | license, devices, signing |
| 16 Release audit | Independent audit | not started | — | 15 |
| 17 Store package & handover | Store copy, runbooks | not started | — | 16 |

Browser prototype (`index.html`, `src/`) is preserved and is **not** evidence for any native stage.
