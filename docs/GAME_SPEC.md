# Game specification (v1, RulesVersion 1)

Source: "Shared product contract → Exact default rules" in `Screw-Game-AI-Agent-Prompts.md`. Reference implementation: `Assets/ScrewGame/Core/Rules.cs` (engine-free). If this file and the pack ever disagree, the pack wins and this file is a bug.

## Board and defaults (`RulesDefaults`)

| Item | Default | Owner |
| --- | --- | --- |
| Active tray positions | 2 | level data (`TrayPositions`) |
| Tray capacity | 3 | level data (`TrayCapacity`) |
| Buffer slots | 5 | level data (`BufferSlots`) |
| Ordered tray-color queue | ≥ 2 entries; first 2 fill positions 0 and 1, the rest are replacements in order | level data (`TrayQueue`) |

Capacities are level data. They are never remote-configured or changed at runtime.

## Rules

1. **Structure.** Each screw has a stable ID and color and belongs to exactly one part. A part has ≥ 1 screw and a blocker list. A screw is *structurally available* when it is still attached and every blocker of its part has been released.
2. **Visibility.** A tap additionally requires the screw to be visible and hit from the current camera. Camera orientation never makes a move legal or illegal for loss evaluation. Authoring must keep every structurally available screw reachable from some permitted viewpoint (Prompt 09 gate).
3. **Routing.** The screw goes to the lowest-index active tray whose color matches and has room. Otherwise it goes to the lowest-index empty buffer slot. Otherwise the command is **rejected with no state change**.
4. **Buffer order.** Slots keep their index. Every insertion gets a monotonic sequence number (`NextSeq`). Automatic transfers use insertion order, not slot index.
5. **Commit.** The removal is committed logically. Then every part with no attached screws is released (in part index order). Presentation events are produced for both.
6. **Stabilization loop.** Repeat until nothing changes:
   1. complete every full tray in ascending tray index;
   2. refill every empty position in ascending index from the queue (a position stays inactive, color −1, when the queue is exhausted);
   3. pick the earliest-inserted buffered screw that fits any active tray, and move that single screw to its lowest-index matching tray.
7. **Win.** After stabilization, every screw is in a completed tray and every part is released. An empty object with screws still in the buffer is **not** a win.
8. **Loss (stuck).** Not won, no structurally available screw has a legal destination, and no automatic transfer remains. A full buffer alone is not a loss while a direct tray move exists. Loss is **recoverable** in the same attempt through undo or restart.
9. **Rejected command.** No revision increment, inventory change, success sound or haptic, or undo entry. Animation and physics never decide logical outcomes.
10. **Restart.** Restores the authored level and queue and creates a **new attempt ID**. It is free and unlimited, and it resets the per-attempt free help.
11. **Undo.** Restores the previous puzzle payload (trays, queue position, buffer slots and sequence numbers, released parts, outcome). It then assigns a **fresh, higher** session revision; revisions are never rewound. Undo is allowed after loss and rejected after victory (the attempt is closed).
12. **Help policy.** Each attempt gets 1 free undo and 1 free *successful* hint. A rewarded ad grants +1 banked credit, usable for either undo or hint. There is no paid consumable currency, no timer and no hidden paid difficulty. Pause, relaunch and resume do not reset the free allocation, because it is stored in the attempt record.
13. **Undo scope.** Undo snapshots contain puzzle payload only. They exclude entitlements, reward and purchase ledgers, help balances, settings and progression. Spent help is never refunded by undo.
14. **Campaign guarantee.** Every campaign level has a verified solver witness under its normal capacities, with no help, ads or purchases.
15. **Hints.** A hint is bound to `(AttemptId, Revision, ContentHash)`. A result that no longer matches is `Stale` and is discarded. A credit is consumed only when a current `Suggested` hint is presented.

## Hint results (`HintStatus`)

| Status | Meaning | Credit consumed |
| --- | --- | --- |
| `Suggested` | `ScrewId` is the first move of a verified winning line from the current state | yes, when presented while current |
| `Unsolvable` | exhaustive search proves no win exists from the current state (undo/restart advised) | no |
| `Inconclusive` | the search budget ran out before a proof either way | no |
| `Stale` | the binding no longer matches the session | no |

## Terminal states

| Situation | `PuzzleState` outcome | `AttemptRecord.Terminal` | Attempt |
| --- | --- | --- | --- |
| playing | `Playing` | `Open` | open |
| stuck | `Lost` | `Open` | open; undo/restart allowed |
| victory committed | `Won` | `Won` | closed; undo rejected with `AttemptClosed` |
| content/rules changed under a saved attempt | n/a | `Replaced` | a new attempt starts |

## Resolver example (regression fixture)

Before the tap: active trays are A(2/3) and B(2/3), the remaining queue is [C, A], and the buffer by insertion order is C, A, B.

1. Tap an available A. It goes to tray 0, which is now full.
2. Tray 0 completes and refills with C.
3. The earliest fitting buffered screw is C, which moves to tray 0 (C 1/3).
4. Buffer A doesn't fit any tray. The next is B, which moves to tray 1 and fills it.
5. Tray 1 completes and refills with A.
6. Buffer A moves to tray 1 (A 1/3).
7. Result: trays C(1) and A(1), empty buffer, two newly completed trays.

This is encoded as `RulesTests.ResolverFixture_CascadesInInsertionOrder`. The test also asserts the exact event order.
