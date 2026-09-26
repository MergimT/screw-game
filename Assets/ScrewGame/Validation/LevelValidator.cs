using System;
using System.Collections.Generic;
using ScrewGame.Core;

namespace ScrewGame.Validation
{
    public sealed class ValidationReport
    {
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>Structural and logical checks that do not require search.</summary>
    public static class LevelValidator
    {
        public static ValidationReport Validate(LevelDefinition d)
        {
            var r = new ValidationReport();
            if (d == null) { r.Errors.Add("level is null"); return r; }
            if (string.IsNullOrWhiteSpace(d.Id)) r.Errors.Add("level id is empty");
            if (d.SchemaVersion != LevelDefinition.CurrentSchemaVersion) r.Errors.Add("unsupported schema version " + d.SchemaVersion);
            if (d.TrayPositions < 1) r.Errors.Add("tray positions must be >= 1");
            if (d.TrayCapacity < 1) r.Errors.Add("tray capacity must be >= 1");
            if (d.BufferSlots < 0) r.Errors.Add("buffer slots must be >= 0");
            if (d.TrayQueue == null || d.TrayQueue.Count < Math.Max(RulesDefaults.MinInitialTrayColors, d.TrayPositions))
                r.Errors.Add("tray queue needs at least " + Math.Max(RulesDefaults.MinInitialTrayColors, d.TrayPositions) + " entries");
            if (d.Parts == null || d.Parts.Count == 0) r.Errors.Add("level has no parts");
            if (d.Screws == null || d.Screws.Count == 0) r.Errors.Add("level has no screws");
            if (!r.IsValid) return r;

            foreach (var c in d.TrayQueue)
                if (c < 0 || c >= RulesDefaults.PaletteSize) r.Errors.Add("tray queue color out of range: " + c);

            var parts = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < d.Parts.Count; i++)
            {
                var p = d.Parts[i];
                if (string.IsNullOrWhiteSpace(p.Id)) { r.Errors.Add("part " + i + " has empty id"); continue; }
                if (parts.ContainsKey(p.Id)) r.Errors.Add("duplicate part id " + p.Id);
                else parts.Add(p.Id, i);
            }
            var screwIds = new HashSet<string>(StringComparer.Ordinal);
            var perPart = new int[d.Parts.Count];
            var perColor = new int[RulesDefaults.PaletteSize];
            foreach (var s in d.Screws)
            {
                if (string.IsNullOrWhiteSpace(s.Id)) { r.Errors.Add("screw with empty id"); continue; }
                if (!screwIds.Add(s.Id)) r.Errors.Add("duplicate screw id " + s.Id);
                if (parts.ContainsKey(s.Id)) r.Errors.Add("id used by both part and screw: " + s.Id);
                if (s.Color < 0 || s.Color >= RulesDefaults.PaletteSize) r.Errors.Add("screw " + s.Id + " color out of range " + s.Color);
                else perColor[s.Color]++;
                if (!parts.TryGetValue(s.PartId ?? "", out var pi)) r.Errors.Add("screw " + s.Id + " references unknown part " + s.PartId);
                else perPart[pi]++;
            }
            for (int i = 0; i < d.Parts.Count; i++)
            {
                var p = d.Parts[i];
                if (perPart[i] == 0) r.Errors.Add("part " + p.Id + " has no securing screws");
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var b in p.Blockers ?? new List<string>())
                {
                    if (b == p.Id) r.Errors.Add("part " + p.Id + " blocks itself");
                    else if (!parts.ContainsKey(b)) r.Errors.Add("part " + p.Id + " references unknown blocker " + b);
                    if (!seen.Add(b)) r.Errors.Add("part " + p.Id + " lists blocker twice: " + b);
                }
            }
            var queueColor = new int[RulesDefaults.PaletteSize];
            foreach (var c in d.TrayQueue) if (c >= 0 && c < RulesDefaults.PaletteSize) queueColor[c]++;
            for (int c = 0; c < RulesDefaults.PaletteSize; c++)
            {
                if (perColor[c] != queueColor[c] * d.TrayCapacity)
                    r.Errors.Add("color " + c + ": " + perColor[c] + " screws but tray capacity " + queueColor[c] * d.TrayCapacity);
            }
            if (r.IsValid) FindCycle(d, parts, r);
            return r;
        }

        private static void FindCycle(LevelDefinition d, Dictionary<string, int> parts, ValidationReport r)
        {
            var state = new int[d.Parts.Count];
            for (int i = 0; i < d.Parts.Count; i++)
                if (state[i] == 0 && Visit(i, d, parts, state)) { r.Errors.Add("blocker cycle involving part " + d.Parts[i].Id); return; }
        }

        private static bool Visit(int i, LevelDefinition d, Dictionary<string, int> parts, int[] state)
        {
            state[i] = 1;
            foreach (var b in d.Parts[i].Blockers)
            {
                int j = parts[b];
                if (state[j] == 1) return true;
                if (state[j] == 0 && Visit(j, d, parts, state)) return true;
            }
            state[i] = 2;
            return false;
        }
    }
}
