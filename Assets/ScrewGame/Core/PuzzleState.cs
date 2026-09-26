using System;

namespace ScrewGame.Core
{
    public enum ScrewPlace : byte
    {
        OnPart = 0,
        Buffer = 1,
        Tray = 2,
        Collected = 3,
    }

    /// <summary>Complete mutable puzzle payload. Excludes session revision, help, purchases and progression.</summary>
    [Serializable]
    public sealed class PuzzleState
    {
        public ScrewPlace[] Place;
        /// <summary>Tray position or buffer slot for screws in a tray or buffer; -1 otherwise.</summary>
        public int[] Slot;
        /// <summary>Buffer insertion sequence for buffered screws; -1 otherwise.</summary>
        public long[] Seq;
        public bool[] Released;
        /// <summary>Color per tray position, -1 when inactive.</summary>
        public int[] TrayColor;
        public int[] TrayCount;
        /// <summary>Screw index per buffer slot, -1 when empty.</summary>
        public int[] Buffer;
        public int QueueCursor;
        public long NextSeq;
        public int CompletedTrays;
        public int Outcome;

        public PuzzleState Clone()
        {
            return new PuzzleState
            {
                Place = (ScrewPlace[])Place.Clone(),
                Slot = (int[])Slot.Clone(),
                Seq = (long[])Seq.Clone(),
                Released = (bool[])Released.Clone(),
                TrayColor = (int[])TrayColor.Clone(),
                TrayCount = (int[])TrayCount.Clone(),
                Buffer = (int[])Buffer.Clone(),
                QueueCursor = QueueCursor,
                NextSeq = NextSeq,
                CompletedTrays = CompletedTrays,
                Outcome = Outcome,
            };
        }

        public bool ContentEquals(PuzzleState o)
        {
            return o != null
                && ArrEq(Place, o.Place) && ArrEq(Slot, o.Slot) && ArrEq(Seq, o.Seq) && ArrEq(Released, o.Released)
                && ArrEq(TrayColor, o.TrayColor) && ArrEq(TrayCount, o.TrayCount) && ArrEq(Buffer, o.Buffer)
                && QueueCursor == o.QueueCursor && NextSeq == o.NextSeq && CompletedTrays == o.CompletedTrays && Outcome == o.Outcome;
        }

        private static bool ArrEq<T>(T[] a, T[] b)
        {
            if (a == null || b == null) return a == b;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (!a[i].Equals(b[i])) return false;
            return true;
        }
    }
}
