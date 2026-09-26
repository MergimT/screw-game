using System;
using System.Collections.Generic;
using System.Threading;
using ScrewGame.Contracts;
using ScrewGame.Core;

namespace ScrewGame.Validation
{
    public sealed class SolveResult
    {
        public SolveOutcome Outcome;
        public List<string> Witness = new List<string>();
        public int StatesExplored;
        public string ContentHash;
        public int RulesVersion = RulesDefaults.RulesVersion;
        public int SolverVersion = Solver.Version;
    }

    /// <summary>Deterministic bounded depth-first search over the production rules with dead-state memoization.</summary>
    public sealed class Solver
    {
        public const int Version = 1;
        public const int DefaultBudget = 200000;

        private readonly CompiledLevel _level;
        private readonly int _budget;
        private readonly CancellationToken _cancel;
        private readonly Action<int> _progress;
        private readonly HashSet<string> _dead = new HashSet<string>(StringComparer.Ordinal);
        private int _explored;
        private bool _exhausted;
        private bool _cancelled;

        public Solver(CompiledLevel level, int budget = DefaultBudget, CancellationToken cancel = default, Action<int> progress = null)
        {
            _level = level;
            _budget = budget;
            _cancel = cancel;
            _progress = progress;
        }

        public SolveResult Solve()
        {
            return Solve(Rules.CreateInitial(_level));
        }

        public SolveResult Solve(PuzzleState start)
        {
            _dead.Clear();
            _explored = 0;
            _exhausted = false;
            _cancelled = false;
            var result = new SolveResult { ContentHash = _level.ContentHash };
            var path = new List<int>();
            bool found = Search(start.Clone(), path);
            result.StatesExplored = _explored;
            if (found)
            {
                result.Outcome = SolveOutcome.Solved;
                foreach (var s in path) result.Witness.Add(_level.ScrewIds[s]);
            }
            else if (_cancelled) result.Outcome = SolveOutcome.Cancelled;
            else if (_exhausted) result.Outcome = SolveOutcome.Inconclusive;
            else result.Outcome = SolveOutcome.Unsolvable;
            return result;
        }

        private bool Search(PuzzleState st, List<int> path)
        {
            if (st.Outcome == (int)Outcome.Won) return true;
            if (st.Outcome == (int)Outcome.Lost) return false;
            if (_cancel.IsCancellationRequested) { _cancelled = true; return false; }
            if (_explored >= _budget) { _exhausted = true; return false; }
            var key = CanonicalKey.Of(_level, st);
            if (_dead.Contains(key)) return false;
            _explored++;
            if (_progress != null && (_explored & 1023) == 0) _progress(_explored);

            var moves = OrderedMoves(st);
            foreach (var m in moves)
            {
                var next = st.Clone();
                Rules.ApplyRemoval(_level, next, m, null);
                path.Add(m);
                if (Search(next, path)) return true;
                path.RemoveAt(path.Count - 1);
                if (_exhausted || _cancelled) return false;
            }
            _dead.Add(key);
            return false;
        }

        /// <summary>
        /// Direct tray matches first, then buffer moves; each group in ascending screw index. Screws with the same part and color
        /// lead to logically identical successors (blocking is per part, routing per color), so only the first is expanded.
        /// </summary>
        private List<int> OrderedMoves(PuzzleState st)
        {
            var direct = new List<int>();
            var buffered = new List<int>();
            var seen = new HashSet<long>();
            for (int s = 0; s < _level.ScrewCount; s++)
            {
                if (!Rules.IsLegal(_level, st, s)) continue;
                if (!seen.Add(((long)_level.ScrewPart[s] << 8) | (uint)_level.ScrewColor[s])) continue;
                if (Rules.FindTray(_level, st, _level.ScrewColor[s]) >= 0) direct.Add(s);
                else buffered.Add(s);
            }
            direct.AddRange(buffered);
            return direct;
        }

        /// <summary>Replays a witness through the production rules and reports whether it ends in victory with valid invariants.</summary>
        public static bool ReplayWins(CompiledLevel level, IList<string> witness, PuzzleState start = null)
        {
            var st = (start ?? Rules.CreateInitial(level)).Clone();
            foreach (var id in witness)
            {
                if (!level.TryGetScrew(id, out var s)) return false;
                if (Rules.Check(level, st, s) != RejectReason.None) return false;
                Rules.ApplyRemoval(level, st, s, null);
                if (Rules.CheckInvariants(level, st) != null) return false;
            }
            return st.Outcome == (int)Outcome.Won;
        }
    }
}
