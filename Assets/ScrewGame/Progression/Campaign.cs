using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ScrewGame.Persistence;

namespace ScrewGame.Progression
{
    public sealed class CampaignEntry
    {
        public string LevelId;
        public string ObjectId;
        public string Name;
        public string Family;
    }

    /// <summary>Ordered campaign; first victory unlocks the next level and records the collection object. All operations are idempotent.</summary>
    public sealed class Campaign
    {
        public readonly IReadOnlyList<CampaignEntry> Entries;

        public Campaign(IReadOnlyList<CampaignEntry> entries)
        {
            Entries = entries;
        }

        public int IndexOf(string levelId)
        {
            for (int i = 0; i < Entries.Count; i++) if (Entries[i].LevelId == levelId) return i;
            return -1;
        }

        public bool IsUnlocked(ProfileData p, string levelId)
        {
            int i = IndexOf(levelId);
            if (i < 0) return false;
            return i == 0 || p.Progress.CompletedLevels.Contains(Entries[i - 1].LevelId) || p.Progress.CompletedLevels.Contains(levelId);
        }

        public bool IsCompleted(ProfileData p, string levelId) => p.Progress.CompletedLevels.Contains(levelId);

        /// <summary>Next level to play: first uncompleted unlocked entry, or null when the campaign is finished.</summary>
        public CampaignEntry Current(ProfileData p)
        {
            foreach (var e in Entries) if (!p.Progress.CompletedLevels.Contains(e.LevelId)) return e;
            return null;
        }

        public CampaignEntry Next(string levelId)
        {
            int i = IndexOf(levelId);
            return i >= 0 && i + 1 < Entries.Count ? Entries[i + 1] : null;
        }

        /// <summary>Applies a campaign victory. Returns true only the first time this level is won.</summary>
        public bool RecordVictory(ProfileData p, string levelId)
        {
            int i = IndexOf(levelId);
            if (i < 0 || p.Progress.CompletedLevels.Contains(levelId)) return false;
            p.Progress.CompletedLevels.Add(levelId);
            var obj = Entries[i].ObjectId;
            if (!string.IsNullOrEmpty(obj) && !p.Progress.CollectedObjects.Contains(obj)) p.Progress.CollectedObjects.Add(obj);
            p.Progress.Wins++;
            return true;
        }
    }

    /// <summary>Deterministic UTC daily challenge drawn from prevalidated bundled levels and pinned per date.</summary>
    public static class DailyChallenge
    {
        public static string DateKey(DateTime utc) => utc.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public static string Select(string dateKey, IReadOnlyList<string> pool, int contentVersion)
        {
            if (pool == null || pool.Count == 0) return null;
            using (var sha = SHA256.Create())
            {
                var h = sha.ComputeHash(Encoding.UTF8.GetBytes("daily|" + contentVersion + "|" + dateKey));
                uint v = (uint)(h[0] | h[1] << 8 | h[2] << 16 | h[3] << 24);
                return pool[(int)(v % (uint)pool.Count)];
            }
        }

        /// <summary>Returns the level pinned for the date, pinning it on first request. Clock rollback reuses an existing pin.</summary>
        public static string Resolve(ProfileData p, string dateKey, IReadOnlyList<string> pool, int contentVersion)
        {
            if (p.Daily.Pinned.TryGetValue(dateKey, out var pinned) && Contains(pool, pinned)) return pinned;
            var chosen = Select(dateKey, pool, contentVersion);
            if (chosen != null) p.Daily.Pinned[dateKey] = chosen;
            return chosen;
        }

        public static bool IsCompleted(ProfileData p, string dateKey) => p.Daily.CompletedDates.Contains(dateKey);

        /// <summary>Records completion for the date the attempt was started on (midnight-safe). Returns true only the first time.</summary>
        public static bool RecordCompletion(ProfileData p, string dateKey)
        {
            if (string.IsNullOrEmpty(dateKey) || p.Daily.CompletedDates.Contains(dateKey)) return false;
            p.Daily.CompletedDates.Add(dateKey);
            return true;
        }

        private static bool Contains(IReadOnlyList<string> pool, string id)
        {
            foreach (var x in pool) if (x == id) return true;
            return false;
        }
    }
}
