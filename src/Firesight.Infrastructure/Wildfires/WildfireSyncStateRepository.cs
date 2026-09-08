using Firesight.Application.Wildfires;
using Firesight.Infrastructure.Persistence;
using Firesight.Infrastructure.Persistence.Sync;
using Microsoft.EntityFrameworkCore;

namespace Firesight.Infrastructure.Wildfires;

public sealed class WildfireSyncStateRepository(FiresightDbContext dbContext)
    : IWildfireSyncStateRepository
{
    public async Task<WildfireFeedSyncStateDto?> GetAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CwfisSyncStates
            .AsNoTracking()
            .Where(state => state.Source == CwfisSyncState.SourceKey)
            .Select(state => new WildfireFeedSyncStateDto(
                state.LastAttemptUtc,
                state.LastSuccessfulFetchUtc,
                state.LastAttemptSucceeded,
                state.Received,
                state.Accepted,
                state.Rejected))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RecordFetchSuccessAsync(
        DateTime attemptUtc,
        DateTime successfulFetchUtc,
        int received,
        int accepted,
        int rejected,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(cancellationToken);
        state.LastAttemptUtc = attemptUtc;
        state.LastSuccessfulFetchUtc = successfulFetchUtc;
        state.LastAttemptSucceeded = false;
        state.Received = received;
        state.Accepted = accepted;
        state.Rejected = rejected;
        state.LastError = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordAttemptSuccessAsync(
        DateTime attemptUtc,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(cancellationToken);
        state.LastAttemptUtc = attemptUtc;
        state.LastAttemptSucceeded = true;
        state.LastError = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordFailureAsync(
        DateTime attemptUtc,
        string error,
        CancellationToken cancellationToken = default)
    {
        var state = await GetOrCreateAsync(cancellationToken);
        state.LastAttemptUtc = attemptUtc;
        state.LastAttemptSucceeded = false;
        state.LastError = error.Length <= 2000 ? error : error[..2000];

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CwfisSyncState> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var state = await dbContext.CwfisSyncStates
            .SingleOrDefaultAsync(
                item => item.Source == CwfisSyncState.SourceKey,
                cancellationToken);

        if (state is not null)
        {
            return state;
        }

        state = new CwfisSyncState();
        dbContext.CwfisSyncStates.Add(state);
        return state;
    }
}
