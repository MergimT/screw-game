using System.Collections.Generic;
using ScrewGame.Contracts;

namespace ScrewGame.Core
{
    /// <summary>Stateful wrapper around Rules: owns the current payload, monotonic revision and undo history for one attempt.</summary>
    public sealed class PuzzleEngine
    {
        private readonly List<PuzzleState> _history = new List<PuzzleState>();
        private readonly List<VisualEvent> _scratch = new List<VisualEvent>();

        public CompiledLevel Level { get; }
        public PuzzleState State { get; private set; }
        public long Revision { get; private set; }
        public Outcome Outcome => (Outcome)State.Outcome;
        public int UndoDepth => _history.Count;
        public IReadOnlyList<PuzzleState> History => _history;

        public PuzzleEngine(CompiledLevel level)
        {
            Level = level;
            State = Rules.CreateInitial(level);
        }

        /// <summary>Restores a saved attempt. The state and history are validated; returns null if they are inconsistent with the level.</summary>
        public static PuzzleEngine Restore(CompiledLevel level, PuzzleState state, long revision, IEnumerable<PuzzleState> history)
        {
            var engine = new PuzzleEngine(level);
            if (!Fits(level, state) || Rules.CheckInvariants(level, state) != null) return null;
            if (history != null)
            {
                foreach (var h in history)
                {
                    if (!Fits(level, h) || Rules.CheckInvariants(level, h) != null) return null;
                    engine._history.Add(h.Clone());
                }
            }
            engine.State = state.Clone();
            engine.Revision = revision;
            return engine;
        }

        private static bool Fits(CompiledLevel level, PuzzleState s)
        {
            return s != null && s.Place != null && s.Place.Length == level.ScrewCount && s.Slot != null && s.Slot.Length == level.ScrewCount
                && s.Seq != null && s.Seq.Length == level.ScrewCount && s.Released != null && s.Released.Length == level.PartCount
                && s.TrayColor != null && s.TrayColor.Length == level.TrayPositions && s.TrayCount != null && s.TrayCount.Length == level.TrayPositions
                && s.Buffer != null && s.Buffer.Length == level.BufferSlots;
        }

        public RejectReason Check(string screwId)
        {
            if (!Level.TryGetScrew(screwId, out var s)) return RejectReason.UnknownScrew;
            return Rules.Check(Level, State, s);
        }

        public bool IsAvailable(string screwId)
        {
            return Level.TryGetScrew(screwId, out var s) && Rules.IsAvailable(Level, State, s);
        }

        public CommandResult Remove(string screwId, long expectedRevision)
        {
            if (expectedRevision != Revision) return CommandResult.Reject(RejectReason.StaleRevision, Revision);
            if (!Level.TryGetScrew(screwId, out var s)) return CommandResult.Reject(RejectReason.UnknownScrew, Revision);
            var reason = Rules.Check(Level, State, s);
            if (reason != RejectReason.None) return CommandResult.Reject(reason, Revision);

            var previous = State.Clone();
            _scratch.Clear();
            Rules.ApplyRemoval(Level, State, s, _scratch);
            _history.Add(previous);
            Revision++;
            return CommandResult.Accept(Revision, _scratch.ToArray());
        }

        public CommandResult Undo(long expectedRevision)
        {
            if (expectedRevision != Revision) return CommandResult.Reject(RejectReason.StaleRevision, Revision);
            if (Outcome == Outcome.Won) return CommandResult.Reject(RejectReason.AttemptClosed, Revision);
            if (_history.Count == 0) return CommandResult.Reject(RejectReason.NothingToUndo, Revision);
            State = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            Revision++;
            return CommandResult.Accept(Revision, new VisualEvent[] { new StateReplacedEvent() });
        }

        /// <summary>Restores the authored initial state. The session assigns the new attempt ID.</summary>
        public CommandResult Restart()
        {
            State = Rules.CreateInitial(Level);
            _history.Clear();
            Revision++;
            return CommandResult.Accept(Revision, new VisualEvent[] { new StateReplacedEvent() });
        }

        /// <summary>Rolls back to a previously captured payload and history without touching the revision counter semantics; used when a durable commit fails.</summary>
        public void RollBack(PuzzleState state, long revision, List<PuzzleState> history)
        {
            State = state;
            Revision = revision;
            _history.Clear();
            _history.AddRange(history);
        }

        public List<PuzzleState> CopyHistory()
        {
            return new List<PuzzleState>(_history);
        }
    }
}
