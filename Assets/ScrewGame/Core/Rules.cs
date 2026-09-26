using System.Collections.Generic;
using ScrewGame.Contracts;

namespace ScrewGame.Core
{
    /// <summary>Pure rule functions over (CompiledLevel, PuzzleState). No clocks, randomness, or engine types.</summary>
    public static class Rules
    {
        public static PuzzleState CreateInitial(CompiledLevel level)
        {
            int n = level.ScrewCount;
            var st = new PuzzleState
            {
                Place = new ScrewPlace[n],
                Slot = new int[n],
                Seq = new long[n],
                Released = new bool[level.PartCount],
                TrayColor = new int[level.TrayPositions],
                TrayCount = new int[level.TrayPositions],
                Buffer = new int[level.BufferSlots],
            };
            for (int s = 0; s < n; s++) { st.Slot[s] = -1; st.Seq[s] = -1; }
            for (int b = 0; b < level.BufferSlots; b++) st.Buffer[b] = -1;
            for (int t = 0; t < level.TrayPositions; t++)
            {
                if (st.QueueCursor < level.Queue.Length) st.TrayColor[t] = level.Queue[st.QueueCursor++];
                else st.TrayColor[t] = -1;
            }
            ReleaseEmptyParts(level, st, null);
            st.Outcome = (int)Evaluate(level, st);
            return st;
        }

        public static bool IsPartBlocked(CompiledLevel level, PuzzleState st, int part)
        {
            foreach (var b in level.PartBlockers[part])
                if (!st.Released[b]) return true;
            return false;
        }

        /// <summary>Structural availability: screw is attached and every blocker of its part is released. Camera visibility is a presentation concern.</summary>
        public static bool IsAvailable(CompiledLevel level, PuzzleState st, int screw)
        {
            return st.Place[screw] == ScrewPlace.OnPart && !IsPartBlocked(level, st, level.ScrewPart[screw]);
        }

        public static int FindTray(CompiledLevel level, PuzzleState st, int color)
        {
            for (int t = 0; t < st.TrayColor.Length; t++)
                if (st.TrayColor[t] == color && st.TrayCount[t] < level.TrayCapacity) return t;
            return -1;
        }

        public static int FindEmptyBuffer(PuzzleState st)
        {
            for (int b = 0; b < st.Buffer.Length; b++)
                if (st.Buffer[b] < 0) return b;
            return -1;
        }

        public static bool HasDestination(CompiledLevel level, PuzzleState st, int screw)
        {
            return FindTray(level, st, level.ScrewColor[screw]) >= 0 || FindEmptyBuffer(st) >= 0;
        }

        public static bool IsLegal(CompiledLevel level, PuzzleState st, int screw)
        {
            return st.Outcome != (int)Outcome.Won && IsAvailable(level, st, screw) && HasDestination(level, st, screw);
        }

        public static void LegalMoves(CompiledLevel level, PuzzleState st, List<int> into)
        {
            into.Clear();
            if (st.Outcome == (int)Outcome.Won) return;
            for (int s = 0; s < level.ScrewCount; s++)
                if (IsLegal(level, st, s)) into.Add(s);
        }

        /// <summary>Validates without mutating. Returns None when the removal is legal.</summary>
        public static RejectReason Check(CompiledLevel level, PuzzleState st, int screw)
        {
            if (screw < 0 || screw >= level.ScrewCount) return RejectReason.UnknownScrew;
            if (st.Outcome == (int)Outcome.Won) return RejectReason.AttemptClosed;
            if (st.Place[screw] != ScrewPlace.OnPart) return RejectReason.NotOnObject;
            if (IsPartBlocked(level, st, level.ScrewPart[screw])) return RejectReason.Blocked;
            if (!HasDestination(level, st, screw)) return RejectReason.NoDestination;
            return RejectReason.None;
        }

        /// <summary>Applies a legal removal and stabilizes. Caller must ensure Check returned None. Events may be null.</summary>
        public static void ApplyRemoval(CompiledLevel level, PuzzleState st, int screw, List<VisualEvent> events)
        {
            int color = level.ScrewColor[screw];
            int tray = FindTray(level, st, color);
            if (tray >= 0)
            {
                st.Place[screw] = ScrewPlace.Tray;
                st.Slot[screw] = tray;
                st.TrayCount[tray]++;
                events?.Add(new ScrewRemovedEvent { ScrewId = level.ScrewIds[screw], Destination = DestinationKind.Tray, DestinationIndex = tray });
            }
            else
            {
                int slot = FindEmptyBuffer(st);
                st.Place[screw] = ScrewPlace.Buffer;
                st.Slot[screw] = slot;
                st.Seq[screw] = st.NextSeq++;
                st.Buffer[slot] = screw;
                events?.Add(new ScrewRemovedEvent { ScrewId = level.ScrewIds[screw], Destination = DestinationKind.Buffer, DestinationIndex = slot });
            }

            ReleaseEmptyParts(level, st, events);
            Stabilize(level, st, events);

            var before = (Outcome)st.Outcome;
            var after = Evaluate(level, st);
            st.Outcome = (int)after;
            if (after != before) events?.Add(new OutcomeChangedEvent { Outcome = after });
        }

        private static void ReleaseEmptyParts(CompiledLevel level, PuzzleState st, List<VisualEvent> events)
        {
            for (int p = 0; p < level.PartCount; p++)
            {
                if (st.Released[p]) continue;
                bool attached = false;
                foreach (var s in level.PartScrews[p])
                    if (st.Place[s] == ScrewPlace.OnPart) { attached = true; break; }
                if (attached) continue;
                st.Released[p] = true;
                events?.Add(new PartReleasedEvent { PartId = level.PartIds[p] });
            }
        }

