using System;
using System.Collections.Generic;
using ScrewGame.Contracts;
using ScrewGame.Core;
using ScrewGame.Persistence;
using ScrewGame.Progression;
using ScrewGame.Validation;

namespace ScrewGame.Session
{
    public enum StartKind
    {
        New = 0,
        Resumed = 1,
        /// <summary>A saved attempt existed but no longer matches bundled content or rules; a fresh attempt was started.</summary>
        ReplacedIncompatible = 2,
    }

    /// <summary>
    /// Owns one active attempt: gates duplicate commands, spends help credits, records victory, and commits every accepted
    /// logical change durably before its events are returned for presentation. On a failed write the in-memory state is
    /// rolled back to the last committed state and SaveFailed is returned.
    /// </summary>
    public sealed class GameSession
    {
        public const int FreeUndoPerAttempt = 1;
        public const int FreeHintPerAttempt = 1;

        private readonly SaveStore _store;
        private readonly IIdGenerator _ids;
        private readonly Campaign _campaign;
        private bool _busy;

        public ProfileData Profile { get; private set; }
        public PuzzleEngine Engine { get; private set; }
        public AttemptRecord Attempt => Profile.ActiveAttempt;
        public CompiledLevel Level => Engine?.Level;
        public Exception LastSaveError { get; private set; }

        public event Action<string, IReadOnlyDictionary<string, object>> Analytics;

        public GameSession(SaveStore store, ProfileData profile, IIdGenerator ids, Campaign campaign)
        {
            _store = store;
            Profile = profile;
            _ids = ids;
            _campaign = campaign;
        }

        public HelpBalance Help => new HelpBalance
        {
            FreeUndoRemaining = Attempt?.FreeUndoRemaining ?? 0,
            FreeHintRemaining = Attempt?.FreeHintRemaining ?? 0,
            BankedCredits = Profile.Help.BankedCredits,
        };

        /// <summary>Resumes the saved attempt for this level when compatible; otherwise starts a new attempt.</summary>
        public StartKind Start(CompiledLevel level, bool isDaily = false, string dailyDate = "")
        {
            var saved = Profile.ActiveAttempt;
            if (saved != null && !saved.Closed && saved.LevelId == level.Definition.Id)
            {
                if (saved.ContentHash == level.ContentHash && saved.RulesVersion == RulesDefaults.RulesVersion)
                {
                    var restored = PuzzleEngine.Restore(level, saved.State, saved.Revision, saved.History);
                    if (restored != null)
                    {
                        Engine = restored;
                        Emit("attempt_resume", level, null);
                        return StartKind.Resumed;
                    }
                }
                NewAttempt(level, isDaily, dailyDate);
                return StartKind.ReplacedIncompatible;
            }
            NewAttempt(level, isDaily, dailyDate);
            return StartKind.New;
        }

        private void NewAttempt(CompiledLevel level, bool isDaily, string dailyDate)
        {
            Engine = new PuzzleEngine(level);
            Profile.ActiveAttempt = new AttemptRecord
            {
                LevelId = level.Definition.Id,
                ContentHash = level.ContentHash,
                AttemptId = _ids.NewId(),
                IsDaily = isDaily,
                DailyDate = dailyDate ?? "",
                FreeUndoRemaining = FreeUndoPerAttempt,
                FreeHintRemaining = FreeHintPerAttempt,
            };
            SyncAttempt();
            Commit(out _);
            Emit("attempt_start", level, null);
        }

        public CommandResult Remove(string screwId, long expectedRevision)
        {
            if (_busy) return CommandResult.Reject(RejectReason.Busy, Engine.Revision);
            if (Attempt.Closed) return CommandResult.Reject(RejectReason.AttemptClosed, Engine.Revision);
            return Transact(() =>
            {
                var r = Engine.Remove(screwId, expectedRevision);
                if (r.Accepted && Engine.Outcome == Outcome.Won) CloseWithVictory();
                return r;
            });
        }

        public CommandResult Undo(long expectedRevision)
        {
            if (_busy) return CommandResult.Reject(RejectReason.Busy, Engine.Revision);
            if (Attempt.Closed) return CommandResult.Reject(RejectReason.AttemptClosed, Engine.Revision);
            if (Engine.UndoDepth == 0) return CommandResult.Reject(RejectReason.NothingToUndo, Engine.Revision);
            if (Help.Available(HelpKind.Undo) == 0) return CommandResult.Reject(RejectReason.NoHelpCredit, Engine.Revision);
            var result = Transact(() =>
            {
                var r = Engine.Undo(expectedRevision);
                if (r.Accepted) Spend(HelpKind.Undo);
                return r;
            });
            if (result.Accepted) Emit("help_used", Engine.Level, new Dictionary<string, object> { ["kind"] = "undo" });
            return result;
        }

        public CommandResult Restart()
        {
            if (_busy) return CommandResult.Reject(RejectReason.Busy, Engine.Revision);
            var level = Engine.Level;
            bool daily = Attempt.IsDaily;
            string date = Attempt.DailyDate;
            var snapshot = SaveStore.Clone(Profile);
            var prevEngine = Engine;
            Emit("attempt_restart", level, null);
            Engine = new PuzzleEngine(level);
            Profile.ActiveAttempt = new AttemptRecord
            {
                LevelId = level.Definition.Id,
                ContentHash = level.ContentHash,
                AttemptId = _ids.NewId(),
                IsDaily = daily,
                DailyDate = date,
                FreeUndoRemaining = FreeUndoPerAttempt,
                FreeHintRemaining = FreeHintPerAttempt,
            };
            SyncAttempt();
            if (!Commit(out _))
            {
                Profile = snapshot;
                Engine = prevEngine;
                return CommandResult.Reject(RejectReason.SaveFailed, Engine.Revision);
            }
            return CommandResult.Accept(Engine.Revision, new VisualEvent[] { new StateReplacedEvent() });
        }

