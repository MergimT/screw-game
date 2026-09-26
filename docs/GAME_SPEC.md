# Game specification

Source of truth: "Shared product contract → Exact default rules" in `Screw-Game-AI-Agent-Prompts.md` (rules 1–15 and the resolver example), implemented verbatim in `Assets/ScrewGame/Core/Rules.cs`.

Defaults (`RulesDefaults`): 2 active trays, capacity 3, 5 buffer slots, initial tray queue ≥ 2.

Resolution order after a removal: route (lowest-index matching tray with room → lowest-index empty buffer slot → reject) → release parts with no screws → loop { complete full trays ascending; refill empty trays from queue ascending; transfer earliest-inserted fitting buffered screw to lowest-index matching tray } until no change → evaluate.

- Win: every screw collected and every part released.
- Loss: no structurally available screw has a destination and no automatic transfer remains. A full buffer alone is not a loss. Loss is recoverable (undo/restart) in the same attempt.
- Rejected command: no revision change, no event, no undo entry.
- Help: 1 free undo + 1 free useful hint per attempt; restarts unlimited; rewarded ad = +1 banked help credit. No paid consumables, no timers.
- Resolver example is encoded as the regression fixture `RulesTests.ResolverFixture_CascadesInInsertionOrder`.
