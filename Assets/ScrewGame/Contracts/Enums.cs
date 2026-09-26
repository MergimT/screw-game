namespace ScrewGame.Contracts
{
    public enum Outcome
    {
        Playing = 0,
        Won = 1,
        Lost = 2,
    }

    public enum CommandStatus
    {
        Accepted = 0,
        Rejected = 1,
    }

    public enum RejectReason
    {
        None = 0,
        StaleRevision,
        UnknownScrew,
        NotOnObject,
        Blocked,
        NoDestination,
        AttemptClosed,
        NothingToUndo,
        NoHelpCredit,
        Busy,
        SaveFailed,
        NotLost,
    }

    public enum DestinationKind
    {
        Tray = 0,
        Buffer = 1,
    }

    public enum HelpKind
    {
        Undo = 0,
        Hint = 1,
    }

    public enum HintStatus
    {
        Suggested = 0,
        Unsolvable = 1,
        Inconclusive = 2,
        Stale = 3,
    }

    public enum SolveOutcome
    {
        Solved = 0,
        Unsolvable = 1,
        Inconclusive = 2,
        Cancelled = 3,
    }
}
