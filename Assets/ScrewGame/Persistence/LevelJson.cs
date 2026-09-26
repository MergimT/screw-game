using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ScrewGame.Core;

namespace ScrewGame.Persistence
{
    /// <summary>Deterministic level JSON import/export (field order follows the LevelDefinition declaration).</summary>
    public static class LevelJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Error,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
        };

        public static LevelDefinition Parse(string json)
        {
            return JsonConvert.DeserializeObject<LevelDefinition>(json, Settings);
        }

        public static string Write(LevelDefinition level)
        {
            return JsonConvert.SerializeObject(level, Settings).Replace("\r\n", "\n") + "\n";
        }

        public static List<LevelDefinition> ParseAll(IEnumerable<string> jsons)
        {
            var list = new List<LevelDefinition>();
            foreach (var j in jsons) list.Add(Parse(j));
            return list;
        }

        public static byte[] Utf8(string s) => new UTF8Encoding(false).GetBytes(s);
    }
}
