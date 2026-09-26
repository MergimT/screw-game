using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ScrewGame.Contracts;
using ScrewGame.Core;
using ScrewGame.Persistence;
using ScrewGame.Progression;
using ScrewGame.Session;
using ScrewGame.Validation;

namespace ScrewGame.Tests
{
    public class SessionTests
    {
        private static Campaign MakeCampaign()
        {
            return new Campaign(SampleContent.AllIds().Select(id =>
            {
                var d = SampleContent.Load(id);
                return new CampaignEntry { LevelId = d.Id, ObjectId = d.ObjectId, Name = d.Name, Family = d.Family };
            }).ToList());
        }

        private static (GameSession session, MemoryStorage disk, SaveStore store) NewSession(MemoryStorage disk = null)
        {
            disk ??= new MemoryStorage();
            var store = new SaveStore(disk);
            var profile = store.Load(out _);
            return (new GameSession(store, profile, new SequentialIds("att"), MakeCampaign()), disk, store);
        }

        private static CompiledLevel Level(string id) => CompiledLevel.Compile(SampleContent.Load(id));

        private static List<string> Witness(CompiledLevel level) => new Solver(level).Solve().Witness;

        [Test]
        public void MoveIsCommittedBeforeEventsReturned_AndResumesAfterRelaunch()
        {
            var (s, disk, _) = NewSession();
            var level = Level("L04");
            Assert.AreEqual(StartKind.New, s.Start(level));
            var w = Witness(level);
            Assert.IsTrue(s.Remove(w[0], 0).Accepted);
            Assert.IsTrue(s.Remove(w[1], 1).Accepted);

            var (s2, _, _) = NewSession(disk);
            Assert.AreEqual(StartKind.Resumed, s2.Start(level));
            Assert.AreEqual(2, s2.Engine.Revision);
            Assert.IsTrue(s.Engine.State.ContentEquals(s2.Engine.State));
            Assert.AreEqual(s.Attempt.AttemptId, s2.Attempt.AttemptId);
            Assert.AreEqual(2, s2.Engine.UndoDepth, "undo history survives relaunch");
        }

        [Test]
        public void SaveFailure_RollsBackAndReportsError()
        {
            var (s, disk, _) = NewSession();
            var level = Level("L04");
            s.Start(level);
            var w = Witness(level);
            var before = s.Engine.State.Clone();
            disk.FailWritesRemaining = 1;
            disk.FailOnlyName = SaveStore.PrimaryName;
            var r = s.Remove(w[0], 0);
            Assert.AreEqual(RejectReason.SaveFailed, r.Reason);
            Assert.AreEqual(0, s.Engine.Revision);
            Assert.IsTrue(before.ContentEquals(s.Engine.State));
            Assert.IsNotNull(s.LastSaveError);
            Assert.IsTrue(s.Remove(w[0], 0).Accepted, "recovers once storage works");
        }

        [Test]
        public void Undo_UsesFreeThenBankedCredit_AndIsDurable()
        {
            var (s, disk, _) = NewSession();
            var level = Level("L05");
            s.Start(level);
            var w = Witness(level);
            s.Remove(w[0], 0);
            Assert.IsTrue(s.Undo(1).Accepted);
            Assert.AreEqual(0, s.Help.FreeUndoRemaining);
            s.Remove(w[0], 2);
            Assert.AreEqual(RejectReason.NoHelpCredit, s.Undo(3).Reason);
            Assert.AreEqual(3, s.Engine.Revision, "rejected undo keeps revision");
            Assert.IsTrue(s.GrantReward("ad-op-1"));
            Assert.IsTrue(s.Undo(3).Accepted);
            Assert.AreEqual(0, s.Help.BankedCredits);

            var (s2, _, _) = NewSession(disk);
            s2.Start(level);
            Assert.AreEqual(0, s2.Help.FreeUndoRemaining);
            Assert.AreEqual(4, s2.Engine.Revision);
        }

        [Test]
        public void DuplicateRewardCallback_GrantsOnce_AcrossRelaunch()
        {
            var (s, disk, _) = NewSession();
            s.Start(Level("L01"));
            Assert.IsTrue(s.GrantReward("op-7"));
            Assert.IsFalse(s.GrantReward("op-7"));
            var (s2, _, _) = NewSession(disk);
            Assert.IsFalse(s2.GrantReward("op-7"));
            Assert.AreEqual(1, s2.Profile.Help.BankedCredits);
        }

