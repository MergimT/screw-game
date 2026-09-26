using System;
using System.Collections.Generic;
using ScrewGame.Contracts;
using ScrewGame.Core;

namespace ScrewGame.Persistence
{
    /// <summary>Single durable profile document. Every gameplay commit writes it atomically so attempt, help and progression never diverge.</summary>
    [Serializable]
    public sealed class ProfileData
    {
        public const int CurrentVersion = 1;

        public int SaveVersion = CurrentVersion;
        public SettingsData Settings = new SettingsData();
        public TutorialData Tutorial = new TutorialData();
        public ProgressData Progress = new ProgressData();
        public DailyData Daily = new DailyData();
        public HelpData Help = new HelpData();
        public LedgerData Ledger = new LedgerData();
        public ConsentData Consent = new ConsentData();
        public AttemptRecord ActiveAttempt;
    }

    [Serializable]
    public sealed class SettingsData
    {
        public string Language = "";
        public bool Sound = true;
        public bool Haptics = true;
        public bool ReducedMotion;
        public bool ColorSymbols;
        public string Theme = "default";
    }

    [Serializable]
    public sealed class TutorialData
    {
        public int Step;
        public bool Completed;
    }

    [Serializable]
    public sealed class ProgressData
    {
        public List<string> CompletedLevels = new List<string>();
        public List<string> CollectedObjects = new List<string>();
        public int Wins;
    }

    [Serializable]
    public sealed class DailyData
    {
        /// <summary>UTC date (yyyy-MM-dd) to pinned level ID; pinned on first view so content updates and clock changes cannot swap it.</summary>
        public Dictionary<string, string> Pinned = new Dictionary<string, string>();
        public List<string> CompletedDates = new List<string>();
    }

    [Serializable]
    public sealed class HelpData
    {
        /// <summary>Credits earned from rewarded ads; usable for either undo or hint.</summary>
        public int BankedCredits;
    }

    [Serializable]
    public sealed class LedgerData
    {
        public List<string> FulfilledRewards = new List<string>();
        public List<string> FulfilledPurchases = new List<string>();
        public List<string> OwnedProducts = new List<string>();
    }

    [Serializable]
    public sealed class ConsentData
    {
        /// <summary>null = not asked yet. Independent of ad consent and tracking authorization.</summary>
        public bool? AnalyticsAllowed;
        public ConsentStatus AdConsent = ConsentStatus.Unknown;
        public TrackingAuthorization Tracking = TrackingAuthorization.NotDetermined;
    }

    [Serializable]
    public sealed class AttemptRecord
    {
        public string LevelId = "";
        public string ContentHash = "";
        public int RulesVersion = RulesDefaults.RulesVersion;
        public string AttemptId = "";
        public long Revision;
        public bool IsDaily;
        public string DailyDate = "";
        public int FreeUndoRemaining = 1;
        public int FreeHintRemaining = 1;
        public bool Closed;
        /// <summary>Terminal outcome tracked outside the puzzle payload; undo never changes it.</summary>
        public AttemptOutcome Terminal = AttemptOutcome.Open;
        public PuzzleState State;
        public List<PuzzleState> History = new List<PuzzleState>();
    }
}
