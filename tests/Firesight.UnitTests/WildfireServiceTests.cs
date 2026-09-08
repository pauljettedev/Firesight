using Firesight.Application.Common;
using Firesight.Application.Wildfires;

namespace Firesight.UnitTests;

public sealed class WildfireServiceTests
{
    [Fact]
    public async Task RefreshAsync_SynchronizesSourceRecordsAndRecordsSuccessfulAttempt()
    {
        var records = new[]
        {
            new WildfireImportRecord(
                "cwfis:test-1",
                "ON",
                "Test Fire",
                46.5,
                -80.5,
                new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
                1250,
                "OC",
                new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc))
        };

        var source = new StubSource(new WildfireSourceResult(records, 2, 1, 1));
        var repository = new StubRepository((1, 0, 0));
        var syncStateRepository = new StubSyncStateRepository();
        var service = new WildfireService(source, repository, syncStateRepository);

        var result = await service.RefreshAsync();

        Assert.Equal(2, result.Received);
        Assert.Equal(1, result.Accepted);
        Assert.Equal(1, result.Rejected);
        Assert.Equal(1, result.Inserted);
        Assert.Equal(0, result.Changed);
        Assert.Equal(0, result.Observed);
        Assert.Same(records, repository.SynchronizedRecords);
        Assert.True(syncStateRepository.RecordedFetchSuccess);
        Assert.True(syncStateRepository.RecordedAttemptSuccess);
        Assert.False(syncStateRepository.RecordedFailure);
    }

    [Fact]
    public async Task RefreshAsync_WhenSourceFails_RecordsFailedFetchAndRethrows()
    {
        var source = new ThrowingSource(new InvalidOperationException("CWFIS unavailable"));
        var repository = new StubRepository((0, 0, 0));
        var syncStateRepository = new StubSyncStateRepository();
        var service = new WildfireService(source, repository, syncStateRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshAsync());

        Assert.Equal("CWFIS unavailable", exception.Message);
        Assert.True(syncStateRepository.RecordedFailure);
        Assert.False(syncStateRepository.RecordedFetchSuccess);
        Assert.False(syncStateRepository.RecordedAttemptSuccess);
        Assert.Null(repository.SynchronizedRecords);
    }

    [Fact]
    public async Task RefreshAsync_WhenRepositorySynchronizationFails_RecordsFailedAttemptAndRethrows()
    {
        var source = new StubSource(new WildfireSourceResult([], 0, 0, 0));
        var repository = new ThrowingRepository(new InvalidOperationException("database unavailable"));
        var syncStateRepository = new StubSyncStateRepository();
        var service = new WildfireService(source, repository, syncStateRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RefreshAsync());

        Assert.Equal("database unavailable", exception.Message);
        Assert.True(syncStateRepository.RecordedFetchSuccess);
        Assert.False(syncStateRepository.RecordedAttemptSuccess);
        Assert.True(syncStateRepository.RecordedFailure);
    }

    [Fact]
    public async Task GetActiveWildfiresAsync_ReturnsRepositoryData()
    {
        var expected = new[]
        {
            new WildfireDto(
                Guid.NewGuid(),
                "cwfis:test-1",
                "ON",
                "Test Fire",
                46.5,
                -80.5,
                null,
                1250,
                "OC",
                new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc),
                DateTime.UtcNow,
                false)
        };

        var repository = new StubRepository((0, 0, 0), expected);
        var service = new WildfireService(
            new StubSource(new WildfireSourceResult([], 0, 0, 0)),
            repository,
            new StubSyncStateRepository());

        var result = await service.GetActiveWildfiresAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetWildfireByExternalIdAsync_ReturnsRepositoryData()
    {
        var expected = new WildfireDto(
            Guid.NewGuid(),
            "2026_ON_THU_FIRE_036",
            "ON",
            null,
            50.6828,
            -89.468,
            null,
            301240,
            "BH",
            new DateTime(2026, 9, 4, 16, 0, 0, DateTimeKind.Utc),
            DateTime.UtcNow,
            false);

        var repository = new StubRepository((0, 0, 0), wildfireById: expected);
        var service = new WildfireService(
            new StubSource(new WildfireSourceResult([], 0, 0, 0)),
            repository,
            new StubSyncStateRepository());

        var result = await service.GetWildfireByExternalIdAsync(expected.ExternalId);

        Assert.Same(expected, result);
        Assert.Equal(expected.ExternalId, repository.RequestedExternalId);
    }

    [Fact]
    public async Task FindWildfiresNearAsync_ForwardsValidatedQueryToRepository()
    {
        IReadOnlyList<NearbyWildfireDto> expected = [];
        var repository = new StubRepository((0, 0, 0), nearbyWildfires: expected);
        var service = new WildfireService(
            new StubSource(new WildfireSourceResult([], 0, 0, 0)),
            repository,
            new StubSyncStateRepository());

        var result = await service.FindWildfiresNearAsync(45.4215, -75.6972, 100);

        Assert.Same(expected, result);
        Assert.Equal(45.4215, repository.RequestedLatitude);
        Assert.Equal(-75.6972, repository.RequestedLongitude);
        Assert.Equal(100, repository.RequestedRadiusKm);
    }

    [Theory]
    [InlineData(-91, 0, 10, "latitude")]
    [InlineData(91, 0, 10, "latitude")]
    [InlineData(0, -181, 10, "longitude")]
    [InlineData(0, 181, 10, "longitude")]
    [InlineData(0, 0, 0, "radiusKm")]
    [InlineData(0, 0, -1, "radiusKm")]
    [InlineData(double.NaN, 0, 10, "latitude")]
    [InlineData(double.PositiveInfinity, 0, 10, "latitude")]
    [InlineData(0, double.NaN, 10, "longitude")]
    [InlineData(0, double.NegativeInfinity, 10, "longitude")]
    [InlineData(0, 0, double.NaN, "radiusKm")]
    [InlineData(0, 0, double.PositiveInfinity, "radiusKm")]
    public async Task FindWildfiresNearAsync_InvalidQuery_ThrowsValidationException(
        double latitude,
        double longitude,
        double radiusKm,
        string expectedField)
    {
        var service = new WildfireService(
            new StubSource(new WildfireSourceResult([], 0, 0, 0)),
            new StubRepository((0, 0, 0)),
            new StubSyncStateRepository());

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => service.FindWildfiresNearAsync(latitude, longitude, radiusKm));

        Assert.Contains(expectedField, exception.Errors.Keys);
    }

    [Fact]
    public async Task FindWildfiresNearAsync_MultipleInvalidValues_ReturnsAllValidationErrors()
    {
        var service = new WildfireService(
            new StubSource(new WildfireSourceResult([], 0, 0, 0)),
            new StubRepository((0, 0, 0)),
            new StubSyncStateRepository());

        var exception = await Assert.ThrowsAsync<ApplicationValidationException>(
            () => service.FindWildfiresNearAsync(double.NaN, 181, 0));

        Assert.Equal(3, exception.Errors.Count);
        Assert.Contains("latitude", exception.Errors.Keys);
        Assert.Contains("longitude", exception.Errors.Keys);
        Assert.Contains("radiusKm", exception.Errors.Keys);
    }

    [Fact]
    public async Task RefreshAsync_SerializesConcurrentRefreshesAcrossServiceInstances()
    {
        var firstSource = new BlockingSource();
        var secondSource = new SignalingSource();

        var firstService = new WildfireService(
            firstSource,
            new StubRepository((0, 0, 0)),
            new StubSyncStateRepository());

        var secondService = new WildfireService(
            secondSource,
            new StubRepository((0, 0, 0)),
            new StubSyncStateRepository());

        var firstRefresh = firstService.RefreshAsync();
        await firstSource.Started.Task;

        var secondRefresh = secondService.RefreshAsync();

        Assert.False(secondSource.Started.Task.IsCompleted);

        firstSource.Release.SetResult();

        await firstRefresh;
        await secondRefresh;

        Assert.True(secondSource.Started.Task.IsCompleted);
    }

    private sealed class BlockingSource : IWildfireSource
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<WildfireSourceResult> GetActiveWildfiresAsync(
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new WildfireSourceResult([], 0, 0, 0);
        }
    }

    private sealed class SignalingSource : IWildfireSource
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<WildfireSourceResult> GetActiveWildfiresAsync(
            CancellationToken cancellationToken = default)
        {
            Started.SetResult();
            return Task.FromResult(new WildfireSourceResult([], 0, 0, 0));
        }
    }

    private sealed class StubSource(WildfireSourceResult result) : IWildfireSource
    {
        public Task<WildfireSourceResult> GetActiveWildfiresAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(result);
    }

    private sealed class ThrowingSource(Exception exception) : IWildfireSource
    {
        public Task<WildfireSourceResult> GetActiveWildfiresAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromException<WildfireSourceResult>(exception);
    }

    private sealed class StubRepository(
        (int Inserted, int Changed, int Observed) syncResult,
        IReadOnlyList<WildfireDto>? wildfires = null,
        WildfireDto? wildfireById = null,
        IReadOnlyList<NearbyWildfireDto>? nearbyWildfires = null) : IWildfireRepository
    {
        public IReadOnlyCollection<WildfireImportRecord>? SynchronizedRecords { get; private set; }
        public string? RequestedExternalId { get; private set; }
        public double? RequestedLatitude { get; private set; }
        public double? RequestedLongitude { get; private set; }
        public double? RequestedRadiusKm { get; private set; }

        public Task<IReadOnlyList<WildfireDto>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(wildfires ?? []);

        public Task<WildfireDto?> GetByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default)
        {
            RequestedExternalId = externalId;
            return Task.FromResult(wildfireById);
        }

        public Task<IReadOnlyList<NearbyWildfireDto>> FindNearAsync(
            double latitude,
            double longitude,
            double radiusKm,
            CancellationToken cancellationToken = default)
        {
            RequestedLatitude = latitude;
            RequestedLongitude = longitude;
            RequestedRadiusKm = radiusKm;
            return Task.FromResult(nearbyWildfires ?? []);
        }

        public Task<(int Inserted, int Changed, int Observed)> SynchronizeAsync(
            IReadOnlyCollection<WildfireImportRecord> records,
            CancellationToken cancellationToken = default)
        {
            SynchronizedRecords = records;
            return Task.FromResult(syncResult);
        }
    }

    private sealed class ThrowingRepository(Exception exception) : IWildfireRepository
    {
        public Task<IReadOnlyList<WildfireDto>> GetAllAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WildfireDto>>([]);

        public Task<WildfireDto?> GetByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WildfireDto?>(null);

        public Task<IReadOnlyList<NearbyWildfireDto>> FindNearAsync(
            double latitude,
            double longitude,
            double radiusKm,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<NearbyWildfireDto>>([]);

        public Task<(int Inserted, int Changed, int Observed)> SynchronizeAsync(
            IReadOnlyCollection<WildfireImportRecord> records,
            CancellationToken cancellationToken = default) =>
            Task.FromException<(int Inserted, int Changed, int Observed)>(exception);
    }

    private sealed class StubSyncStateRepository : IWildfireSyncStateRepository
    {
        public bool RecordedFetchSuccess { get; private set; }
        public bool RecordedAttemptSuccess { get; private set; }
        public bool RecordedFailure { get; private set; }

        public Task<WildfireFeedSyncStateDto?> GetAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WildfireFeedSyncStateDto?>(null);

        public Task RecordFetchSuccessAsync(
            DateTime attemptUtc,
            DateTime successfulFetchUtc,
            int received,
            int accepted,
            int rejected,
            CancellationToken cancellationToken = default)
        {
            RecordedFetchSuccess = true;
            return Task.CompletedTask;
        }

        public Task RecordAttemptSuccessAsync(
            DateTime attemptUtc,
            CancellationToken cancellationToken = default)
        {
            RecordedAttemptSuccess = true;
            return Task.CompletedTask;
        }

        public Task RecordFailureAsync(
            DateTime attemptUtc,
            string error,
            CancellationToken cancellationToken = default)
        {
            RecordedFailure = true;
            return Task.CompletedTask;
        }
    }
}