        [Test]
        public void Hint_ConsumesOnlyWhenUsefulAndCurrent()
        {
            var (s, _, _) = NewSession();
            var level = Level("L06");
            s.Start(level);
            var stale = s.ComputeHint();
            s.Remove(stale.ScrewId, 0);
            Assert.IsFalse(s.ApplyHint(stale));
            Assert.AreEqual(HintStatus.Stale, stale.Status);
            Assert.AreEqual(1, s.Help.FreeHintRemaining);
            var h = s.ComputeHint();
            Assert.IsTrue(s.ApplyHint(h));
            Assert.AreEqual(0, s.Help.FreeHintRemaining);
            var h2 = s.ComputeHint();
            Assert.IsFalse(s.ApplyHint(h2), "no credit left");
            Assert.IsFalse(s.CanRequestHint);
        }

        [Test]
        public void UselessHint_DoesNotConsume()
        {
            var (s, _, _) = NewSession();
            s.Start(Level("L01"));
            var h = new HintResult { Status = HintStatus.Unsolvable, AttemptId = s.Attempt.AttemptId, Revision = s.Engine.Revision, ContentHash = s.Level.ContentHash };
            Assert.IsFalse(s.ApplyHint(h));
            Assert.AreEqual(1, s.Help.FreeHintRemaining);
        }

        [Test]
        public void Restart_CreatesNewAttemptAndResetsFreeHelp()
        {
            var (s, _, _) = NewSession();
            var level = Level("L05");
            s.Start(level);
            var first = s.Attempt.AttemptId;
            var w = Witness(level);
            s.Remove(w[0], 0);
            s.Undo(1);
            Assert.IsTrue(s.Restart().Accepted);
            Assert.AreNotEqual(first, s.Attempt.AttemptId);
            Assert.AreEqual(1, s.Help.FreeUndoRemaining);
            Assert.IsTrue(Rules.CreateInitial(level).ContentEquals(s.Engine.State));
        }

        [Test]
        public void Victory_IsIdempotent_UnlocksNextAndCollection()
        {
            var (s, disk, _) = NewSession();
            var level = Level("L01");
            s.Start(level);
            var camp = MakeCampaign();
            Assert.IsFalse(camp.IsUnlocked(s.Profile, "L02"));
            foreach (var id in Witness(level)) Assert.IsTrue(s.Remove(id, s.Engine.Revision).Accepted);
            Assert.AreEqual(Outcome.Won, s.Engine.Outcome);
            Assert.IsTrue(s.Attempt.Closed);
            Assert.IsTrue(camp.IsUnlocked(s.Profile, "L02"));
            CollectionAssert.Contains(s.Profile.Progress.CollectedObjects, "plank");
            Assert.AreEqual(RejectReason.AttemptClosed, s.Undo(s.Engine.Revision).Reason);

            // Replay the same level: second victory does not duplicate progression.
            var (s2, _, _) = NewSession(disk);
            Assert.AreEqual(StartKind.New, s2.Start(level), "closed attempt is not resumed");
            foreach (var id in Witness(level)) s2.Remove(id, s2.Engine.Revision);
            Assert.AreEqual(1, s2.Profile.Progress.CompletedLevels.Count(x => x == "L01"));
            Assert.AreEqual(1, s2.Profile.Progress.CollectedObjects.Count);
            Assert.AreEqual(1, s2.Profile.Progress.Wins, "wins counts first victories only");
        }

        [Test]
        public void InterruptedVictoryWrite_KeepsPreVictoryState()
        {
            var (s, disk, _) = NewSession();
            var level = Level("L01");
            s.Start(level);
            var w = Witness(level);
            for (int i = 0; i < w.Count - 1; i++) s.Remove(w[i], s.Engine.Revision);
            disk.FailWritesRemaining = 1;
            disk.FailOnlyName = SaveStore.PrimaryName;
            Assert.AreEqual(RejectReason.SaveFailed, s.Remove(w[w.Count - 1], s.Engine.Revision).Reason);
            Assert.IsFalse(s.Attempt.Closed);
            Assert.IsEmpty(s.Profile.Progress.CompletedLevels);
            var (s2, _, _) = NewSession(disk);
            Assert.AreEqual(StartKind.Resumed, s2.Start(level));
            Assert.IsTrue(s2.Remove(w[w.Count - 1], s2.Engine.Revision).Accepted);
            CollectionAssert.Contains(s2.Profile.Progress.CompletedLevels, "L01");
        }

