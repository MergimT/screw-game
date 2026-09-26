# Screw Game — Complete AI Agent Prompt Pack

Prepared for Nyrico · 26 September 2026

This is a self-contained implementation prompt pack for an original Unity screw-sorting game. It expands the current Screw-Game-Build-Brief.md into executable agent assignments. It contains prompts, not a finished game or evidence that any native build has passed.

## How to use this pack

1. Put this file in the root of the game repository, retaining the filename `Screw-Game-AI-Agent-Prompts.md`. An empty project folder is fine.
2. Open that folder with a coding agent that can edit files and run commands. A Mac with Unity and Xcode is the intended local iOS build environment.
3. Paste **Prompt 00**. It tells a lead agent to coordinate the complete implementation. If subagents are available, it can delegate independent work; otherwise it executes the stages itself.
4. For more control, use Prompts 01–17 individually in their dependency order. Give every new agent access to this entire file, not just an isolated task fragment.
5. Use Prompt 18 only once real usage data exists. Prompts 19 and 20 resume interrupted work and fix defects.

The practical first checkpoint is a playable ten-level game. Continue toward the complete release scope after checking that interaction and level quality hold up. Missing accounts, licenses, tools, or phones must be reported precisely; an agent can continue independent work without pretending those checks passed.

## Shared product contract — all agents must read

### Product

Create a portrait, offline-playable 3D screw-sorting game for iOS and Android. Launch iOS first while maintaining Android builds from the prototype onward. Internal codename: **Project Screw**; this is not an availability-checked public brand.

Players rotate a small original object, remove accessible colored screws, fill color-matching trays, and release its parts. Completed objects appear assembled on a collection shelf. Start with workshop objects, small toys, and miniature buildings. Target understandable, satisfying play for a broad general audience. Do not make voice input or generative AI a gameplay dependency.

Genre references are Screwdom, Screw Sort 3D, Screw Out, and Dévisser des Vis. Use their general genre as inspiration; create original models, layouts, levels, branding, audio, and store assets. Their internal engines and commercial results are not established by the earlier research.

### Technical direction

- Unity + C# + URP. The existing brief selects Unity 6.3 LTS. Check the actual installed editor and official release support, then pin a compatible patch. Do not silently switch engine or upgrade a working project.
- Unity Canvas/uGUI and TextMeshPro for UI, with the input package/configuration selected and pinned during setup.
- Plain C# puzzle rules independent from Unity scenes, physics, animation, SDKs, and wall-clock time.
- Versioned level data; authoring tools inside Unity; original reusable meshes, optionally authored in Blender.
- Local versioned saves; no mandatory account or backend for ordinary puzzle play.
- Google Mobile Ads Unity plugin and UMP for the advertising integration; Unity IAP for platform store purchases; Firebase Analytics and Crashlytics, with Remote Config after the core is working.
- Third-party services are adapters. Offline or failed services never stop campaign play. Editor test doubles must be clearly distinguished from real native integrations.
- Commit source, package locks, ProjectSettings, scenes, prefabs, and their `.meta` files. Exclude generated caches, credentials, signing keys, and build outputs from ordinary source control. Use Git LFS only where justified for large assets.

### Exact default rules

1. A level declares two active tray positions, tray capacity three, and five temporary buffer slots. The initial ordered tray-color list contains at least two entries. The first two fill the active positions; later entries are replacements. These are the v1 defaults.
2. Each screw belongs to exactly one part and has a stable ID and color. A part has one or more securing screws and a list of blocker parts. All blockers must be released before any screw on that part is structurally selectable.
3. Picking additionally requires visibility from the current camera. Authoring must make structurally available screws reachable from permitted viewpoints. Camera rotation is free; camera orientation never determines loss.
4. A removal routes the screw to the lowest-index matching tray with room. If no tray matches, use the lowest-index empty buffer slot. If neither destination exists, reject the move without changing any state.
5. Buffer slots retain their indexes. Every insertion gets a monotonic sequence number. Automatic transfers use insertion order, not slot index; a reused low-numbered slot does not jump the queue.
6. Commit the removal logically, then release every part that has no screws left on the object. Produce presentation events for both actions.
7. Resolve automatic changes in this exact loop: collect all full trays by ascending tray index; refill empty tray positions from the remaining queue in ascending index order; find the earliest-inserted buffered screw that fits any active tray; move that one screw to its lowest-index matching tray; repeat. Stop when no collection, refill, or transfer remains. A position stays inactive if the queue is exhausted.
8. After stabilization, win only when all screws are in completed trays and all parts are released. An empty object with screws in the buffer is not a win.
9. Otherwise, lose when no structurally available screw has a legal destination and no automatic transfer remains. A full buffer alone is not a loss: a direct-to-tray removal can still be legal.
10. A rejected command is a no-op: no revision increment, inventory change, sound suggesting success, or undo entry. Animation and falling physics never choose logical outcomes.
11. Restart restores the authored level and tray order but creates a new attempt ID. Undo restores the preceding puzzle payload, including tray replacements, buffer insertion order, released parts, and outcome, then assigns a fresh increasing session revision. Never rewind a session revision from a snapshot. Allow undo after loss; loss is recoverable within the same attempt. Close the attempt once victory is committed, so undo cannot revoke a recorded win.
12. Default help policy: one free undo and one free successful hint per attempt; unlimited free restarts. An optional rewarded ad grants one additional help credit. No paid consumable currency in v1. Returning from pause or relaunch must not reset the free allocation.
13. Puzzle undo snapshots exclude purchase entitlements, reward transaction ledgers, help-credit balances, settings, and permanent progression. Help consumption is durable and must not be refunded accidentally by undo. If an operation fails before being applied, recover or roll it back deliberately.
14. Every campaign level must have a verified solution under its ordinary tray/buffer capacities, without hints, ads, or purchases. No campaign timer and no hidden paid difficulty changes.
15. Bind each asynchronous hint search/result to attempt ID, session revision, and content hash. Discard stale results after a move, undo, restart, or changed ad-return context. Consume a help credit only when a still-current useful hint is presented.

