using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ScrewGame.Core
{
    /// <summary>Deterministic SHA-256 over every rules- and geometry-relevant field of a level.</summary>
    public static class LevelHasher
    {
        public static string Compute(LevelDefinition d)
        {
            var sb = new StringBuilder(1024);
            sb.Append("schema=").Append(d.SchemaVersion).Append(";rules=").Append(RulesDefaults.RulesVersion)
              .Append(";id=").Append(d.Id).Append(";cv=").Append(d.ContentVersion)
              .Append(";t=").Append(d.TrayPositions).Append(',').Append(d.TrayCapacity).Append(',').Append(d.BufferSlots)
              .Append(";q=");
            foreach (var c in d.TrayQueue) sb.Append(c).Append(',');
            sb.Append(";parts=");
            foreach (var p in d.Parts)
            {
                sb.Append(p.Id).Append('[');
                foreach (var b in p.Blockers) sb.Append(b).Append(',');
                sb.Append(']').Append(p.Shape).Append('|');
                Vec(sb, p.Position); Vec(sb, p.Rotation); Vec(sb, p.Size);
                sb.Append(p.Material).Append(';');
            }
            sb.Append(";screws=");
            foreach (var s in d.Screws)
            {
                sb.Append(s.Id).Append('@').Append(s.PartId).Append('#').Append(s.Color).Append('|');
                Vec(sb, s.Position); Vec(sb, s.Normal);
                sb.Append(';');
            }
            var cam = d.Camera ?? new CameraRange();
            sb.Append(";cam=");
            Num(sb, cam.MinYaw); Num(sb, cam.MaxYaw); Num(sb, cam.MinPitch); Num(sb, cam.MaxPitch); Num(sb, cam.Distance);

            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                var hex = new StringBuilder(64);
                foreach (var b in bytes) hex.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return hex.ToString();
            }
        }

        private static void Vec(StringBuilder sb, float[] v)
        {
            if (v == null) { sb.Append("null|"); return; }
            foreach (var f in v) Num(sb, f);
            sb.Append('|');
        }

        private static void Num(StringBuilder sb, float f)
        {
            sb.Append(System.Math.Round(f, 4).ToString("0.####", CultureInfo.InvariantCulture)).Append(',');
        }
    }
}
