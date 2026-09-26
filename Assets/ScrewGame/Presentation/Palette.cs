using UnityEngine;

namespace ScrewGame.Presentation
{
    /// <summary>Screw colors paired with distinct symbols so color is never the only cue.</summary>
    public static class Palette
    {
        private static Color Hex(uint rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

        public static readonly Color[] Screw =
        {
            Hex(0xe53935), // red
            Hex(0x1e88e5), // blue
            Hex(0xfdd835), // yellow
            Hex(0x43a047), // green
            Hex(0x8e24aa), // purple
            Hex(0xfb8c00), // orange
            Hex(0x00acc1), // teal
            Hex(0xec407a), // pink
        };

        public static readonly string[] Symbol = { "●", "■", "▲", "◆", "★", "✚", "⬟", "♥" };
        public static readonly string[] SymbolAscii = { "O", "#", "^", "<>", "*", "+", "5", "v" };

        public static readonly Color[] PartMaterial =
        {
            Hex(0xffcc80), Hex(0x80deea), Hex(0xc5e1a5), Hex(0xf48fb1),
            Hex(0xb39ddb), Hex(0xfff59d), Hex(0x90caf9), Hex(0xffab91),
        };

        public static readonly Color Background = Hex(0x22324a);
        public static readonly Color BackgroundTop = Hex(0x4f6d8f);
        public static readonly Color BackgroundBottom = Hex(0x141e2e);
        public static readonly Color Card = Hex(0x2c3e57);
        public static readonly Color Ink = Color.white;
        public static readonly Color InkMuted = new Color(1f, 1f, 1f, 0.55f);
        public static readonly Color Primary = Hex(0xff7043);
        public static readonly Color Secondary = Hex(0x3b5474);
        public static readonly Color Success = Hex(0x43a047);
        public static readonly Color Slot = Hex(0x455a64);
        public static readonly Color Hole = Hex(0x1b252f);
        public static readonly Color Wood = Hex(0xb9824f);
        public static readonly Color Steel = Hex(0xb0bec5);

        public static Color ScrewColor(int c) => c >= 0 && c < Screw.Length ? Screw[c] : Color.gray;
    }
}
