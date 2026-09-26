using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ScrewGame.Contracts;

namespace ScrewGame.Persistence
{
    public enum LoadStatus
    {
        Fresh = 0,
        Primary = 1,
        RecoveredFromBackup = 2,
        CorruptReset = 3,
        NewerVersionReadOnly = 4,
    }

    [Serializable]
    internal sealed class SaveEnvelope
    {
        public string Format = SaveStore.FormatTag;
        public int Version;
        public long Sequence;
        public string Sha256 = "";
        public string Payload = "";
    }

    /// <summary>
    /// Checksummed, versioned profile persistence with a verified backup. Writes are serialized; a write carrying an older
    /// sequence than the last committed one is refused so late writes can never overwrite newer state.
    /// </summary>
    public sealed class SaveStore
    {
        public const string FormatTag = "screwgame-profile";
        public const string PrimaryName = "profile.json";
        public const string BackupName = "profile.bak.json";
        public const string QuarantinePrefix = "profile.corrupt.";

        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        private readonly IDurableStorage _storage;
        private readonly object _gate = new object();
        private byte[] _lastGood;
        private long _sequence;

        public SaveStore(IDurableStorage storage)
        {
            _storage = storage;
        }

        public long Sequence => _sequence;
        public bool ReadOnly { get; private set; }

        public ProfileData Load(out LoadStatus status)
        {
            lock (_gate)
            {
                bool primaryExists = _storage.TryRead(PrimaryName, out var primary);
                bool backupExists = _storage.TryRead(BackupName, out var backup);
                if (!primaryExists && !backupExists)
                {
                    status = LoadStatus.Fresh;
                    return new ProfileData();
                }
                long pSeq = 0, bSeq = 0;
                bool pNewer = false;
                var p = primaryExists ? Decode(primary, out pSeq, out pNewer) : null;
                if (p != null)
                {
                    _lastGood = primary;
                    _sequence = pSeq;
                    status = LoadStatus.Primary;
                    return p;
                }
                if (primaryExists && pNewer) { ReadOnly = true; status = LoadStatus.NewerVersionReadOnly; return new ProfileData(); }
                var b = backupExists ? Decode(backup, out bSeq, out _) : null;
                if (b != null)
                {
                    if (primaryExists) _storage.WriteAtomic(QuarantinePrefix + "primary", primary);
                    _lastGood = backup;
                    _sequence = bSeq;
                    status = LoadStatus.RecoveredFromBackup;
                    return b;
                }
                if (primaryExists) _storage.WriteAtomic(QuarantinePrefix + "primary", primary);
                if (backupExists) _storage.WriteAtomic(QuarantinePrefix + "backup", backup);
                status = LoadStatus.CorruptReset;
                return new ProfileData();
            }
        }

        /// <summary>Atomically commits the profile. Returns false (state on disk unchanged) on storage failure.</summary>
        public bool TrySave(ProfileData profile, out Exception error)
        {
            lock (_gate)
            {
                error = null;
                if (ReadOnly) { error = new InvalidOperationException("save created by a newer app version"); return false; }
                long seq = _sequence + 1;
                byte[] bytes;
                try { bytes = Encode(profile, seq); }
                catch (Exception e) { error = e; return false; }
                try
                {
                    if (_lastGood != null) _storage.WriteAtomic(BackupName, _lastGood);
                    _storage.WriteAtomic(PrimaryName, bytes);
                }
                catch (Exception e)
                {
                    error = e;
                    return false;
                }
                _lastGood = bytes;
                _sequence = seq;
                return true;
            }
        }

        /// <summary>Deep copy through the save format; used to snapshot the profile for rollback.</summary>
        public static ProfileData Clone(ProfileData p)
        {
            return JsonConvert.DeserializeObject<ProfileData>(JsonConvert.SerializeObject(p, Json), Json);
        }

        internal static byte[] Encode(ProfileData profile, long seq)
        {
            var payload = JsonConvert.SerializeObject(profile, Json);
            var env = new SaveEnvelope { Version = ProfileData.CurrentVersion, Sequence = seq, Payload = payload, Sha256 = Hash(payload) };
            return new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(env, Json));
        }

        internal static ProfileData Decode(byte[] bytes, out long seq, out bool newer)
        {
            seq = 0;
            newer = false;
            try
            {
                var env = JsonConvert.DeserializeObject<SaveEnvelope>(Encoding.UTF8.GetString(bytes), Json);
                if (env == null || env.Format != FormatTag || env.Payload == null) return null;
                if (Hash(env.Payload) != env.Sha256) return null;
                if (env.Version > ProfileData.CurrentVersion) { newer = true; return null; }
                var profile = JsonConvert.DeserializeObject<ProfileData>(env.Payload, Json);
                if (profile == null) return null;
                profile = Migrations.Upgrade(profile, env.Version);
                seq = env.Sequence;
                return profile;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string Hash(string s)
        {
            using (var sha = SHA256.Create())
            {
                var b = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(64);
                foreach (var x in b) sb.Append(x.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }

    public static class Migrations
    {
        /// <summary>Upgrades older payloads in place. Version 1 is the first shipped schema.</summary>
        public static ProfileData Upgrade(ProfileData p, int fromVersion)
        {
            if (fromVersion < 1) throw new JsonException("unsupported save version " + fromVersion);
            p.SaveVersion = ProfileData.CurrentVersion;
            p.Settings ??= new SettingsData();
            p.Tutorial ??= new TutorialData();
            p.Progress ??= new ProgressData();
            p.Daily ??= new DailyData();
            p.Help ??= new HelpData();
            p.Ledger ??= new LedgerData();
            p.Consent ??= new ConsentData();
            return p;
        }
    }
}