Example resolver fixture: immediately before a tap, active trays are A with 2 screws and B with 2; remaining replacement colors are C then A. Buffer entries by insertion time are C, A, B. Tapping an available A completes the first tray, which becomes C. Buffer C moves there. Buffer A cannot move yet, so buffer B completes the second tray, which becomes A. Buffer A then moves. Final trays: C with 1 and A with 1, empty buffer, two newly completed trays. Encode this expected behavior in a small valid test state.

### Release scope and boundaries

Working v1 includes a tutorial, rotation and removal, validated campaign targeting 100 levels, restart/undo/hints, reliable resume, collection shelf, prevalidated daily challenge, English/French UI, settings, symbol-assisted colors, reduced motion, sound/haptics, diagnostics, optional rewarded ads, store purchases, and native build/release documentation.

Initial monetization implementation: rewarded help plus one optional non-consumable cosmetic theme pack. The cosmetic pack changes appearance, never puzzle capacity or the solver. Use a development product ID until real store identifiers are configured. Show localized store prices only after successful product retrieval. Interstitial ads and a matching remove-interstitials product are later options, disabled by default. Do not sell ad removal while there are no interstitials to remove. No subscriptions, energy gates, multiplayer, paid currency, or compulsory accounts in v1.

The collection and daily challenge use modest cosmetic progression. They are not a competitive or fraud-resistant economy. Native SDK integrations, store products, physical-device qualification, and distribution remain real work even if all C# source exists.

### Working agreement

- Implement each assignment; do not stop at a plan when tools allow implementation.
- Inspect the repository and applicable project instructions. Preserve user changes and existing project rules. Do not create an overriding AGENTS.md to bypass them.
- Read current official documentation before choosing SDK APIs, platform targets, signing settings, or submission requirements. Record exact versions and sources in `docs/DEPENDENCIES.md`. Do not guess package versions from old examples.
- Make reasonable implementation choices and record them. Ask a focused question only when the missing answer prevents safe progress or changes the product materially. Collect genuine setup blockers in `docs/OWNER_SETUP.md` while continuing independent work.
- Keep tests meaningful: deterministic rules, persistence, reward fulfillment, and actual user flows. Avoid tests that only repeat implementation or inflate a test count.
- Evidence states are **implemented**, **automatically verified**, **device verified**, and **blocked**. They are not interchangeable. Record commands, exit codes, build/configuration, and device/OS where relevant.
- Do not claim that generated source, mock ads, simulated purchases, screenshots, or an Xcode export prove a complete native product.
- Keep public submission, live paid campaigns, and expenditure outside this pack's default execution scope. Prepare concrete artifacts for the owner; use existing explicit authorization if the owner later requests an external action.
- End each assignment with files changed, behavior delivered, evidence, unresolved blockers, and the next dependency-ready assignment. Update its own status report; the coordinator maintains shared status.

## Dependency and ownership map

One lead owns shared integration files. Agent labels are roles; a single agent can perform multiple roles.

| Prompt | Assignment | Prerequisites | Primary ownership |
| --- | --- | --- | --- |
| 00 | Coordination | This pack | Integration, status, shared files |
| 01 | Architecture and contracts | 00 | `docs/`, `Assets/ScrewGame/Contracts/` |
| 02 | Unity setup and early mobile builds | 01 | Project configuration, bootstrap, build scripts |
| 03 | Deterministic rules | 01; compiler setup from 02 | `Core/`, core tests |
| 04 | Validation, solver, hints | 03 | `Validation/`, solver tests |
| 05 | Saves, sessions, lifecycle | 03 | `Persistence/`, `Session/` |
| 06 | 3D board, camera, picking | 02, 03, 05 | `Presentation/`, board assets |
| 07 | Art, audio, motion, haptics | 06 | Art/audio assets and presentation components |
| 08 | UI, tutorial, accessibility | 04, 05, 06 | `UI/`, localization |
| 09 | Level editor and geometry checks | 04, 06 | `Editor/`, authoring tools |
| 10 | Campaign content | 07, 08, 09 | Level/model content |
| 11 | Collection and daily challenge | 05, 08, first validated content | `Progression/` |
| 12 | Analytics, crashes, configuration | 01, 05, 08 | Diagnostics service adapter |
| 13 | Consent and rewarded ads | 02, 04, 05, 08 | Consent/ad service adapters |
| 14 | Purchases and entitlements | 02, 05, 07, 08 | Purchase service adapter |
| 15 | Performance and native builds | 10–14 integration | Build configuration, profiles |
| 16 | Independent release audit | 15 | Audit reports and isolated fixes |
| 17 | Store materials and handover | 16 disposition | Release material, runbooks |
| 18 | Product iteration | Real usage evidence | Measurement and proposed changes |
| 19 | Resume | An interrupted implementation | Reconciled status |
| 20 | Repair a defect | A concrete bug | Smallest relevant implementation area |

Paths such as `Core/` are relative to `Assets/ScrewGame/`. The lead owns `Packages/manifest.json`, lockfiles, ProjectSettings, shared scenes/prefabs, and dependency decisions. Specialists request changes to these instead of editing them concurrently. UI, ads, analytics, and IAP workers can overlap after interfaces are fixed. Never run two Unity editors against the same working directory. Use isolated branches/worktrees with their own Unity caches, or serialize editor operations. Integrate at clear boundaries and compile after each integration.

## Prompt 00 — Lead implementation agent

