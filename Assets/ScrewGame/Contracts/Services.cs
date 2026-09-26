using System;
using System.Collections.Generic;

namespace ScrewGame.Contracts
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public interface IIdGenerator
    {
        string NewId();
    }

    /// <summary>Raw durable byte storage. Implementations must make WriteAtomic all-or-nothing for the named record.</summary>
    public interface IDurableStorage
    {
        bool TryRead(string name, out byte[] data);
        void WriteAtomic(string name, byte[] data);
        void Delete(string name);
    }

    public enum AdAvailability
    {
        Unavailable = 0,
        Loading = 1,
        Ready = 2,
    }

    public enum AdShowResult
    {
        Earned = 0,
        DismissedWithoutReward = 1,
        FailedToShow = 2,
        NotReady = 3,
    }

    /// <summary>Rewarded ad adapter. The callback reports the documented earned-reward signal only.</summary>
    public interface IRewardedAdService
    {
        bool IsTestAdapter { get; }
        AdAvailability Availability { get; }
        void Load();
        void Show(string operationId, Action<string, AdShowResult> onFinished);
    }

    public interface IConsentService
    {
        bool CanRequestAds { get; }
        bool AnalyticsAllowed { get; }
        bool PrivacyOptionsRequired { get; }
        void Refresh(Action onComplete);
        void ShowPrivacyOptions(Action onComplete);
    }

    public enum PurchaseOutcome
    {
        Purchased = 0,
        Pending = 1,
        Cancelled = 2,
        Failed = 3,
        Unavailable = 4,
    }

    public sealed class ProductInfo
    {
        public string ProductId;
        public string LocalizedTitle;
        public string LocalizedPrice;
        public bool Owned;
    }

    public interface IPurchaseService
    {
        bool IsTestAdapter { get; }
        bool IsReady { get; }
        IReadOnlyList<ProductInfo> Products { get; }
        void Initialize(Action onReady);
        void Purchase(string productId, Action<string, PurchaseOutcome, string> onResult);
        void Restore(Action<bool> onComplete);
    }

    public interface IAnalyticsService
    {
        void SetCollectionEnabled(bool enabled);
        void LogEvent(string name, IReadOnlyDictionary<string, object> parameters);
    }

    public interface ICrashReporter
    {
        void SetCollectionEnabled(bool enabled);
        void Log(string message);
        void RecordException(Exception exception);
    }

    public interface IRemoteSettings
    {
        int GetInt(string key, int fallback, int min, int max);
        bool GetBool(string key, bool fallback);
    }

    public interface IHaptics
    {
        bool Supported { get; }
        void Play(HapticKind kind);
    }

    public enum HapticKind
    {
        Selection = 0,
        Success = 1,
        Error = 2,
    }
}
