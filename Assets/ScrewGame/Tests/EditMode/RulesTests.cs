using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ScrewGame.Contracts;
using ScrewGame.Core;
using static ScrewGame.Tests.TestLevels;

namespace ScrewGame.Tests
{
    public class RulesTests
    {
        private static CompiledLevel FixtureLevel() => CompiledLevel.Compile(Build("fixture", new[] { A, B, C, A },
            P("p0", new[] { A, A, B, B, C, A, B }),
            P("p1", new[] { A }),
            P("p2", new[] { A, A, C, C }, "p1")));

        /// <summary>Pack resolver fixture: trays A2/B2, queue C then A, buffer C,A,B; tapping A cascades to C1/A1 with an empty buffer.</summary>
        [Test]
        public void ResolverFixture_CascadesInInsertionOrder()
        {
            var level = FixtureLevel();
            var st = Rules.CreateInitial(level);
            st.Released[0] = true;
            void Put(int s, ScrewPlace place, int slot, long seq) { st.Place[s] = place; st.Slot[s] = slot; st.Seq[s] = seq; }
            Put(0, ScrewPlace.Tray, 0, -1); Put(1, ScrewPlace.Tray, 0, -1);
            Put(2, ScrewPlace.Tray, 1, -1); Put(3, ScrewPlace.Tray, 1, -1);
            Put(4, ScrewPlace.Buffer, 0, 0); Put(5, ScrewPlace.Buffer, 1, 1); Put(6, ScrewPlace.Buffer, 2, 2);
            st.TrayCount = new[] { 2, 2 };
            st.Buffer = new[] { 4, 5, 6, -1, -1 };
            st.NextSeq = 3;
            Assert.IsNull(Rules.CheckInvariants(level, st));
            var engine = PuzzleEngine.Restore(level, st, 7, null);
            Assert.NotNull(engine);

            var r = engine.Remove("s7", 7);

            Assert.IsTrue(r.Accepted, r.Reason.ToString());
            var s = engine.State;
            CollectionAssert.AreEqual(new[] { C, A }, s.TrayColor);
            CollectionAssert.AreEqual(new[] { 1, 1 }, s.TrayCount);
            Assert.IsTrue(s.Buffer.All(b => b < 0));
            Assert.AreEqual(2, s.CompletedTrays);
            Assert.AreEqual(4, s.QueueCursor);
            Assert.AreEqual(8, engine.Revision);
            var trace = string.Join(" ", r.Events.Select(Describe));
            Assert.AreEqual("remove:s7>T0 release:p1 complete:0 refill:0=2 xfer:s4>0 xfer:s6>1 complete:1 refill:1=0 xfer:s5>1", trace);
            Assert.IsNull(Rules.CheckInvariants(level, s));
        }

        internal static string Describe(VisualEvent e)
        {
            switch (e)
            {
                case ScrewRemovedEvent x: return "remove:" + x.ScrewId + ">" + (x.Destination == DestinationKind.Tray ? "T" : "B") + x.DestinationIndex;
                case PartReleasedEvent x: return "release:" + x.PartId;
                case TrayCompletedEvent x: return "complete:" + x.TrayIndex;
                case TrayRefilledEvent x: return "refill:" + x.TrayIndex + "=" + x.Color;
                case BufferTransferredEvent x: return "xfer:" + x.ScrewId + ">" + x.ToTray;
                case OutcomeChangedEvent x: return "outcome:" + x.Outcome;
                default: return e.GetType().Name;
            }
        }

        [Test]
        public void RejectedCommands_DoNotChangeStateOrRevision()
        {
            var engine = new PuzzleEngine(CompiledLevel.Compile(Chain()));
            var before = engine.State.Clone();
            Assert.AreEqual(RejectReason.Blocked, engine.Remove("s2", 0).Reason);
            Assert.AreEqual(RejectReason.UnknownScrew, engine.Remove("nope", 0).Reason);
            Assert.AreEqual(RejectReason.StaleRevision, engine.Remove("s0", 5).Reason);
            Assert.AreEqual(RejectReason.NothingToUndo, engine.Undo(0).Reason);
            Assert.IsTrue(before.ContentEquals(engine.State));
            Assert.AreEqual(0, engine.Revision);
            Assert.AreEqual(0, engine.UndoDepth);
        }

