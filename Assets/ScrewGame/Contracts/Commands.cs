using System;
using System.Collections.Generic;

namespace ScrewGame.Contracts
{
    /// <summary>Player intent sent to the session. ExpectedRevision must equal the current session revision or the command is rejected as stale.</summary>
    public abstract class GameCommand
    {
        public long ExpectedRevision;
    }

    public sealed class RemoveScrewCommand : GameCommand
    {
        public string ScrewId;
    }

    public sealed class UndoCommand : GameCommand
    {
    }

    /// <summary>Restart is always accepted for an open or closed attempt and creates a new attempt ID.</summary>
    public sealed class RestartCommand : GameCommand
    {
    }

    /// <summary>Presents a previously computed hint; the session re-checks binding and consumes one credit only if it is still current and useful.</summary>
    public sealed class ApplyHintCommand : GameCommand
    {
        public HintResult Hint;
    }

    /// <summary>Terminal attempt outcome, stored beside (not inside) the puzzle payload. Loss is not terminal.</summary>
    public enum AttemptOutcome
    {
        Open = 0,
        Won = 1,
        Replaced = 2,
    }

    /// <summary>Read-only settled view data for presentation reconstruction after interruption or relaunch.</summary>
    public sealed class SettledSnapshot
    {
        public string LevelId;
        public string ContentHash;
        public string AttemptId;
        public long Revision;
        public Outcome PuzzleOutcome;
        public AttemptOutcome AttemptOutcome;
        public IReadOnlyList<string> AttachedScrewIds;
        public IReadOnlyList<string> ReleasedPartIds;
        public IReadOnlyList<int> TrayColors;
        public IReadOnlyList<int> TrayFill;
        /// <summary>Buffer slot index to screw ID (null when empty).</summary>
        public IReadOnlyList<string> BufferSlots;
    }

    public enum SaveOutcome
    {
        Committed = 0,
        Failed = 1,
        ReadOnly = 2,
    }

    /// <summary>External grant (rewarded ad) delivered to the session. FulfillmentId makes delivery idempotent.</summary>
    public sealed class RewardGrant
    {
        public string FulfillmentId;
        public int HelpCredits = 1;
    }

    /// <summary>Store-verified ownership record for a non-consumable product.</summary>
    public sealed class EntitlementGrant
    {
        public string ProductId;
        public string TransactionId;
        public bool Restored;
    }

    public enum ConsentStatus
    {
        Unknown = 0,
        Required = 1,
        Obtained = 2,
        NotRequired = 3,
    }

    /// <summary>Platform tracking authorization (iOS App Tracking Transparency); NotApplicable elsewhere.</summary>
    public enum TrackingAuthorization
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3,
        NotApplicable = 4,
    }

    /// <summary>Three independent privacy decisions; none implies another.</summary>
    public sealed class PrivacyState
    {
        public ConsentStatus AdConsent;
        public bool CanRequestAds;
        public TrackingAuthorization Tracking;
        public bool? AnalyticsAllowed;
    }

    public enum DiagnosticSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    /// <summary>Non-fatal diagnostic; must not contain personal data or free text entered by players.</summary>
    public sealed class DiagnosticRecord
    {
        public DiagnosticSeverity Severity;
        public string Code;
        public string LevelId;
        public string AttemptId;
        public long Revision;
    }

    /// <summary>Tunable non-puzzle configuration. Remote values are clamped; puzzle capacities are level data and never remote.</summary>
    public sealed class GameConfig
    {
        public int FreeUndoPerAttempt = 1;
        public int FreeHintPerAttempt = 1;
        public int RewardCreditsPerAd = 1;
        public int HintSearchBudget = 200000;
        public bool RewardedAdsEnabled = true;
        public bool InterstitialsEnabled;

        public static GameConfig FromRemote(IRemoteSettings remote)
        {
            var c = new GameConfig();
            if (remote == null) return c;
            c.FreeUndoPerAttempt = remote.GetInt("free_undo_per_attempt", c.FreeUndoPerAttempt, 0, 3);
            c.FreeHintPerAttempt = remote.GetInt("free_hint_per_attempt", c.FreeHintPerAttempt, 0, 3);
            c.RewardCreditsPerAd = remote.GetInt("reward_credits_per_ad", c.RewardCreditsPerAd, 1, 1);
            c.HintSearchBudget = remote.GetInt("hint_search_budget", c.HintSearchBudget, 10000, 2000000);
            c.RewardedAdsEnabled = remote.GetBool("rewarded_ads_enabled", c.RewardedAdsEnabled);
            c.InterstitialsEnabled = false;
            return c;
        }
    }
}
