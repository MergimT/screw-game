using System;
using System.Collections.Generic;
using ScrewGame.Contracts;
using UnityEngine;

namespace ScrewGame.Services
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    /// <summary>Release default until a real SDK adapter is configured: honestly reports rewarded help as unavailable.</summary>
    public sealed class UnavailableRewardedAds : IRewardedAdService
    {
        public bool IsTestAdapter => false;
        public AdAvailability Availability => AdAvailability.Unavailable;
        public void Load() { }
        public void Show(string operationId, Action<string, AdShowResult> onFinished) => onFinished(operationId, AdShowResult.NotReady);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>Editor/development-only fake that grants immediately. Compiled out of release builds.</summary>
    public sealed class TestRewardedAds : IRewardedAdService
    {
        public bool IsTestAdapter => true;
        public AdAvailability Availability => AdAvailability.Ready;
        public void Load() { }
        public void Show(string operationId, Action<string, AdShowResult> onFinished) => onFinished(operationId, AdShowResult.Earned);
    }
#endif

    /// <summary>Local-only analytics sink: keeps recent events in memory and logs them in development builds. Sends nothing.</summary>
    public sealed class LocalAnalytics : IAnalyticsService
    {
        public readonly List<string> Recent = new List<string>();
        public bool Enabled { get; private set; }
        public void SetCollectionEnabled(bool enabled) => Enabled = enabled;

        public void LogEvent(string name, IReadOnlyDictionary<string, object> parameters)
        {
            if (!Enabled) return;
            var line = name;
            if (parameters != null) foreach (var kv in parameters) line += " " + kv.Key + "=" + kv.Value;
            Recent.Add(line);
            if (Recent.Count > 200) Recent.RemoveAt(0);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[analytics] " + line);
#endif
        }
    }

    public sealed class DeviceHaptics : IHaptics
    {
        public bool Enabled = true;
#if UNITY_IOS || UNITY_ANDROID
        public bool Supported => true;
#else
        public bool Supported => false;
#endif

        public void Play(HapticKind kind)
        {
            if (!Enabled || !Supported) return;
#if UNITY_IOS || UNITY_ANDROID
            if (kind != HapticKind.Selection) Handheld.Vibrate();
#endif
        }
    }
}