        [Test]
        public void ChangedContent_ReplacesIncompatibleAttempt()
        {
            var (s, disk, _) = NewSession();
            var level = Level("L02");
            s.Start(level);
            s.Remove(Witness(level)[0], 0);
            var edited = SampleContent.Load("L02");
            edited.ContentVersion = 2;
            var (s2, _, _) = NewSession(disk);
            Assert.AreEqual(StartKind.ReplacedIncompatible, s2.Start(CompiledLevel.Compile(edited)));
            Assert.AreEqual(0, s2.Engine.Revision);
        }

        [Test]
        public void Busy_RejectsReentrantCommands()
        {
            var (s, _, _) = NewSession();
            s.Start(Level("L01"));
            RejectReason nested = RejectReason.None;
            s.Analytics += (n, p) => { if (n == "level_stuck" || n == "attempt_start") return; };
            // Re-entrancy is only possible through callbacks; simulate by invoking during analytics of a win on L01.
            s.Analytics += (n, p) => { if (n == "level_win") nested = s.Remove("L01-s00", s.Engine.Revision).Reason; };
            foreach (var id in Witness(s.Level)) s.Remove(id, s.Engine.Revision);
            Assert.AreEqual(RejectReason.Busy, nested);
        }
    }

    public class PersistenceTests
    {
        [Test]
        public void CorruptPrimary_RecoversFromBackup()
        {
            var disk = new MemoryStorage();
            var store = new SaveStore(disk);
            var p = store.Load(out var st);
            Assert.AreEqual(LoadStatus.Fresh, st);
            p.Settings.Sound = false;
            Assert.IsTrue(store.TrySave(p, out _));
            p.Settings.Haptics = false;
            Assert.IsTrue(store.TrySave(p, out _));
            var bytes = disk.Files[SaveStore.PrimaryName];
            disk.Files[SaveStore.PrimaryName] = bytes.Take(bytes.Length / 2).ToArray();

            var loaded = new SaveStore(disk).Load(out st);
            Assert.AreEqual(LoadStatus.RecoveredFromBackup, st);
            Assert.IsFalse(loaded.Settings.Sound);
            Assert.IsTrue(loaded.Settings.Haptics, "backup is the previous committed version");
            Assert.IsTrue(disk.Files.ContainsKey(SaveStore.QuarantinePrefix + "primary"));
        }

        [Test]
        public void TamperedChecksum_IsRejected()
        {
            var disk = new MemoryStorage();
            var store = new SaveStore(disk);
            var p = store.Load(out _);
            store.TrySave(p, out _);
            var text = Encoding.UTF8.GetString(disk.Files[SaveStore.PrimaryName]).Replace("\\\"Sound\\\":true", "\\\"Sound\\\":false");
            disk.Files[SaveStore.PrimaryName] = Encoding.UTF8.GetBytes(text);
            new SaveStore(disk).Load(out var st);
            Assert.AreEqual(LoadStatus.CorruptReset, st);
        }

        [Test]
        public void BothCorrupt_ResetsAndQuarantines()
        {
            var disk = new MemoryStorage();
            disk.Files[SaveStore.PrimaryName] = new byte[] { 1, 2, 3 };
            disk.Files[SaveStore.BackupName] = new byte[0];
            var p = new SaveStore(disk).Load(out var st);
            Assert.AreEqual(LoadStatus.CorruptReset, st);
            Assert.NotNull(p);
            Assert.IsTrue(disk.Files.ContainsKey(SaveStore.QuarantinePrefix + "backup"));
        }