        /// <summary>Whether requesting a hint could possibly be paid for right now.</summary>
        public bool CanRequestHint => !Attempt.Closed && Help.Available(HelpKind.Hint) > 0;

        /// <summary>Computes a hint for the current settled state. Pure; call ApplyHint to consume a credit.</summary>
        public HintResult ComputeHint(int budget = HintService.DefaultBudget)
        {
            return HintService.Compute(Engine.Level, Engine.State.Clone(), Attempt.AttemptId, Engine.Revision, budget);
        }

        /// <summary>Consumes one hint credit only for a current, useful suggestion. Returns false when stale, useless or unpaid.</summary>
        public bool ApplyHint(HintResult hint)
        {
            if (_busy || Attempt.Closed) return false;
            if (hint == null || hint.Status != HintStatus.Suggested) return false;
            if (!HintService.IsCurrent(hint, Attempt.AttemptId, Engine.Revision, Engine.Level.ContentHash)) { hint.Status = HintStatus.Stale; return false; }
            if (Help.Available(HelpKind.Hint) == 0) return false;
            var snapshot = SaveStore.Clone(Profile);
            Spend(HelpKind.Hint);
            if (!Commit(out _)) { Profile = snapshot; return false; }
            Emit("help_used", Engine.Level, new Dictionary<string, object> { ["kind"] = "hint" });
            return true;
        }

        /// <summary>Grants one banked help credit for a verified earned reward. Idempotent per fulfillment ID.</summary>
        public bool GrantReward(string fulfillmentId)
        {
            if (string.IsNullOrEmpty(fulfillmentId) || Profile.Ledger.FulfilledRewards.Contains(fulfillmentId)) return false;
            var snapshot = SaveStore.Clone(Profile);
            Profile.Ledger.FulfilledRewards.Add(fulfillmentId);
            Profile.Help.BankedCredits++;
            if (!Commit(out _)) { Profile = snapshot; return false; }
            return true;
        }

        /// <summary>Commits non-gameplay profile edits (settings, tutorial, consent, pins).</summary>
        public bool SaveProfile(Action<ProfileData> edit)
        {
            var snapshot = SaveStore.Clone(Profile);
            edit(Profile);
            if (Commit(out _)) return true;
            Profile = snapshot;
            return false;
        }

        private void Spend(HelpKind kind)
        {
            if (kind == HelpKind.Undo && Attempt.FreeUndoRemaining > 0) Attempt.FreeUndoRemaining--;
            else if (kind == HelpKind.Hint && Attempt.FreeHintRemaining > 0) Attempt.FreeHintRemaining--;
            else Profile.Help.BankedCredits--;
        }

        private void CloseWithVictory()
        {
            Attempt.Closed = true;
            var id = Engine.Level.Definition.Id;
            bool first;
            if (Attempt.IsDaily) first = DailyChallenge.RecordCompletion(Profile, Attempt.DailyDate);
            else first = _campaign != null && _campaign.RecordVictory(Profile, id);
            Emit("level_win", Engine.Level, new Dictionary<string, object> { ["first_win"] = first, ["daily"] = Attempt.IsDaily });
        }

        private CommandResult Transact(Func<CommandResult> action)
        {
            _busy = true;
            try
            {
                var profileBefore = SaveStore.Clone(Profile);
                var stateBefore = Engine.State.Clone();
                var revBefore = Engine.Revision;
                var historyBefore = Engine.CopyHistory();
                var r = action();
                if (!r.Accepted) return r;
                SyncAttempt();
                if (Commit(out _))
                {
                    if (Engine.Outcome == Outcome.Lost) Emit("level_stuck", Engine.Level, null);
                    return r;
                }
                Profile = profileBefore;
                Engine.RollBack(stateBefore, revBefore, historyBefore);
                return CommandResult.Reject(RejectReason.SaveFailed, revBefore);
            }
            finally
            {
                _busy = false;
            }
        }

        private void SyncAttempt()
        {
            Attempt.Revision = Engine.Revision;
            Attempt.State = Engine.State.Clone();
            Attempt.History = Engine.CopyHistory();
        }

        private bool Commit(out Exception error)
        {
            bool ok = _store.TrySave(Profile, out error);
            LastSaveError = ok ? null : error;
            return ok;
        }

        private void Emit(string name, CompiledLevel level, Dictionary<string, object> extra)
        {
            if (Analytics == null) return;
            var p = extra ?? new Dictionary<string, object>();
            p["level_id"] = level.Definition.Id;
            p["content_hash"] = level.ContentHash.Substring(0, 12);
            p["attempt_id"] = Attempt?.AttemptId ?? "";
            p["revision"] = Engine?.Revision ?? 0;
            Analytics(name, p);
        }
    }

    public sealed class GuidIds : IIdGenerator
    {
        public string NewId() => Guid.NewGuid().ToString("N");
    }

    public sealed class SequentialIds : IIdGenerator
    {
        private int _n;
        private readonly string _prefix;
        public SequentialIds(string prefix = "id") { _prefix = prefix; }
        public string NewId() => _prefix + (++_n);
    }
}