        /// <summary>Repeats collection (ascending tray index), refill (ascending), then one insertion-order transfer, until nothing changes.</summary>
        public static void Stabilize(CompiledLevel level, PuzzleState st, List<VisualEvent> events)
        {
            while (true)
            {
                bool changed = false;
                for (int t = 0; t < st.TrayColor.Length; t++)
                {
                    if (st.TrayColor[t] < 0 || st.TrayCount[t] < level.TrayCapacity) continue;
                    int color = st.TrayColor[t];
                    for (int s = 0; s < level.ScrewCount; s++)
                    {
                        if (st.Place[s] == ScrewPlace.Tray && st.Slot[s] == t)
                        {
                            st.Place[s] = ScrewPlace.Collected;
                            st.Slot[s] = -1;
                        }
                    }
                    st.TrayColor[t] = -1;
                    st.TrayCount[t] = 0;
                    st.CompletedTrays++;
                    events?.Add(new TrayCompletedEvent { TrayIndex = t, Color = color });
                    changed = true;
                }

                for (int t = 0; t < st.TrayColor.Length; t++)
                {
                    if (st.TrayColor[t] >= 0 || st.QueueCursor >= level.Queue.Length) continue;
                    st.TrayColor[t] = level.Queue[st.QueueCursor++];
                    events?.Add(new TrayRefilledEvent { TrayIndex = t, Color = st.TrayColor[t] });
                    changed = true;
                }

                int best = -1;
                long bestSeq = long.MaxValue;
                for (int b = 0; b < st.Buffer.Length; b++)
                {
                    int s = st.Buffer[b];
                    if (s < 0 || st.Seq[s] >= bestSeq) continue;
                    if (FindTray(level, st, level.ScrewColor[s]) < 0) continue;
                    best = s;
                    bestSeq = st.Seq[s];
                }
                if (best >= 0)
                {
                    int from = st.Slot[best];
                    int to = FindTray(level, st, level.ScrewColor[best]);
                    st.Buffer[from] = -1;
                    st.Place[best] = ScrewPlace.Tray;
                    st.Slot[best] = to;
                    st.Seq[best] = -1;
                    st.TrayCount[to]++;
                    events?.Add(new BufferTransferredEvent { ScrewId = level.ScrewIds[best], FromSlot = from, ToTray = to });
                    changed = true;
                }

                if (!changed) return;
            }
        }

        public static Outcome Evaluate(CompiledLevel level, PuzzleState st)
        {
            bool all = true;
            for (int s = 0; s < level.ScrewCount; s++)
                if (st.Place[s] != ScrewPlace.Collected) { all = false; break; }
            if (all)
            {
                bool released = true;
                foreach (var r in st.Released) if (!r) { released = false; break; }
                if (released) return Outcome.Won;
            }
            for (int s = 0; s < level.ScrewCount; s++)
                if (IsAvailable(level, st, s) && HasDestination(level, st, s)) return Outcome.Playing;
            return Outcome.Lost;
        }

        /// <summary>Checks conservation invariants. Returns null when valid, otherwise a description of the first violation.</summary>
        public static string CheckInvariants(CompiledLevel level, PuzzleState st)
        {
            var trayCounts = new int[st.TrayColor.Length];
            int collected = 0;
            for (int s = 0; s < level.ScrewCount; s++)
            {
                switch (st.Place[s])
                {
                    case ScrewPlace.OnPart:
                        if (st.Released[level.ScrewPart[s]]) return "released part " + level.PartIds[level.ScrewPart[s]] + " holds screw " + level.ScrewIds[s];
                        if (st.Slot[s] != -1) return "attached screw has slot " + level.ScrewIds[s];
                        break;
                    case ScrewPlace.Buffer:
                        if (st.Slot[s] < 0 || st.Slot[s] >= st.Buffer.Length || st.Buffer[st.Slot[s]] != s) return "buffer mismatch " + level.ScrewIds[s];
                        if (st.Seq[s] < 0 || st.Seq[s] >= st.NextSeq) return "bad seq " + level.ScrewIds[s];
                        break;
                    case ScrewPlace.Tray:
                        if (st.Slot[s] < 0 || st.Slot[s] >= st.TrayColor.Length) return "bad tray " + level.ScrewIds[s];
                        if (st.TrayColor[st.Slot[s]] != level.ScrewColor[s]) return "color mismatch " + level.ScrewIds[s];
                        trayCounts[st.Slot[s]]++;
                        break;
                    case ScrewPlace.Collected:
                        collected++;
                        break;
                }
            }
            for (int b = 0; b < st.Buffer.Length; b++)
            {
                int s = st.Buffer[b];
                if (s >= 0 && (st.Place[s] != ScrewPlace.Buffer || st.Slot[s] != b)) return "buffer slot " + b + " points to misplaced screw";
            }
            for (int t = 0; t < trayCounts.Length; t++)
            {
                if (trayCounts[t] != st.TrayCount[t]) return "tray count mismatch " + t;
                if (st.TrayCount[t] > level.TrayCapacity) return "tray over capacity " + t;
                if (st.TrayColor[t] < 0 && st.TrayCount[t] != 0) return "inactive tray holds screws " + t;
            }
            if (collected != st.CompletedTrays * level.TrayCapacity) return "collected count " + collected + " != " + st.CompletedTrays + "*" + level.TrayCapacity;
            return null;
        }
    }
}
