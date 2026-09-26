using System.Collections.Generic;
using NUnit.Framework;
using ScrewGame.Contracts;

namespace ScrewGame.Tests
{
    public class ContractTests
    {
        private sealed class FakeRemote : IRemoteSettings
        {
            public Dictionary<string, int> Ints = new Dictionary<string, int>();
            public int GetInt(string key, int fallback, int min, int max)
            {
                if (!Ints.TryGetValue(key, out var v)) return fallback;
                return v < min ? min : v > max ? max : v;
            }
            public bool GetBool(string key, bool fallback) => fallback;
        }

        [Test]
        public void GameConfig_ClampsRemoteValues_AndKeepsInterstitialsOff()
        {
            var r = new FakeRemote();
            r.Ints["free_undo_per_attempt"] = 99;
            r.Ints["reward_credits_per_ad"] = 5;
            r.Ints["hint_search_budget"] = 1;
            var c = GameConfig.FromRemote(r);
            Assert.AreEqual(3, c.FreeUndoPerAttempt);
            Assert.AreEqual(1, c.RewardCreditsPerAd);
            Assert.AreEqual(10000, c.HintSearchBudget);
            Assert.IsFalse(c.InterstitialsEnabled);
        }

        [Test]
        public void GameConfig_OfflineDefaultsMatchHelpPolicy()
        {
            var c = GameConfig.FromRemote(null);
            Assert.AreEqual(1, c.FreeUndoPerAttempt);
            Assert.AreEqual(1, c.FreeHintPerAttempt);
            Assert.AreEqual(1, c.RewardCreditsPerAd);
        }
    }
}
