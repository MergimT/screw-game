using System.Threading;
using ScrewGame.Contracts;
using ScrewGame.Core;

namespace ScrewGame.Validation
{
    /// <summary>Solves the current settled state. Pure computation; safe to run off the main thread on a cloned state.</summary>
    public static class HintService
    {
        public const int DefaultBudget = 50000;

        public static HintResult Compute(CompiledLevel level, PuzzleState settled, string attemptId, long revision, int budget = DefaultBudget, CancellationToken cancel = default)
        {
            var result = new HintResult { AttemptId = attemptId, Revision = revision, ContentHash = level.ContentHash };
            if (settled.Outcome == (int)Outcome.Won) { result.Status = HintStatus.Unsolvable; return result; }
            var solve = new Solver(level, budget, cancel).Solve(settled);
            result.StatesExplored = solve.StatesExplored;
            switch (solve.Outcome)
            {
                case SolveOutcome.Solved:
                    result.Status = HintStatus.Suggested;
                    result.ScrewId = solve.Witness[0];
                    break;
                case SolveOutcome.Unsolvable:
                    result.Status = HintStatus.Unsolvable;
                    break;
                default:
                    result.Status = HintStatus.Inconclusive;
                    break;
            }
            return result;
        }

        public static bool IsCurrent(HintResult hint, string attemptId, long revision, string contentHash)
        {
            return hint != null && hint.AttemptId == attemptId && hint.Revision == revision && hint.ContentHash == contentHash;
        }
    }
}
