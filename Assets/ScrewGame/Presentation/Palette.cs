using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>Screw colors paired with distinct symbols so color is never the only cue.</summary>
    public static class Palette
    {
        public static readonly Color[] Screw =
        {
            new Color(0.89f, 0.23f, 0.25f), // red
            new Color(0.20f, 0.45f, 0.90f), // blue
            new Color(0.98f, 0.78f, 0.15f), // yellow
            new Color(0.22f, 0.70f, 0.35f), // green
            new Color(0.58f, 0.34f, 0.85f), // purple
            new Color(0.98f, 0.52f, 0.14f), // orange
            new Color(0.10f, 0.72f, 0.72f), // teal
            new Color(0.95f, 0.45f, 0.70f), // pink
        };

        public static readonly string[] Symbol = { "●", "■", "▲", "◆", "★", "✚", "⬟", "♥" };
        public static readonly string[] SymbolAscii = { "O", "#", "^", "<>", "*", "+", "5", "v" };

        public static readonly Color[] PartMaterial =
        {
            new Color(0.80f, 0.66f, 0.50f), // light wood
            new Color(0.62f, 0.47f, 0.34f), // dark wood
            new Color(0.70f, 0.74f, 0.78f), // steel
            new Color(0.93f, 0.90f, 0.84f), // cream
            new Color(0.55f, 0.72f, 0.80f), // pale blue
            new Color(0.85f, 0.60f, 0.55f), // terracotta
            new Color(0.75f, 0.80f, 0.62f), // sage
            new Color(0.90f, 0.82f, 0.60f), // brass
        };

        public static readonly Color Background = new Color(0.96f, 0.93f, 0.88f);
        public static readonly Color Ink = new Color(0.16f, 0.14f, 0.13f);
        public static readonly Color Slot = new Color(0.86f, 0.82f, 0.76f);

        public static Color ScrewColor(int c) => c >= 0 && c < Screw.Length ? Screw[c] : Color.gray;
    }
}