        [Test]
        public void DuplicateRapidTap_RemovesOnce()
        {
            var engine = new PuzzleEngine(CompiledLevel.Compile(Simple()));
            Assert.IsTrue(engine.Remove("s0", 0).Accepted);
            Assert.AreEqual(RejectReason.StaleRevision, engine.Remove("s0", 0).Reason);
            Assert.AreEqual(RejectReason.NotOnObject, engine.Remove("s0", 1).Reason);
            Assert.AreEqual(1, engine.Revision);
            Assert.AreEqual(1, engine.State.TrayCount[0]);
        }

        [Test]
        public void DependencyChain_UnblocksOnlyAfterAllBlockersReleased()
        {
            var engine = new PuzzleEngine(CompiledLevel.Compile(Chain()));
            Assert.IsFalse(engine.IsAvailable("s2"));
            Assert.IsTrue(engine.Remove("s0", 0).Accepted);
            Assert.IsFalse(engine.IsAvailable("s2"), "p0 still has a screw");
            var r = engine.Remove("s1", 1);
            Assert.IsTrue(r.Events.OfType<PartReleasedEvent>().Any(e => e.PartId == "p0"));
            Assert.IsTrue(engine.IsAvailable("s2"));
            Assert.IsFalse(engine.IsAvailable("s5"));
        }

        [Test]
        public void MultipleBlockers_AllMustRelease()
        {
            var level = CompiledLevel.Compile(Build("multi", new[] { A, B },
                P("top1", new[] { A }), P("top2", new[] { B }), P("base", new[] { A, A, B, B }, "top1", "top2")));
            var e = new PuzzleEngine(level);
            e.Remove("s0", 0);
            Assert.IsFalse(e.IsAvailable("s2"));
            e.Remove("s1", 1);
            Assert.IsTrue(e.IsAvailable("s2"));
        }

        [Test]
        public void DuplicateActiveColors_RouteToLowestIndexWithRoom()
        {
            var level = CompiledLevel.Compile(Build("dup", new[] { A, A }, P("p", new[] { A, A, A, A, A, A })));
            var e = new PuzzleEngine(level);
            for (int i = 0; i < 2; i++) e.Remove("s" + i, e.Revision);
            CollectionAssert.AreEqual(new[] { 2, 0 }, e.State.TrayCount);
            var r = e.Remove("s2", e.Revision);
            Assert.AreEqual("remove:s2>T0 complete:0", string.Join(" ", r.Events.Select(Describe)));
            Assert.AreEqual(-1, e.State.TrayColor[0], "queue exhausted: position stays inactive");
            e.Remove("s3", e.Revision);
            Assert.AreEqual(1, e.State.TrayCount[1]);
        }

        [Test]
        public void ExhaustedQueue_EmptyTrayStaysInactive_AndLevelCanStillWin()
        {
            var level = CompiledLevel.Compile(Build("exhaust", new[] { A, B }, P("p", new[] { A, A, A, B, B, B })));
            var e = new PuzzleEngine(level);
            foreach (var id in new[] { "s0", "s1", "s2" }) e.Remove(id, e.Revision);
            Assert.AreEqual(-1, e.State.TrayColor[0]);
            foreach (var id in new[] { "s3", "s4" }) e.Remove(id, e.Revision);
            var r = e.Remove("s5", e.Revision);
            Assert.AreEqual(Outcome.Won, e.Outcome);
            Assert.IsInstanceOf<OutcomeChangedEvent>(r.Events.Last());
        }