```text
You are the lead Unity engineer and delivery coordinator for Nyrico's Project Screw.

Read the entire Screw-Game-AI-Agent-Prompts.md in this repository or attachment, including the shared product contract and dependency map. Treat it as the requested implementation scope. Inspect the repository, existing instructions, available tools, and work already done before editing.

Build the game, starting with a playable ten-level prototype and continuing through the complete v1 release candidate. Use Unity/C#/URP for iOS and Android. Follow Prompts 01–17 in dependency order. Do not replace native delivery with a web demonstration or stop after scaffolding when further implementation is possible.

Create docs/IMPLEMENTATION_STATUS.md with one row per stage, acceptance criteria, evidence state, current owner, and blockers. Maintain docs/DECISIONS.md, docs/OWNER_SETUP.md, and a short NEXT_ACTION.md. Keep implementation and verification status separate.

If your environment supports multiple agents, delegate bounded independent tasks with explicit interfaces and file ownership. Otherwise execute those roles sequentially. You own package/configuration changes and shared scene integration. Prevent concurrent Unity access to one working directory and preserve .meta files. Review every worker's changes before integration; a worker's claim is not evidence by itself.

Settle low-risk details using the pack's defaults. Verify current SDK compatibility rather than inventing package versions. Record necessary configuration without asking the owner to repeat information already available. Missing credentials or hardware should block their specific verification gates, not unrelated local implementation.

Do not mark work complete because the files exist. Exercise rules, solver witnesses, saves, input, UI, and integrations at the applicable level. Report exact commands and device evidence. Keep known failures visible and fix them in dependency order. Do not fabricate human playtesting or live metrics.

When a session must end, checkpoint working changes and status so another agent can resume. Continue until all locally achievable work is done and remaining external requirements are concrete. Prepare release artifacts; public submission and paid campaigns are outside the default scope.

Begin now with Prompt 01 and the toolchain inspection for Prompt 02. Your first checkpoint is a usable native prototype, not only a written plan.
```

## Prompt 01 — Architecture, contracts, and testable specification

```text
Act as the gameplay architect. Read the complete prompt pack and current repository instructions/status. Implement the architecture foundation and shared contracts for the specified screw game.

Inspect existing work first. Preserve a compatible implementation. If starting empty, create a simple modular structure for Core, Contracts, Validation, Presentation, Session, Persistence, UI, Progression, Services, Editor, and Tests. Avoid a general-purpose framework or elaborate dependency injection system.

Write docs/GAME_SPEC.md using the pack's exact rules, defaults, help policy, terminal-state behavior, and resolver example. Write docs/ARCHITECTURE.md and docs/DEPENDENCIES.md. Specify immutable level definitions, mutable session state boundaries, stable IDs, level/rules/save versions, content hashes, attempt IDs, and monotonic state revisions.

Implement the small shared C# contracts needed for commands, command results, ordered visual events, state snapshots, saving, hints, help credits, rewarded ads, purchases, diagnostics, and configuration. Separate puzzle state from durable progression and transaction ledgers. A HintResult must distinguish a valid suggested move, an already-unsolvable state, and search-budget exhaustion.

Define ownership of victory recording, help consumption, and external reward fulfillment. Session orchestration must prevent duplicate actions and coordinate atomic writes. Explain how logical state commits before animation and how a settled view is reconstructed after interruption. Undo restores puzzle payload while increasing the session revision. Loss is recoverable; terminal attempt outcome is tracked separately.

The shared composition root owns startup and consent policy. Define native SDK automatic-collection defaults before initialization, rather than letting feature agents initialize SDKs independently. Keep ad consent, platform tracking authorization where applicable, and analytics consent distinct.

Define the compatibility policy for versioned levels and active saved attempts. Content updates must retain referenced definitions or migrate explicitly. Specify where test adapters are allowed and how release builds exclude fake fulfillment.

Acceptance: another engineer can implement the rules and renderer independently without guessing tray order, buffer order, loss rules, reward ownership, or save semantics. Compile the contracts if the compiler is available; otherwise report that specific missing gate. Give specialist agents a stable API and bounded ownership handoff.
```

## Prompt 02 — Reproducible Unity project and device-build foundation

```text
Act as the Unity platform engineer. Read the pack and architecture, inspect the actual OS, installed Unity editors, license availability, Xcode, Android build modules, and existing project. Do not assume these tools exist.

Create or complete a reproducible Unity/C#/URP project. Use the brief's Unity 6.3 LTS baseline unless a documented compatibility requirement warrants another supported release. Pin the exact editor and compatible packages after checking official documentation. Record input-system configuration, render pipeline, scripting backend, minimum OS targets, and build prerequisites; verify platform requirements when choosing them.

Provide a bootstrap scene, portrait safe-area UI frame, service composition root, and a minimal original test object. Use a repeatable Editor setup command to create project-owned scenes, materials, prefabs, and references where practical. Re-running setup must not duplicate assets, change stable GUIDs unnecessarily, or overwrite hand-edited work.

Configure source serialization, visible .meta files, assembly boundaries, and targeted editor tests. Create repeatable development/release build entry points for iOS export and Android packaging. Clearly label placeholder app identifiers; production builds must validate required identifiers/configuration instead of silently shipping placeholders.

Attempt early mobile builds with available tools. Distinguish Unity compilation, Xcode export, Xcode archive, signed device install, Android development APK, and release AAB. A successful export alone is not an iOS build. Record exact commands, outputs, versions, and blockers in docs/BUILD_SETUP.md and docs/OWNER_SETUP.md.

Acceptance: a clean checkout can open, resolve packages, compile, and start the bootstrap scene. Run the minimal scene on an iPhone and Android phone when available. If access is missing, complete the scripts and local checks, leave device checks blocked, and let independent gameplay work proceed. Do not fabricate project IDs, credentials, licenses, or device results.
```

## Prompt 03 — Deterministic puzzle rules and undo snapshots

```text
Act as the gameplay-core engineer. Implement the exact shared rules in plain C#, without UnityEngine scene dependencies, SDK calls, clocks, camera state, physics, or animation callbacks.

Implement level initialization, legal removal discovery, command validation, direct-to-tray routing, first-empty buffer placement, insertion-order buffer transfers, ordered tray completion/refill, part release, stabilization, victory, dead-end loss, restart initialization, and deep puzzle snapshots.

Each accepted removal produces one settled logical state and an ordered list of presentation events. Reject stale, duplicate, blocked, nonexistent, or unroutable commands with a no-op result. Give commands/state a revision relationship so rapid repeated taps cannot remove the same screw twice. Session code owns durable help balances and campaign progression; the core must not roll them back through undo.

Encode the pack's resolver example as a regression fixture. Cover duplicate active colors, exhausted tray queues, reused buffer slots, multiple automatic transfers, dependency chains, full-buffer direct matches, and empty-object/nonempty-buffer states. Check loss across all structurally available screws, independently of the current view.

Enforce invariants: each screw occupies exactly one location; no tray exceeds capacity; counts and IDs are conserved; released parts contain no attached screws; rejected commands preserve state and revision; replaying identical inputs yields identical canonical states. Do not depend on unspecified dictionary iteration order or a platform-dependent string hash.

Add focused automated tests, including a deliberately trapped state and a solvable alternative. Use legal, well-formed levels for behavior tests and separate malformed-content validation tests. Exercise deterministic replay beyond one handpicked fixture.

Acceptance: the rules compile and the meaningful tests pass on the available compatible runtime. Expose an API suitable for the renderer and solver. Report actual execution evidence. Do not add cosmetic timing logic or monetization decisions to the core.
```