        [Test]
        public void NewerVersion_IsReadOnly()
        {
            var disk = new MemoryStorage();
            var env = "{\"Format\":\"screwgame-profile\",\"Version\":99,\"Sequence\":1,\"Sha256\":\"" + Sha("{}") + "\",\"Payload\":\"{}\"}";
            disk.Files[SaveStore.PrimaryName] = Encoding.UTF8.GetBytes(env);
            var store = new SaveStore(disk);
            store.Load(out var st);
            Assert.AreEqual(LoadStatus.NewerVersionReadOnly, st);
            Assert.IsFalse(store.TrySave(new ProfileData(), out _));
            Assert.AreEqual(env, Encoding.UTF8.GetString(disk.Files[SaveStore.PrimaryName]), "newer save untouched");
        }

        private static string Sha(string s)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(s)).Select(b => b.ToString("x2")));
        }

        [Test]
        public void FailedWrite_LeavesPreviousCommitReadable()
        {
            var disk = new MemoryStorage();
            var store = new SaveStore(disk);
            var p = store.Load(out _);
            p.Tutorial.Step = 1;
            store.TrySave(p, out _);
            p.Tutorial.Step = 2;
            disk.FailWritesRemaining = 1;
            disk.FailOnlyName = SaveStore.PrimaryName;
            Assert.IsFalse(store.TrySave(p, out var err));
            Assert.IsNotNull(err);
            Assert.AreEqual(1, new SaveStore(disk).Load(out _).Tutorial.Step);
        }

        [Test]
        public void SequenceIsMonotonic()
        {
            var store = new SaveStore(new MemoryStorage());
            var p = store.Load(out _);
            store.TrySave(p, out _);
            store.TrySave(p, out _);
            Assert.AreEqual(2, store.Sequence);
        }

        [Test]
        public void FileStorage_RoundTripsAndReplacesAtomically()
        {
            var dir = Path.Combine(Path.GetTempPath(), "screwgame-test-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                var fs = new FileDurableStorage(dir);
                var store = new SaveStore(fs);
                var p = store.Load(out _);
                p.Settings.Language = "fr";
                Assert.IsTrue(store.TrySave(p, out _));
                p.Settings.Language = "en";
                Assert.IsTrue(store.TrySave(p, out _));
                Assert.IsFalse(File.Exists(Path.Combine(dir, SaveStore.PrimaryName + ".tmp")));
                var loaded = new SaveStore(new FileDurableStorage(dir)).Load(out var st);
                Assert.AreEqual(LoadStatus.Primary, st);
                Assert.AreEqual("en", loaded.Settings.Language);
                // Simulate crash mid-write: stale temp file left behind must not affect load.
                File.WriteAllText(Path.Combine(dir, SaveStore.PrimaryName + ".tmp"), "garbage");
                Assert.AreEqual("en", new SaveStore(new FileDurableStorage(dir)).Load(out _).Settings.Language);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }
    }

    public class ProgressionTests
    {
        [Test]
        public void Daily_IsDeterministicAndPinned()
        {
            var pool = new[] { "L03", "L05", "L07" };
            var a = DailyChallenge.Select("2026-09-26", pool, 1);
            Assert.AreEqual(a, DailyChallenge.Select("2026-09-26", pool, 1));
            var p = new ProfileData();
            var pinned = DailyChallenge.Resolve(p, "2026-09-26", pool, 1);
            Assert.AreEqual(pinned, DailyChallenge.Resolve(p, "2026-09-26", pool, 2), "content update keeps pin");
            var days = Enumerable.Range(1, 30).Select(d => DailyChallenge.Select("2026-10-" + d.ToString("00"), pool, 1)).Distinct().Count();
            Assert.Greater(days, 1);
        }

        [Test]
        public void Daily_MidnightAndRollback()
        {
            var p = new ProfileData();
            Assert.IsTrue(DailyChallenge.RecordCompletion(p, "2026-09-26"));
            Assert.IsFalse(DailyChallenge.RecordCompletion(p, "2026-09-26"), "duplicate");
            Assert.IsTrue(DailyChallenge.IsCompleted(p, "2026-09-26"));
            Assert.IsFalse(DailyChallenge.IsCompleted(p, "2026-09-27"));
            Assert.AreEqual("2026-09-26", DailyChallenge.DateKey(new System.DateTime(2026, 9, 26, 23, 59, 59, System.DateTimeKind.Utc)));
        }
    }
}
