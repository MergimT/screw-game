using System.Collections.Generic;

namespace ScrewGame.Contracts
{
    public sealed class CommandResult
    {
        public static readonly IReadOnlyList<VisualEvent> NoEvents = new VisualEvent[0];

        public CommandStatus Status;
        public RejectReason Reason;
        public long Revision;
        public IReadOnlyList<VisualEvent> Events = NoEvents;

        public bool Accepted => Status == CommandStatus.Accepted;

        public static CommandResult Reject(RejectReason reason, long revision)
        {
            return new CommandResult { Status = CommandStatus.Rejected, Reason = reason, Revision = revision };
        }

        public static CommandResult Accept(long revision, IReadOnlyList<VisualEvent> events)
        {
            return new CommandResult { Status = CommandStatus.Accepted, Reason = RejectReason.None, Revision = revision, Events = events };
        }
    }

    /// <summary>Hint bound to the attempt, session revision and content hash it was computed for.</summary>
    public sealed class HintResult
    {
        public HintStatus Status;
        public string ScrewId;
        public string AttemptId;
        public long Revision;
        public string ContentHash;
        public int StatesExplored;
    }

    public sealed class HelpBalance
    {
        public int FreeUndoRemaining;
        public int FreeHintRemaining;
        public int BankedCredits;

        public int Available(HelpKind kind)
        {
            return (kind == HelpKind.Undo ? FreeUndoRemaining : FreeHintRemaining) + BankedCredits;
        }
    }
}