## Prompt 04 — Level validator, solver, replay, and useful hints

```text
Act as the puzzle-tools engineer. Use the production Core API; do not create a second implementation of the rules.

Build a level validator for stable/unique IDs, valid ownership and blocker references, dependency cycles, nonempty secured parts, color validity, capacities, minimum initial tray count, and exact per-color screw versus total tray capacity. Reject invalid content before play.

Implement deterministic bounded state-space search returning SOLVED with a winning command witness, UNSOLVABLE only after exhaustive valid search, or INCONCLUSIVE when a time/memory/state budget is reached. Add cancellation and progress reporting. Keep long-running search off the UI thread using only thread-safe plain data.

Canonical state keys must preserve remaining screws, released parts, active tray positions/colors/fills, queue cursor, and buffer insertion order. Do not merge distinct tray or buffer permutations unless equivalence is proved. Record solver/rules version and content hash with each witness, then replay the witness through the production rules engine and require a win.

Implement hints from the player's CURRENT settled state. Do not index blindly into the initial winning trace after the player takes another branch. A successful hint identifies a legal move that leads to a found solution; the presentation can rotate/highlight it through a separate adapter. Bind every search/result to attempt ID, increasing session revision, and content hash. Cancel/discard stale results after moves, undo, restart, or changed ad-return context. Revalidate before presentation and consume a credit only when the useful result still matches. An unsolvable state offers undo/restart; an inconclusive search is not called impossible. No useful hint means no help credit consumed and no ad solicited for a nonexistent result.

Export machine-readable validation reports with outcomes, witness, search effort, and assumptions. Pure logical solving does not establish physical visibility; hand geometry validation to Prompt 09.

Acceptance: fixtures exercise all three search outcomes, stale witness rejection, replay, and a current-state hint after a divergent move. Provide a batch validation command for campaign content and show actual results.
```

## Prompt 05 — Durable saves, sessions, help inventory, and lifecycle

```text
Act as the persistence and session engineer. Implement robust progression and in-progress saving around the production Core. Store logical settled state, not Transform or Rigidbody state.

Define versioned records for profile/settings, campaign/collection/daily progress, active attempt, help allocations/credits, and transaction ledgers. Include rules/content versions and hashes, attempt ID, state revision, and integrity checks. Checksums detect corruption; they do not establish purchase authenticity.

Use an atomic write strategy supported by the target platform, plus a recoverable last-good backup. Serialize writes so a late older write cannot overwrite a newer revision. Specify behavior for disk/write failure, truncation, corrupt primary/backup, missing content, migration failure, and interrupted replacement. Preserve recoverable progress and explain an actual reset to the player; never fabricate entitlements from corrupt data.

Commit a move durably before presenting its animation. On failure, restore or keep the prior committed state and expose a recoverable error. Reconstruct the settled scene after relaunch or focus loss. Persist free-help consumption so relaunch cannot reset it.

Make undo consumption and snapshot restoration one durable operation. Restore puzzle payload with a new increasing session revision; never reuse a revision from the snapshot. Puzzle snapshots must exclude entitlement ownership, reward grants, help balances, permanent unlocks, and transaction history. Restart uses a fresh attempt ID with the same authored puzzle. Victory closes an attempt and applies level/collection/daily unlocks atomically or through replay-safe recorded operations.

Use unique fulfillment IDs for external grants. Persist received rewards exactly once even if the view is gone; apply attempt-independent help credits safely to a later eligible attempt when appropriate. Do not assume a client can prove an ad reward that never reached its callback.

Acceptance: inject interruptions at meaningful save/undo/win boundaries, replay duplicate completion callbacks, test migrations and corrupt saves, and exercise real app lifecycle where available. Report the recovery outcome, not just the existence of save methods.
```

## Prompt 06 — Playable 3D objects, camera, and correct touch selection

```text
Act as the Unity interaction engineer. Connect the deterministic Core to an original 3D board scene using stable IDs and the architecture's events. Produce a genuinely playable object, initially using simple reusable meshes.

Implement portrait framing, horizontal rotation with constrained tilt, touch/mouse input for development, reliable tap-versus-drag distinction, UI input exclusion, and a deliberate multitouch policy. A released drag must never become a screw tap. Reset the gesture cleanly on pause, focus loss, modal dialogs, and touch cancellation.

Perform nearest-hit picking against both screws and occluding object geometry. Do not raycast only a selectable-screw layer. Large touch targets may improve usability but cannot select through an occluder. Structurally blocked screws remain blocked even when visible. Give clear feedback for invalid selection without consuming a move.

Build screw, part, tray, and buffer views from authoritative state. Disable gameplay picking on a released piece immediately; decorative falling pieces must not permanently block accessible screws. Camera movement cannot change structural availability or cause loss.

Serialize player actions through the session controller. Prevent new move commits during conflicting transitions. The view consumes ordered events; it must also reconstruct directly from a settled save without replaying every old animation. Restart, undo, and interrupted animations must leave no orphan views or extra colliders.

Add a repeatable scene construction/import path, a small set of playable fixtures, and integration checks for front/back occlusion, blocked screws, drag cancellation, UI taps, and restoration.

Acceptance: demonstrate tapping, rotation, sorting, release, win, loss, and restart in the available runtime. Record visual evidence and any missing physical-device checks. A visually attractive scene with incorrect through-object picking does not pass.
```

