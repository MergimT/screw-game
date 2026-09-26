# Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-26 | Unity **6000.3.24f1** (6.3 LTS), changeset `4e7b9b5b6244` | Pack baseline 6.3 LTS; exact patch available for Linux with iOS/Android modules at time of install. |
| 2026-09-26 | Packages pinned to the editor's bundled/verified versions: URP 17.3.0, uGUI 2.0.0 (includes TextMeshPro), Input System 1.20.0, Test Framework 1.6.0, Newtonsoft JSON 3.2.2 | Read from the editor's `Resources/PackageManager/Editor/manifest.json`; no guessed versions. |
| 2026-09-26 | Input System only (`activeInputHandler = 1`) | Single pinned input path; touch + mouse via `Pointer`. |
| 2026-09-26 | IL2CPP, ARM64, Android min API 26, iOS min 15.0, portrait only, Linear color | Store-compatible mobile defaults. |
| 2026-09-26 | Rules/validation/persistence/session/progression are engine-free assemblies (`noEngineReferences`) and are also compiled by `tools/dotnet` for .NET 8 NUnit | Lets rules be verified without a Unity license; single source files, no second implementation. |
| 2026-09-26 | Native Core is a new implementation, not a port of `src/logic.js` | Browser rules differ from the pack's contract (tray queue, buffer insertion order, loss rule). |
| 2026-09-26 | UI is built in code (uGUI + TMP) from `GameRoot` rather than prefabs for the prototype | Keeps shared-scene surface to one GameObject; reduces merge/GUID risk while Unity cannot be run here. Revisit for art pass. |
| 2026-09-26 | Geometry uses Unity primitives with procedural materials; audio synthesized at runtime | Guaranteed-original assets for the prototype. |
| 2026-09-26 | L10 blockers changed to `door`, `clock_b` | Original design was proved UNSOLVABLE by exhaustive search. |
| 2026-09-26 | Placeholder app id `com.nyrico.projectscrew.dev`; release builds require `SCREW_BUNDLE_ID` | Pack forbids silently shipping placeholders. |
| 2026-09-26 | Rewarded ads: release uses `UnavailableRewardedAds` until the AdMob/UMP adapter exists; fake adapter compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD` | Release excludes fake fulfillment. |
| 2026-09-26 | Did not accept Unity Hub terms or script Unity licensing on the owner's behalf | Terms (updated 2026-06-30, §17.2) restrict AI agents; legal acceptance belongs to the account owner. |
