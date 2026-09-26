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
            Hex(0xffe3b8), Hex(0x5ec8ff), Hex(0x72d65a), Hex(0xff8fc7),
            Hex(0xb69cff), Hex(0xffd84d), Hex(0xff9f43), Hex(0xf7f9fc),
            Hex(0xff5a5a), Hex(0xb9774a), Hex(0x3dd6c6), Hex(0x9aa7b4),
        };

        public static readonly Color Background = Hex(0x55bdf7);
        public static readonly Color BackgroundTop = Hex(0x2f95ee);
        public static readonly Color BackgroundBottom = Hex(0xb9ecff);
        public static readonly Color Hill = Hex(0x8ed96b);
        public static readonly Color HillFar = Hex(0xb5e89a);
        public static readonly Color Card = Hex(0x1f6fc4);
        public static readonly Color Ink = Color.white;
        public static readonly Color InkMuted = new Color(1f, 1f, 1f, 0.7f);
        public static readonly Color Outline = Hex(0x123b6b);
        public static readonly Color Primary = Hex(0xff8a1f);
        public static readonly Color Secondary = Hex(0x3fb950);
        public static readonly Color Accent = Hex(0x8b5cf6);
        public static readonly Color Success = Hex(0x2f9e44);
        public static readonly Color Slot = Hex(0xd7e3ea);
        public static readonly Color Shelf = Hex(0xe8f6ff);
        public static readonly Color BufferBar = Hex(0x2b74d8);
        public static readonly Color Gold = Hex(0xffd23f);
        public static readonly Color Badge = Hex(0xff5f8f);
        public static readonly Color Socket = Hex(0x9fb0bd);
        public static readonly Color SlotInner = Hex(0x8aa2b3);
        public static readonly Color TrayFace = Hex(0xf5f7fa);
        public static readonly Color Hole = Hex(0x3a4750);
        public static readonly Color Wood = Hex(0xb9824f);
        public static readonly Color Steel = Hex(0xc9d3da);

        public static Color ScrewColor(int c) => c >= 0 && c < Screw.Length ? Screw[c] : Color.gray;
    }
}
