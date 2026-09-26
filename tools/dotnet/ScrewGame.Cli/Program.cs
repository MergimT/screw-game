using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ScrewGame.Contracts;
using ScrewGame.Core;
using ScrewGame.Persistence;
using ScrewGame.Validation;

// Batch level pipeline: validate -> hash -> solve -> replay witness, writing a machine-readable report.
// Usage: dotnet run --project tools/dotnet/ScrewGame.Cli -- validate <levelsDir> [reportPath] [budget]
public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 2 || args[0] != "validate")
        {
            Console.Error.WriteLine("usage: validate <levelsDir> [reportPath] [budget]");
            return 2;
        }
        var dir = args[1];
        var reportPath = args.Length > 2 ? args[2] : null;
        int budget = args.Length > 3 ? int.Parse(args[3]) : Solver.DefaultBudget;
        var rows = new List<Dictionary<string, object>>();
        bool allOk = true;
        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            var row = new Dictionary<string, object> { ["file"] = Path.GetFileName(file) };
            rows.Add(row);
            LevelDefinition def;
            try { def = LevelJson.Parse(File.ReadAllText(file)); }
            catch (Exception e) { row["status"] = "PARSE_ERROR"; row["errors"] = new[] { e.Message }; allOk = false; Print(row); continue; }
            row["id"] = def.Id;
            row["family"] = def.Family;
            row["skill"] = def.Meta?.IntroducedSkill;
            row["difficulty"] = def.Meta?.DifficultyHypothesis;
            row["review"] = def.Meta?.ReviewStatus;
            row["screws"] = def.Screws.Count;
            row["colors"] = def.Screws.Select(s => s.Color).Distinct().Count();
            var report = LevelValidator.Validate(def);
            if (!report.IsValid) { row["status"] = "INVALID"; row["errors"] = report.Errors; allOk = false; Print(row); continue; }
            var level = CompiledLevel.Compile(def);
            row["contentHash"] = level.ContentHash;
            row["rulesVersion"] = RulesDefaults.RulesVersion;
            row["solverVersion"] = Solver.Version;
            var sw = Stopwatch.StartNew();
            var result = new Solver(level, budget).Solve();
            row["solveMs"] = sw.ElapsedMilliseconds;
            row["solver"] = result.Outcome.ToString().ToUpperInvariant();
            row["statesExplored"] = result.StatesExplored;
            if (result.Outcome == SolveOutcome.Solved)
            {
                bool replay = Solver.ReplayWins(level, result.Witness);
                row["witness"] = result.Witness;
                row["replay"] = replay ? "WIN" : "FAILED";
                row["status"] = replay ? "LOGIC_OK" : "REPLAY_FAILED";
                allOk &= replay;
            }
            else { row["status"] = "NOT_SOLVED"; allOk = false; }
            row["geometry"] = "PENDING_UNITY";
            Print(row);
        }
        if (reportPath != null)
            File.WriteAllText(reportPath, JsonConvert.SerializeObject(rows, Formatting.Indented) + "\n");
        return allOk ? 0 : 1;
    }

    private static void Print(Dictionary<string, object> row)
    {
        Console.WriteLine(string.Join("  ", new[] { "id", "status", "solver", "statesExplored", "solveMs", "screws" }
            .Where(row.ContainsKey).Select(k => k + "=" + row[k])) + (row.TryGetValue("errors", out var e) ? "  errors=" + string.Join("; ", (IEnumerable<string>)e) : ""));
    }
}