## Prompt 07 — Original art direction, motion, audio, and haptics

```text
Act as the game artist and presentation engineer. Improve the existing playable game into a coherent premium casual-game presentation. Preserve deterministic rules and existing functional controls.

Use a warm neutral background, dark readable typography, softly lit miniature objects, rounded/beveled parts, and saturated screws with distinct symbols. Keep a clean workshop-collection identity that can appeal to adults as well as younger players. Use original visual assets and document their provenance. Design for real small-screen readability, not only a large screenshot.

Create a reusable screw mesh with a readable head, object-part materials, tray visuals, shadow treatment, and a small collection-display kit. Prefer inexpensive opaque materials and controlled effects initially; justify transparent materials and profile their cost. Make original primitive-based assets if external assets or modeling tools are unavailable.

Implement coordinated unscrew/lift, travel-to-destination, tray completion, part release, win, and undo feedback. Keep animation lengths configurable and short enough for repeated play. Animate the already-committed logical result. Reduced motion must still communicate each state change. Avoid camera shake or distracting particles by default.

Provide distinct subtle sounds for selection, unscrew, tray fill, release, error, and completion, with sensible mix limits. Implement native haptics through a small adapter where supported, with settings and a safe fallback. No fake claims of device-tested haptics. Avoid unlicensed music or sound effects.

Create the cosmetic material/theme variant needed for the proposed non-consumable purchase, preserving screw color and symbol meanings and visibility.

Acceptance: capture real gameplay at representative portrait sizes, inspect readability and clipping, and record performance measurements where available. Include an asset inventory and licensing/provenance notes. Reference mockups or generated images are design aids; they are not proof of an implemented scene.
```

## Prompt 08 — Complete UI, onboarding, accessibility, and localization

```text
Act as the UI/UX engineer. Build functional screens around the existing game: home/continue, campaign access, gameplay HUD, pause/settings, loss/restart/undo, victory/next object, collection entry, daily challenge entry, and a small cosmetic shop entry once products exist.

The gameplay HUD shows active trays, buffered screws, level identity, and help controls without covering the object or requiring tiny taps. Integrate safe areas, multiple portrait aspect ratios, readable fonts, and comfortable touch targets. Keep implementation details and debug statuses out of release player flows.

Create a short interactive tutorial within the real rules. Teach removal, color matching, rotation, blocked parts, and buffer pressure progressively through the first levels. Tutorial progress survives relaunch. Never create hidden tutorial rule exceptions that later invalidate learned behavior.

Implement one-free-hint/undo-per-attempt defaults and free restart. Display remaining help honestly. Hints solve from the current state; if no useful hint exists, offer undo/restart without charging. Allow undo from loss when eligible. Avoid accidental reset with a lightweight confirmation only when it prevents losing current play.

Provide English/French strings in localization data with no scattered UI literals. Check text expansion and pluralization. Add color-plus-symbol mode, reduced motion, separate sound/haptic toggles, readable control labels, and accessibility semantics where the platform/runtime supports them. Do not claim full screen-reader playability without actual validation.

Include sensible offline, unavailable-product, unavailable-ad, interrupted-operation, and save-error states. Never leave a disabled-looking control that secretly calls a fake integration.

Acceptance: complete the new-user flow and returning-user resume flow. Inspect small and large phone layouts, safe areas, French strings, symbol mode, and reduced motion. Record which checks used editor simulation versus physical hardware.
```

## Prompt 09 — Level editor and geometric validation

```text
Act as the level-authoring tools engineer. Build a Unity Editor workflow for making levels without modifying gameplay scripts. Reuse the Core, validator, solver, and renderer.

Support creating a level, choosing an original model kit, placing/naming parts, placing screws, assigning colors/symbols, setting blockers, editing tray order, previewing allowed camera limits, playing the level, validating, solving, and exporting/importing versioned data. Show stable IDs and useful errors. Reordering assets must not rewrite IDs.

Make the authoring data the explicit source of truth and the JSON export a deterministic build artifact. Import/export round trips must preserve the puzzle definition. Show logic validation, solver outcome, content hash, witness freshness, and geometric checks separately. Mark content dirty after changes that invalidate its witness.

Add reachability checks against occluding geometry within the allowed camera range. At minimum, verify screw access at each staged state of a winning witness and at representative alternative structural states. Use the production picking rules. Camera sampling is practical evidence, not proof of universal visibility; report sampled poses, coverage, and remaining assumptions.

Flag screws that only appear tappable because the raycast ignores object geometry, and parts whose decorative release colliders block later interaction. Provide visual gizmos for blocked relationships and unreachable screws. Add witness playback for human inspection on phones.

Create a repeatable batch command that exports per-level structural, logical, geometric, and manual-review status. Reject release inclusion of invalid, stale, unsolved, or required-unreviewed content rather than silently shipping it.

Acceptance: create and export a new playable level entirely through the tool; change it, invalidate the witness, revalidate it, and replay the new solution. Document this exact workflow for a content creator.
```

## Prompt 10 — Ten-level prototype, then a reviewed campaign

```text
Act as the level designer and content engineer. Use the authoring tools and shared rules to produce real, original playable content. Do not generate a superficial list of level names.

First create ten handmade levels with a deliberate learning curve: simple matching, first buffer use, rotation, layered blockers, tray replacement planning, and manageable combinations. Ensure introduction levels are forgiving, visually clear, and fast to restart. Validate every level through the production pipeline and inspect witness playback.

After the prototype interaction passes its technical checks, plan a campaign targeting 100 levels across original object families. Vary structural choices, blocker patterns, active color pressure, and tray sequence. Reskinning an identical layout does not by itself produce meaningful puzzle variety. Keep individual objects affordable to author and render.

Provide per-level metadata: family, introduced skill, intended difficulty, structural layout ID, screw/color count, solution witness/hash, solver outcome, geometric coverage, and review status. Difficulty is a design hypothesis informed by branching and buffer pressure, not a guarantee derived from screw count.

Seeded generation may propose candidates from original templates. It may not bypass logical solving, geometry checks, or design review. Do not silently accept inconclusive solver results or inject ad-only capacity to make a broken level pass.

Use real playtest observations if supplied or collected with available participants. Otherwise create a short observation script and mark human usability evidence pending; do not invent feedback. Continue independent content/tooling work while distinguishing generated candidates from release-approved levels.

Acceptance: ten complete tutorial/prototype levels first, then a campaign manifest accounting for the 100-level target and exact acceptance status of each level. Release content must be playable assets with replayable solutions, not placeholders. Report unfinished or unreviewed counts honestly.
```

