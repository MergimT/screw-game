using System;
using System.Collections.Generic;

namespace ScrewGame.Core
{
    /// <summary>Index-based view of a structurally valid LevelDefinition used by the rules engine.</summary>
    public sealed class CompiledLevel
    {
        public readonly LevelDefinition Definition;
        public readonly string ContentHash;
        public readonly int ScrewCount;
        public readonly int PartCount;
        public readonly string[] ScrewIds;
        public readonly string[] PartIds;
        public readonly int[] ScrewColor;
        public readonly int[] ScrewPart;
        public readonly int[][] PartScrews;
        public readonly int[][] PartBlockers;
        public readonly int[] Queue;
        public readonly int TrayPositions;
        public readonly int TrayCapacity;
        public readonly int BufferSlots;
        private readonly Dictionary<string, int> _screwIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _partIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        private CompiledLevel(LevelDefinition def)
        {
            Definition = def;
            ContentHash = LevelHasher.Compute(def);
            TrayPositions = def.TrayPositions;
            TrayCapacity = def.TrayCapacity;
            BufferSlots = def.BufferSlots;
            Queue = def.TrayQueue.ToArray();
            PartCount = def.Parts.Count;
            ScrewCount = def.Screws.Count;
            PartIds = new string[PartCount];
            for (int p = 0; p < PartCount; p++)
            {
                PartIds[p] = def.Parts[p].Id;
                _partIndex.Add(def.Parts[p].Id, p);
            }

            ScrewIds = new string[ScrewCount];
            ScrewColor = new int[ScrewCount];
            ScrewPart = new int[ScrewCount];
            var partScrews = new List<int>[PartCount];
            for (int p = 0; p < PartCount; p++) partScrews[p] = new List<int>();
            for (int s = 0; s < ScrewCount; s++)
            {
                var sd = def.Screws[s];
                ScrewIds[s] = sd.Id;
                ScrewColor[s] = sd.Color;
                _screwIndex.Add(sd.Id, s);
                if (!_partIndex.TryGetValue(sd.PartId, out var p))
                    throw new ArgumentException("Screw " + sd.Id + " references unknown part " + sd.PartId);
                ScrewPart[s] = p;
                partScrews[p].Add(s);
            }

            PartScrews = new int[PartCount][];
            PartBlockers = new int[PartCount][];
            for (int p = 0; p < PartCount; p++)
            {
                PartScrews[p] = partScrews[p].ToArray();
                var blockers = def.Parts[p].Blockers;
                PartBlockers[p] = new int[blockers.Count];
                for (int b = 0; b < blockers.Count; b++)
                {
                    if (!_partIndex.TryGetValue(blockers[b], out var bi))
                        throw new ArgumentException("Part " + PartIds[p] + " references unknown blocker " + blockers[b]);
                    PartBlockers[p][b] = bi;
                }
            }
        }

        /// <summary>Compiles a level. Throws ArgumentException for broken references; run LevelValidator first for full diagnostics.</summary>
        public static CompiledLevel Compile(LevelDefinition def)
        {
            if (def == null) throw new ArgumentNullException(nameof(def));
            return new CompiledLevel(def);
        }

        public bool TryGetScrew(string id, out int index)
        {
            if (id == null) { index = -1; return false; }
            return _screwIndex.TryGetValue(id, out index);
        }

        public bool TryGetPart(string id, out int index)
        {
            if (id == null) { index = -1; return false; }
            return _partIndex.TryGetValue(id, out index);
        }
    }
}
