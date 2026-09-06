namespace Firesight.Infrastructure.Persistence.Sync;

public sealed class CwfisSyncState
{
    public const string SourceKey = "CWFIS";

    public string Source { get; set; } = SourceKey;
    public DateTime LastAttemptUtc { get; set; }
    public DateTime? LastSuccessfulFetchUtc { get; set; }
    public bool LastAttemptSucceeded { get; set; }
    public int Received { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    public string? LastError { get; set; }
}