## Prompt 11 — Collection, campaign progression, and daily challenge

```text
Act as the progression engineer. Implement modest durable reasons to keep playing using the completed game's existing architecture. Preserve campaign access without accounts or internet.

On a committed first victory, unlock the next eligible campaign level and the relevant original object on the collection shelf. Display the assembled miniature even though the puzzle dismantled it. Use stable object IDs and a defined mapping when several levels share an object family. Give clear progress feedback without inventing a large currency economy.

Make collection and campaign unlocks idempotent across replay, duplicate callbacks, relaunch during victory, and application updates. Free replay/restart must not duplicate rewards or revoke ownership. A puzzle undo can never alter permanent progress.

Implement a daily challenge chosen deterministically from prevalidated bundled levels by UTC date and content version. Persist the first selected challenge for each UTC date, including its content reference. A same-day content update must not create another reward opportunity. Key daily reward fulfillment by UTC date, retaining the pinned challenge identity for replay. Finishing after midnight credits the date pinned when that attempt started. Handle clock rollback without crashing or regranting previously recorded rewards. Retain or explicitly migrate active content references. Explain that this local casual feature is not a trusted competitive economy.

Daily rewards, if present, are small non-purchased cosmetic progress. Do not add streak punishment, expiring paid benefits, notifications, multiplayer, or a backend unless the owner extends the scope.

Provide collection-empty, collection-complete, campaign-end, daily-completed, and interrupted-win UI states through the shared UI contracts.

Acceptance: test first win, replay win, duplicate completion, quit during completion, day rollover, clock rollback, and content migration. Verify collection rendering uses actual available assets. Record evidence and any unresolved design choice.
```

## Prompt 12 — Analytics, crash reporting, and safe remote settings

```text
Act as the diagnostics engineer. Implement Firebase Analytics and Crashlytics through the service contracts after checking the current official Unity SDK documentation and compatibility. Add Remote Config only with bundled safe defaults and a real need for configurable behavior.

Write docs/EVENT_SCHEMA.md before instrumenting: event purpose, parameters, trigger, deduplication key where needed, and privacy/consent behavior. Track tutorial progression, attempt start/win/fail/restart, eligible help use, ad lifecycle/reward/failure, purchase outcomes, and collection unlocks. Use level/content version and attempt identifiers consistently. Distinguish a resumed attempt from a new one. A loss may be followed by undo and victory in the same attempt: key recoverable loss transitions by attempt ID plus session revision, and record terminal attempt outcomes separately. Do not count each recoverable loss as a separate failed attempt. Verify a loss-to-undo-to-hint-to-win trace.

Do not assert every force-quit produces a reliable abandonment event. Define abandonment from observed sessions or an explicit later inference. Avoid double-counting auto-collected commerce/ad events. Do not send raw receipts, secrets, unnecessary personal identifiers, or full free-form device logs to analytics.

Use the composition root's explicit consent policy. Configure native automatic-collection defaults before SDK initialization, including collection that can occur before the first managed callback. Gate analytics independently from UMP ad status; do not equate platform tracking authorization with all consent. Implement denial, withdrawal, form errors, and offline startup behavior. Service failures and missing configuration must not block offline gameplay. Release configuration validation must distinguish an intentionally disabled integration from an accidental missing file.

For remote settings, validate types/ranges and retain last-known-good/bundled values. Version experiments; freeze relevant configuration for an active attempt. Never mutate an already-solved level's tray order or revoke purchased features through a remote flag. Ad kill switches must not disable ordinary gameplay.

Prepare real-device debug-event verification and a controlled development-only crash check. Configure symbol uploads for the actual native build pipeline and prevent intentional crash triggers in release UI.

Acceptance: event traces match actual user actions, network failure is harmless, opt-out behavior is verified, and symbolication evidence is recorded where access exists. Define D1/D7 cohort denominators and timezone in docs/MEASUREMENT.md; do not fabricate retention or revenue results.
```

## Prompt 13 — Consent and reliable rewarded ads

```text
Act as the mobile advertising engineer. Integrate the current Google Mobile Ads Unity plugin and UMP through the agreed adapters. Verify official SDK documentation; do not copy obsolete callback signatures. Use official test ad units and test-device settings during development.

Implement consent startup/update and applicable privacy-options UI under the shared composition root. Refresh UMP consent information each launch, display required forms, and use the current documented CanRequestAds gate without duplicating requests. Do not use a self-invented boolean as a substitute. Evaluate platform tracking authorization separately where relevant; a denial must leave the game playable. Coordinate native collection defaults and initialization with analytics and IAP behavior.

Offer rewarded ads only after an explicit player choice showing the exact reward: one additional usable hint or undo credit. Do not solicit a hint ad when no valid hint exists. Never display an ad in the middle of a move. Handle loading, unavailable/no-fill, failed-to-show, dismissed, app background/resume, and callbacks dispatched on the appropriate thread.

Create a unique operation ID and durable fulfillment record for each reward. Allow one active fullscreen ad operation, dispose one-use ad objects, and use bounded retries. Grant only after the documented earned-reward callback, never merely after opening or closing the ad. Duplicate callbacks must not duplicate help. Persist the received grant before the UI consumes it, and handle the original attempt being closed by crediting help safely without reviving a stale attempt. Revalidate any hint result against the current attempt/revision before applying it or consuming the earned credit. Undo/restart cannot roll the grant ledger back.

Be precise about interruption guarantees: a client can durably apply an earned callback it receives. It cannot prove completion if the process dies before receiving it. Document this limit. A stronger recovery promise requires verified server-side verification and trusted receipt handling; do not add a fake SSV endpoint or claim that capability exists.

Keep interstitials disabled by default. If later authorized, add capped natural-break placements and entitlement-aware suppression without changing this reward flow.

Acceptance: verify consent states, unavailable ads, cancellation/dismissal, earned rewards, duplicates, and lifecycle interruptions. Real-device test ads are required to verify the native integration. A mock adapter passing tests is not that evidence. Report precise console identifiers/configuration still needed.
```

