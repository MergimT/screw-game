using System;
using System.IO;
using ScrewGame.Core;
using ScrewGame.Persistence;

namespace ScrewGame.Tests
{
    /// <summary>Loads bundled level JSON from Assets/ScrewGame/Resources/Levels (Unity or dotnet test output).</summary>
    public static class SampleContent
    {
        public static string LevelsDirectory
        {
            get
            {
                var cwd = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "ScrewGame", "Resources", "Levels");
                if (Directory.Exists(cwd)) return cwd;
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 8 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "Assets", "ScrewGame", "Resources", "Levels");
                    if (Directory.Exists(candidate)) return candidate;
                    var local = Path.Combine(dir.FullName, "Levels");
                    if (Directory.Exists(local)) return local;
                    dir = dir.Parent;
                }
                throw new DirectoryNotFoundException("Level content directory not found");
            }
        }

        public static string[] AllIds()
        {
            var files = Directory.GetFiles(LevelsDirectory, "L*.json");
            Array.Sort(files, StringComparer.Ordinal);
            var ids = new string[files.Length];
            for (int i = 0; i < files.Length; i++) ids[i] = Path.GetFileNameWithoutExtension(files[i]);
            return ids;
        }

        public static LevelDefinition Load(string id)
        {
            return LevelJson.Parse(File.ReadAllText(Path.Combine(LevelsDirectory, id + ".json")));
        }
    }
}
