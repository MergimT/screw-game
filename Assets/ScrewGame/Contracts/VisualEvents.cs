namespace ScrewGame.Contracts
{
    /// <summary>Ordered presentation event produced by an accepted command. Logical state is already committed.</summary>
    public abstract class VisualEvent
    {
    }

    public sealed class ScrewRemovedEvent : VisualEvent
    {
        public string ScrewId;
        public DestinationKind Destination;
        public int DestinationIndex;
    }

    public sealed class PartReleasedEvent : VisualEvent
    {
        public string PartId;
    }

    public sealed class TrayCompletedEvent : VisualEvent
    {
        public int TrayIndex;
        public int Color;
    }

    /// <summary>A tray position received the next queued color. Color is -1 when the queue is exhausted and the position stays inactive.</summary>
    public sealed class TrayRefilledEvent : VisualEvent
    {
        public int TrayIndex;
        public int Color;
    }

    public sealed class BufferTransferredEvent : VisualEvent
    {
        public string ScrewId;
        public int FromSlot;
        public int ToTray;
    }

    public sealed class OutcomeChangedEvent : VisualEvent
    {
        public Outcome Outcome;
    }

    /// <summary>The whole view must be rebuilt from the settled state (undo, restart, restore).</summary>
    public sealed class StateReplacedEvent : VisualEvent
    {
    }
}