## Prompt 14 — Store purchases, cosmetic ownership, and restoration

```text
Act as the mobile commerce engineer. Integrate a compatible released Unity IAP SDK using its current official APIs for Apple and Google. Implement one non-consumable cosmetic theme pack that has real assets and a usable selection control. Do not introduce paid currency or subscriptions.

Use a configurable development catalog and record the production product IDs/account setup still required. Retrieve localized titles/prices from the store. Product metadata unavailability must show an honest unavailable state, not a guessed price or a working-looking fake purchase button.

Implement fetching ownership, purchase initiation, deferred/pending states, successful fulfillment, cancellation/failure, restoration, duplicate delivery, reconnect, and transaction reconciliation. Interpret state names according to the installed SDK: a pending purchase awaiting durable fulfillment is not necessarily the same thing as deferred payment approval. Grant only on the appropriate verified paid/entitled state.

Choose and document a supported store verification method and its trust limits. Never embed private store credentials or equate a saved preference with authentic ownership. Persist validated entitlement and transaction fulfillment before store confirmation/acknowledgment. Redelivery must be safe. Handle refunded/revoked entitlements when the supported platform reconciliation exposes them; document offline cache behavior without claiming instantaneous revocation.

Provide an explicit restore control where appropriate and automatic reconciliation on supported platforms. Restore cannot fabricate consumable balances. Puzzle undo, restart, corrupt-save fallback, and profile migrations must not duplicate or invent paid ownership.

If an ad-removal product is added later, ensure interstitials actually exist and the purchase clearly describes its scope. A paid cosmetic theme must preserve screw colors/symbols and playability.

Acceptance: exercise the real Apple/Google test purchase environments when configured, including cancellation, deferred/pending behavior, duplicate delivery, restore after reinstall, and network interruptions. Distinguish editor simulation from store verification. Keep release credentials and signing material out of source and report remaining setup precisely.
```

## Prompt 15 — Native performance, compatibility, and reproducible builds

```text
Act as the native build and performance engineer. Integrate completed features and verify the exact source revision, dependency set, and configuration used for every build.

Profile representative complex levels, tray cascades, collection scenes, UI transitions, and ad return paths. Measure frame time, allocations, memory, startup, build size, and sustained play on an agreed older supported iPhone and a representative midrange Android device when available. A 60 fps goal is a target to measure, not a result to claim. Provide an intentional fallback quality/frame-rate policy where necessary.

Optimize demonstrated bottlenecks: material/draw-call count, unnecessary transparency/shadows, mesh complexity, transient allocations, excessive UI rebuilds, and leaked views/assets or ad objects. Pool when useful; do not blanket-rewrite working architecture or over-optimize without measurements.

Validate small/large aspect ratios, safe areas, different refresh rates, interruptions, device rotation policy, offline startup, and repeated play/restart cycles. Exercise AOT/code stripping and serialization on real native builds so editor-only success cannot hide missing types or callbacks.

Produce repeatable development and release build configurations for iOS and Android. Verify current official signing, SDK/OS/target requirements, plugin compatibility, privacy manifests and merged platform declarations for the actual included SDKs. Do not freeze submission requirements from this planning document.

Create or complete the build pipeline with secrets supplied externally. Never bypass a missing license or fabricate successful signing. Distinguish compilation, native link/archive, signed install, and distributable packaging. Record build IDs/hashes, source revision, versions, outputs, and symbol handling.

Acceptance: provide a performance report and native build matrix with measured results and blocked rows. Demonstrate clean-checkout reproducibility where possible. Do not broaden testing indefinitely after the concrete release risks are resolved.
```

## Prompt 16 — Independent end-to-end release audit and fixes

```text
Act as an independent game QA and release engineer. Read the shared specification and inspect the actual implementation; do not accept the implementation agent's completion statements as proof. Reuse valid existing evidence and reproduce critical behavior independently.

Audit the complete journey: first launch, consent behavior, tutorial, rotation/picking, buffered sorting, cascading tray replacements, win/loss, undo/restart, current-state hints, resume after force quit, campaign unlocks, collection, daily rollover, settings, English/French layouts, symbol/reduced-motion modes, cosmetic purchase/restore, and rewarded help.

Validate every release-included level's current hash and winning witness. Check geometry/accessibility of representative objects and record exactly which levels have human visual review versus only automated checks. Do not call all 100 levels manually tested unless that happened.

Investigate high-impact failure modes: through-object picking; duplicated taps; full-buffer false loss; initial-solution hints after divergence; stale content saves; corrupt saves; late writes; duplicate win processing; undo affecting reward/purchase ledgers; unavailable services blocking play; native-only SDK/link/stripping failures; and missing release configuration.

Verify removal of release-visible debug UI, fake service fulfillment, placeholder store identifiers, test crash controls, missing assets, and misleading prices. Test adapters may remain for explicit development configurations only.

Write docs/RELEASE_AUDIT.md with severity, reproduction, affected versions/platforms, evidence, fix owner, and retest result. Write a requirement-by-requirement matrix marked PASS, FAIL, or BLOCKED. Fix concrete blockers within your assigned ownership or return them to the responsible engineer; rerun the relevant verification.

Acceptance: an honest release recommendation with no hidden progression, fulfillment, or native-build failures. Missing hardware/account access is BLOCKED, not PASS. Confirm the final reviewed source revision and build artifacts; later code changes invalidate the affected evidence.
```

## Prompt 17 — Store package, release handover, and owner setup