        [Test]
        public void BufferSlots_ReuseLowestEmptySlot_TransfersUseInsertionOrder()
        {
            // Trays A,B. C screws must buffer until a C tray appears.
            var level = CompiledLevel.Compile(Build("reuse", new[] { A, B, C },
                P("p0", new[] { C, A, A, A }), P("p1", new[] { C, C, B, B, B })));
            var e = new PuzzleEngine(level);
            e.Remove("s0", e.Revision); // C -> buffer 0
            e.Remove("s4", e.Revision); // C -> buffer 1
            Assert.AreEqual(0, e.State.Slot[0]);
            Assert.AreEqual(1, e.State.Slot[4]);
            foreach (var id in new[] { "s1", "s2" }) e.Remove(id, e.Revision);
            var r = e.Remove("s3", e.Revision); // completes A, C tray arrives, both buffered C transfer in insertion order
            var trace = string.Join(" ", r.Events.Select(Describe));
            StringAssert.Contains("refill:0=2 xfer:s0>0 xfer:s4>0", trace);
            Assert.IsTrue(e.State.Buffer.All(b => b < 0));
            var r2 = e.Remove("s5", e.Revision);
            Assert.AreEqual(ScrewPlace.Collected, e.State.Place[5], "third C completes tray 0");
            Assert.IsTrue(r2.Events.OfType<TrayCompletedEvent>().Any(x => x.TrayIndex == 0));
        }

        [Test]
        public void FullBuffer_WithDirectMatch_IsNotLoss()
        {
            var level = CompiledLevel.Compile(Build("fullbuf", new[] { A, B, C, D },
                P("p0", new[] { C, C, C, D, D }), P("p1", new[] { A, D, A, A, B, B, B }, "p0")));
            var e = new PuzzleEngine(level);
            foreach (var id in new[] { "s0", "s1", "s2", "s3", "s4" }) e.Remove(id, e.Revision);
            Assert.IsTrue(e.State.Buffer.All(b => b >= 0), "buffer full");
            Assert.AreEqual(Outcome.Playing, e.Outcome, "A and B still route directly");
            Assert.IsTrue(e.Remove("s5", e.Revision).Accepted);
        }

        [Test]
        public void FullBuffer_NoMatch_IsLoss_AndUndoRecovers()
        {
            var trap = CompiledLevel.Compile(Build("trap", new[] { A, B, C, D },
                P("p0", new[] { C, C, C, D, D, D }), P("p1", new[] { A, A, A, B, B, B }, "p0")));
            var t = new PuzzleEngine(trap);
            CommandResult last = null;
            foreach (var id in new[] { "s0", "s1", "s2", "s3" }) last = t.Remove(id, t.Revision);
            Assert.AreEqual(Outcome.Playing, t.Outcome);
            last = t.Remove("s4", t.Revision);
            Assert.AreEqual(Outcome.Lost, t.Outcome, "buffer full, s5 has no tray, p1 still blocked");
            Assert.IsTrue(last.Events.OfType<OutcomeChangedEvent>().Any(x => x.Outcome == Outcome.Lost));
            Assert.AreEqual(RejectReason.NoDestination, t.Remove("s5", t.Revision).Reason);
            var rev = t.Revision;
            Assert.IsTrue(t.Undo(rev).Accepted);
            Assert.AreEqual(rev + 1, t.Revision, "undo assigns a fresh revision");
            Assert.AreEqual(Outcome.Playing, t.Outcome);
        }

