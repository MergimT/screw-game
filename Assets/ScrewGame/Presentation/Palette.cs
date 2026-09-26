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
            Hex(0xffe0b2), Hex(0x8fd3fe), Hex(0xb5e48c), Hex(0xffafcc),
            Hex(0xcdb4f6), Hex(0xffe066), Hex(0xf4a261), Hex(0xf1f3f5),
        };

        public static readonly Color Background = Hex(0x55bdf7);
        public static readonly Color BackgroundTop = Hex(0x2f95ee);
        public static readonly Color BackgroundBottom = Hex(0xb9ecff);
        public static readonly Color Card = Hex(0x1f6fc4);
        public static readonly Color Ink = Color.white;
        public static readonly Color InkMuted = new Color(1f, 1f, 1f, 0.7f);
        public static readonly Color Outline = Hex(0x123b6b);
        public static readonly Color Primary = Hex(0xff8a1f);
        public static readonly Color Secondary = Hex(0x3fb950);
        public static readonly Color Accent = Hex(0x8b5cf6);
        public static readonly Color Success = Hex(0x2f9e44);
        public static readonly Color Slot = Hex(0xd7e3ea);
        public static readonly Color SlotInner = Hex(0x8aa2b3);
        public static readonly Color TrayFace = Hex(0xf5f7fa);
        public static readonly Color Hole = Hex(0x3a4750);
        public static readonly Color Wood = Hex(0xb9824f);
        public static readonly Color Steel = Hex(0xc9d3da);

        public static Color ScrewColor(int c) => c >= 0 && c < Screw.Length ? Screw[c] : Color.gray;
    }
}
