using System.Linq;
using NUnit.Framework;
using ScrewGame.Contracts;
using ScrewGame.Core;
using ScrewGame.Validation;
using static ScrewGame.Tests.TestLevels;

namespace ScrewGame.Tests
{
    public class SolverTests
    {
        [Test]
        public void BundledLevels_ValidateSolveAndReplay([ValueSource(nameof(Ids))] string id)
        {
            var def = SampleContent.Load(id);
            var report = LevelValidator.Validate(def);
            Assert.IsTrue(report.IsValid, string.Join("; ", report.Errors));
            var level = CompiledLevel.Compile(def);
            var result = new Solver(level).Solve();
            Assert.AreEqual(SolveOutcome.Solved, result.Outcome);
            Assert.AreEqual(level.ScrewCount, result.Witness.Count);
            Assert.IsTrue(Solver.ReplayWins(level, result.Witness));
        }

        public static string[] Ids() => SampleContent.AllIds();

        [Test]
        public void Solver_IsDeterministic()
        {
            var level = CompiledLevel.Compile(SampleContent.Load("L10"));
            var a = new Solver(level).Solve();
            var b = new Solver(level).Solve();
            CollectionAssert.AreEqual(a.Witness, b.Witness);
            Assert.AreEqual(a.StatesExplored, b.StatesExplored);
        }

        [Test]
        public void Unsolvable_OnlyAfterExhaustiveSearch()
        {
            var level = CompiledLevel.Compile(Build("dead", new[] { A, B, C, D },
                P("p0", new[] { C, C, C, D, D, D }), P("p1", new[] { A, A, A, B, B, B }, "p0")));
            var r = new Solver(level).Solve();
            Assert.AreEqual(SolveOutcome.Unsolvable, r.Outcome);
            Assert.IsEmpty(r.Witness);
        }

        [Test]
        public void TinyBudget_IsInconclusive_NotUnsolvable()
        {
            var level = CompiledLevel.Compile(Build("dead", new[] { A, B, C, D },
                P("p0", new[] { C, C, C, D, D, D }), P("p1", new[] { A, A, A, B, B, B }, "p0")));
            var r = new Solver(level, budget: 2).Solve();
            Assert.AreEqual(SolveOutcome.Inconclusive, r.Outcome);
        }

        [Test]
        public void Cancellation_ReportsCancelled()
        {
            var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel();
            var r = new Solver(CompiledLevel.Compile(SampleContent.Load("L10")), cancel: cts.Token).Solve();
            Assert.AreEqual(SolveOutcome.Cancelled, r.Outcome);
        }

        [Test]
        public void TrappedAndSolvableAlternatives()
        {
            // Buffering both D first fills buffer with C,C,C? Only 5 slots: taking all six C/D is a trap, taking C/D after A/B is fine.
            var level = CompiledLevel.Compile(Build("alt", new[] { A, B, C, D },
                P("p0", new[] { C, C, C, D, D, D }), P("p1", new[] { A, A, A, B, B, B })));
            var trapped = Rules.CreateInitial(level);
            foreach (var s in new[] { 0, 1, 2, 3, 4 }) Rules.ApplyRemoval(level, trapped, s, null);
            Assert.AreEqual(Outcome.Playing, (Outcome)trapped.Outcome);
            Assert.AreEqual(SolveOutcome.Solved, new Solver(level).Solve(trapped).Outcome, "A/B still route directly and free C/D trays");

            var tight = CompiledLevel.Compile(Build("tight", new[] { A, B, C, D, A },
                P("p0", new[] { C, C, C, D, D, D }), P("p1", new[] { A, A, A, B, B, B }), P("p2", new[] { A, A, A }, "p0")));
            var st = Rules.CreateInitial(tight);
            foreach (var s in new[] { 6, 7, 8, 9, 10, 11 }) Rules.ApplyRemoval(tight, st, s, null);
            Assert.AreEqual(SolveOutcome.Solved, new Solver(tight).Solve(st).Outcome);
        }

        [Test]
        public void Hint_UsesCurrentState_AndIsBoundToContext()
        {
            var level = CompiledLevel.Compile(SampleContent.Load("L04"));
            var e = new PuzzleEngine(level);
            var h0 = HintService.Compute(level, e.State, "att1", e.Revision);
            Assert.AreEqual(HintStatus.Suggested, h0.Status);
            Assert.AreEqual(RejectReason.None, e.Check(h0.ScrewId));
            Assert.IsTrue(e.Remove(h0.ScrewId, e.Revision).Accepted);
            Assert.IsFalse(HintService.IsCurrent(h0, "att1", e.Revision, level.ContentHash), "stale after move");
            var h1 = HintService.Compute(level, e.State, "att1", e.Revision);
            Assert.IsTrue(HintService.IsCurrent(h1, "att1", e.Revision, level.ContentHash));
            Assert.IsFalse(HintService.IsCurrent(h1, "att2", e.Revision, level.ContentHash), "stale after restart");
        }

        [Test]
        public void Hint_OnDeadState_IsUnsolvable()
        {
            var level = CompiledLevel.Compile(Build("dead", new[] { A, B, C, D },
                P("p0", new[] { C, C, C, D, D, D }), P("p1", new[] { A, A, A, B, B, B }, "p0")));
            var st = Rules.CreateInitial(level);
            var h = HintService.Compute(level, st, "a", 0);
            Assert.AreEqual(HintStatus.Unsolvable, h.Status);
            Assert.IsNull(h.ScrewId);
        }

        [Test]
        public void CanonicalKey_DistinguishesBufferOrder()
        {
            var level = CompiledLevel.Compile(Build("k", new[] { A, B, C, D },
                P("p0", new[] { C, D, C, D, C, D }), P("p1", new[] { A, A, A, B, B, B })));
            var x = Rules.CreateInitial(level);
            Rules.ApplyRemoval(level, x, 0, null); Rules.ApplyRemoval(level, x, 1, null);
            var y = Rules.CreateInitial(level);
            Rules.ApplyRemoval(level, y, 1, null); Rules.ApplyRemoval(level, y, 0, null);
            Assert.AreNotEqual(CanonicalKey.Of(level, x), CanonicalKey.Of(level, y));
        }

        [Test]
        public void Validator_ReportsStructuralErrors()
        {
            var d = Build("bad", new[] { A, B }, P("p0", new[] { A, A, A, B, B }, "p1"), P("p1", new int[0], "p0"));
            d.Screws.Add(new ScrewDefinition { Id = "s0", PartId = "ghost", Color = 9 });
            var errors = string.Join("\n", LevelValidator.Validate(d).Errors);
            StringAssert.Contains("duplicate screw id s0", errors);
            StringAssert.Contains("unknown part ghost", errors);
            StringAssert.Contains("color out of range", errors);
            StringAssert.Contains("part p1 has no securing screws", errors);
            StringAssert.Contains("color 1: 2 screws", errors);
        }

        [Test]
        public void Validator_DetectsCycle()
        {
            var d = Build("cyc", new[] { A, B }, P("p0", new[] { A, A, A }, "p1"), P("p1", new[] { B, B, B }, "p0"));
            Assert.IsTrue(LevelValidator.Validate(d).Errors.Any(e => e.Contains("cycle")));
        }

        [Test]
        public void ContentHash_ChangesWithRelevantEdits()
        {
            var a = SampleContent.Load("L02");
            var h = LevelHasher.Compute(a);
            Assert.AreEqual(h, LevelHasher.Compute(SampleContent.Load("L02")));
            a.Screws[0].Color = 1;
            Assert.AreNotEqual(h, LevelHasher.Compute(a));
        }
    }
}
