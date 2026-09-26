using System.Collections.Generic;
using ScrewGame.Core;

namespace ScrewGame.Tests
{
    /// <summary>Compact builder for rule fixtures: parts are (id, blockers, screw colors).</summary>
    public static class TestLevels
    {
        public const int A = 0, B = 1, C = 2, D = 3;

        public static LevelDefinition Build(string id, int[] queue, params (string id, string[] blockers, int[] colors)[] parts)
        {
            var d = new LevelDefinition { Id = id, Name = id, TrayQueue = new List<int>(queue) };
            int n = 0;
            foreach (var p in parts)
            {
                d.Parts.Add(new PartDefinition { Id = p.id, Blockers = new List<string>(p.blockers ?? new string[0]) });
                foreach (var c in p.colors)
                    d.Screws.Add(new ScrewDefinition { Id = "s" + n++, PartId = p.id, Color = c });
            }
            return d;
        }

        public static (string, string[], int[]) P(string id, int[] colors, params string[] blockers) => (id, blockers, colors);

        /// <summary>Three independent plates, two colors, solvable directly.</summary>
        public static LevelDefinition Simple() => Build("simple", new[] { A, B },
            P("p0", new[] { A, B, A }), P("p1", new[] { B, A, B }));

        /// <summary>Chain p2 blocked by p1 blocked by p0.</summary>
        public static LevelDefinition Chain() => Build("chain", new[] { A, B, C },
            P("p0", new[] { C, C }), P("p1", new[] { A, B, C }, "p0"), P("p2", new[] { A, A, B, B }, "p1"));
    }
}