        [Test]
        public void EmptyObject_WithNonemptyBuffer_IsNotVictory()
        {
            // Buffered C can never reach a tray if the queue never offers C again after trays are consumed.
            var level = CompiledLevel.Compile(Build("emptyobj", new[] { A, C }, P("p", new[] { A, A, A, C, C, C })));
            var st = Rules.CreateInitial(level);
            st.TrayColor = new[] { -1, -1 };
            st.QueueCursor = 2;
            st.CompletedTrays = 1;
            for (int s = 0; s < 3; s++) { st.Place[s] = ScrewPlace.Collected; }
            for (int s = 3; s < 6; s++) { st.Place[s] = ScrewPlace.Buffer; st.Slot[s] = s - 3; st.Seq[s] = s - 3; st.Buffer[s - 3] = s; }
            st.NextSeq = 3;
            st.Released[0] = true;
            Assert.IsNull(Rules.CheckInvariants(level, st));
            Assert.AreEqual(Outcome.Lost, Rules.Evaluate(level, st));
        }

        [Test]
        public void Victory_ClosesAttempt()
        {
            var e = new PuzzleEngine(CompiledLevel.Compile(Simple()));
            foreach (var id in new[] { "s0", "s2", "s4", "s1", "s3", "s5" }) Assert.IsTrue(e.Remove(id, e.Revision).Accepted, id);
            Assert.AreEqual(Outcome.Won, e.Outcome);
            Assert.AreEqual(RejectReason.AttemptClosed, e.Undo(e.Revision).Reason);
            Assert.AreEqual(RejectReason.AttemptClosed, e.Remove("s0", e.Revision).Reason);
        }

        [Test]
        public void Restart_RestoresAuthoredState_WithNewRevision()
        {
            var level = CompiledLevel.Compile(Chain());
            var e = new PuzzleEngine(level);
            e.Remove("s0", 0);
            e.Remove("s1", 1);
            e.Restart();
            Assert.IsTrue(Rules.CreateInitial(level).ContentEquals(e.State));
            Assert.AreEqual(3, e.Revision);
            Assert.AreEqual(0, e.UndoDepth);
        }

        [Test]
        public void Undo_RestoresExactPayload()
        {
            var e = new PuzzleEngine(CompiledLevel.Compile(Chain()));
            var snap = e.State.Clone();
            e.Remove("s0", 0);
            e.Undo(1);
            Assert.IsTrue(snap.ContentEquals(e.State));
        }

        [Test]
        public void Snapshots_AreDeep()
        {
            var e = new PuzzleEngine(CompiledLevel.Compile(Simple()));
            var snap = e.State.Clone();
            e.Remove("s0", 0);
            Assert.AreEqual(ScrewPlace.OnPart, snap.Place[0]);
            Assert.AreEqual(0, snap.TrayCount[0]);
        }

        [Test]
        public void RandomPlayouts_PreserveInvariants_AndReplayDeterministically([Range(0, 39)] int seed)
        {
            foreach (var def in new[] { Simple(), Chain(), SampleContent.Load("L01") , SampleContent.Load("L06"), SampleContent.Load("L10") })
            {
                var level = CompiledLevel.Compile(def);
                var run1 = Playout(level, seed, out var moves1);
                var run2 = Playout(level, seed, out var moves2);
                CollectionAssert.AreEqual(moves1, moves2);
                Assert.IsTrue(run1.ContentEquals(run2));
            }
        }

        private static PuzzleState Playout(CompiledLevel level, int seed, out List<string> moves)
        {
            var rng = new Random(seed);
            var e = new PuzzleEngine(level);
            var legal = new List<int>();
            moves = new List<string>();
            while (e.Outcome == Outcome.Playing)
            {
                Rules.LegalMoves(level, e.State, legal);
                Assert.IsNotEmpty(legal);
                var pick = level.ScrewIds[legal[rng.Next(legal.Count)]];
                if (rng.Next(10) == 0 && e.UndoDepth > 0) { Assert.IsTrue(e.Undo(e.Revision).Accepted); moves.Add("undo"); continue; }
                long rev = e.Revision;
                Assert.IsTrue(e.Remove(pick, rev).Accepted);
                Assert.AreEqual(rev + 1, e.Revision);
                moves.Add(pick);
                var err = Rules.CheckInvariants(level, e.State);
                Assert.IsNull(err, err);
            }
            return e.State;
        }
    }
}
