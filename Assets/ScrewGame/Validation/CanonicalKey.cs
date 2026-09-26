using System;
using System.Text;
using ScrewGame.Core;

namespace ScrewGame.Validation
{
    /// <summary>
    /// Logical equivalence key. Screws already in trays or the buffer are interchangeable with same-colored screws because
    /// routing, transfer and completion depend only on color and insertion order, so the key records attached screws by
    /// index, tray colors/fills, queue cursor and buffered colors in insertion order.
    /// </summary>
    public static class CanonicalKey
    {
        public static string Of(CompiledLevel level, PuzzleState st)
        {
            var sb = new StringBuilder(level.ScrewCount + 32);
            for (int s = 0; s < level.ScrewCount; s++) sb.Append(st.Place[s] == ScrewPlace.OnPart ? '1' : '0');
            sb.Append('|');
            for (int t = 0; t < st.TrayColor.Length; t++) sb.Append((char)('a' + st.TrayColor[t] + 1)).Append((char)('0' + st.TrayCount[t]));
            sb.Append('|').Append(st.QueueCursor).Append('|');
            int n = 0;
            var order = new long[st.Buffer.Length];
            var colors = new int[st.Buffer.Length];
            foreach (var s in st.Buffer)
            {
                if (s < 0) continue;
                order[n] = st.Seq[s];
                colors[n] = level.ScrewColor[s];
                n++;
            }
            Array.Sort(order, colors, 0, n);
            for (int i = 0; i < n; i++) sb.Append((char)('a' + colors[i]));
            return sb.ToString();
        }
    }
}