```text
Act as the release producer. Use the implemented game and independent audit to prepare a concrete iOS-first release package and the corresponding Android package. Do not publish publicly as part of this default assignment.

Confirm the final release scope and actual feature availability. Ask for a public app name only if the owner has not provided one and it blocks final listing/signing identity; use the internal codename for development beforehand. Do not claim brand or trademark availability without checking appropriate current sources.

Prepare English/French store copy, accurate feature descriptions, support information, content/age-rating answers grounded in the actual game, and a privacy/data inventory based on the exact SDK versions and configuration. Verify current store requirements and metadata limits from official sources. Draft any required legal/support copy with unresolved owner facts flagged; do not invent contact details or assert legal compliance from a template.

Capture screenshots and short gameplay clips from the real release candidate. Use real mechanics, levels, prices, and UI. Promotional treatments may frame the game attractively but must not depict nonexistent gameplay. Produce a reusable capture plan, localized asset checklist, and source provenance record.

Prepare configured product/ad identifier checklists, signing and store-upload instructions, tester notes, review notes, offline behavior, restore instructions, and known limitations. Separate owner configuration from tasks already completed. Ensure privacy declarations, consent flow, SDK collection, and optional products agree.

Deliver source revision, dependency record, asset inventory, level validation manifest, native build outputs/status, release audit, build/recovery runbooks, and a short exact OWNER_SETUP checklist. Recommend a staged test release and monitoring plan. Do not invent an uploaded build or submission result.

Acceptance: the owner can see exactly what is ready, what was verified on each platform, and what remains before submission. Keep public release and acquisition spending pending a specific owner request, while finishing all concrete preparation that can be done now.
```

## Prompt 18 — Improve the game using real player evidence

```text
Act as the product and game-design analyst after a playable test or release. Use only actual playtest notes, event data, crash reports, and revenue/ad evidence supplied or available through authorized sources.

First inspect data coverage, build/content versions, consent-related gaps, sample size, cohort definitions, timezone, and duplicate events. Distinguish technical failures, misunderstood rules, poor difficulty, and normal challenge. Do not infer a whole market from a few reviews or claim causation from an uncontrolled comparison.

Evaluate tutorial completion, voluntary next-level play, per-level first/repeat completion, median solve time, buffer failure, hint/undo reliance, next-day/seventh-day return, crash-free play, and reward reliability. Keep measured values separate from targets and hypotheses. Do not invent benchmarks, users, revenue, or an A/B test result.

Propose the three highest-value changes with evidence, expected effect, uncertainty, smallest implementation, and a measurable acceptance/rollback condition. Favor readability, responsiveness, fair level progression, and useful content before adding more monetization surfaces. Do not make previously-solvable content depend on spending.

Implement authorized low-risk improvements in isolated changes, preserving level/save versions and entitlements. Use remote settings only within their established safe bounds. If a meaningful experiment requires more users or external spending, produce the exact test plan and mark that dependency.

If no real usage data exists, prepare an observation script and verify instrumentation instead of writing a fictional analysis. Acceptance: evidence-backed changes or an honest data-collection plan, with a concise decision record and reproducible calculations where applicable.
```

## Prompt 19 — Resume the project without starting over

```text
Resume Project Screw using Screw-Game-AI-Agent-Prompts.md, docs/IMPLEMENTATION_STATUS.md, NEXT_ACTION.md, repository instructions, and the current source.

Inspect uncommitted changes, recent commits, installed toolchain, actual files, and existing test/build evidence. Reconcile documentation with reality. Preserve the owner's and other agents' work. Do not regenerate the project or rewrite working modules merely because a previous session ended.

Identify the earliest unfinished dependency and the smallest next concrete action. If multiple agents are available, delegate independent bounded tasks with clear ownership; serialize shared Unity/package configuration. Continue implementation and verification rather than returning only a new plan.

Keep previously blocked gates blocked until the missing evidence is obtained. Reuse still-valid evidence; rerun only checks affected by code/configuration changes or unresolved risk. If old artifacts reference a different source revision or content hash, label them accordingly.

Finish the available work, update durable status and next actions, and report implemented, verified, device-verified, and blocked items separately. If genuine external setup is still required, name the exact tool/account/device/configuration without repeatedly asking broad questions.
```

## Prompt 20 — Reproduce and repair a specific defect

```text
Act as the debugging engineer for Project Screw. Read the prompt pack, relevant contracts, and current implementation. Use the bug report, failing test, log, screenshot, or recording supplied with this prompt. If essential reproduction information is absent, inspect available evidence first and ask only for the missing detail that determines the investigation.

Reproduce the defect on the affected build/platform where possible. Trace the state and operation IDs through input, Core, Session, Persistence, Presentation, and external service callbacks as relevant. Distinguish a logic failure from a visual desynchronization or native SDK issue.

Fix the root cause with the smallest coherent change. Preserve saved data and entitlement history. Do not hide failures by disabling tests, swallowing exceptions, weakening validation, bypassing consent, hardcoding a reward, or replacing a real integration with a mock.

Add a regression test or repeatable scenario that would have caught this defect, at the layer where the failure lives. Retest the affected flow and adjacent invariants; broaden only to resolve a concrete risk.

Report the cause, changed behavior, evidence on the affected platform, migration implications if any, and remaining uncertainty. Update the release matrix if prior evidence is invalidated. Do not call a native-only defect fixed solely because an editor test passes.
```

## Official references for implementation

The integration guidance below was checked while preparing this pack. Agents must recheck applicable versions and requirements when implementing and submitting.

- [Unity release support](https://unity.com/releases/unity-6/support)
- [Unity iOS build process](https://docs.unity3d.com/6000.3/Documentation/Manual/iphone-BuildProcess.html)
- [Unity IAP purchase states and retrieval](https://docs.unity.com/en-us/iap/purchases)
- [Google rewarded ads for Unity](https://developers.google.com/admob/unity/rewarded)
- [Google UMP for Unity](https://developers.google.com/admob/unity/privacy)
- [Firebase Unity setup](https://firebase.google.com/docs/unity/setup)

The prompts' architecture, stage boundaries, rule refinements, and acceptance criteria are proposed engineering decisions. They are not claims about how the reference games are implemented.
