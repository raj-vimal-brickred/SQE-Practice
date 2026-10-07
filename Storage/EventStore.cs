using Microsoft.EntityFrameworkCore;
using SQE_Practice.Models;
using SQE_Practice.Storage.Entities;
using System.Text.Json;

namespace SQE_Practice.Storage
{
    public class EventStore
    {
        private readonly IDbContextFactory<SqeDbContext>
_dbContextFactory;

        private readonly ILogger<EventStore> _logger;

        public EventStore(
        IDbContextFactory<SqeDbContext> dbContextFactory,
        ILogger<EventStore> logger)
        {
            _dbContextFactory = dbContextFactory;
            _logger = logger;
        }

        public async Task<bool> SaveAsync(
        TelemetryEvent telemetryEvent,
        CancellationToken cancellationToken = default)
        {
            await using var database =
            await _dbContextFactory.CreateDbContextAsync(
            cancellationToken);

            var eventId = telemetryEvent.EventId;

            if (eventId == Guid.Empty)
            {
                eventId = Guid.NewGuid();
            }

            var duplicate =
            await database.Events.AnyAsync(
            item => item.EventId == eventId,
            cancellationToken);

            if (duplicate)
            {
                _logger.LogInformation(
                "Duplicate event {EventId} was ignored.",
                eventId);

                return false;
            }

            var entity = new TelemetryEventEntity
            {
                EventId = eventId,
                EventName = telemetryEvent.EventName,
                Service = telemetryEvent.Service,
                Region = telemetryEvent.Region,
                DurationMs = telemetryEvent.DurationMs,

                Timestamp =
            telemetryEvent.Timestamp == default
            ? DateTime.UtcNow
            : telemetryEvent.Timestamp,

                PropertiesJson =
            JsonSerializer.Serialize(
            telemetryEvent.Properties),

                CreatedAtUtc = DateTime.UtcNow
            };

            database.Events.Add(entity);

            await database.SaveChangesAsync(
            cancellationToken);

            _logger.LogInformation(
            "Event {EventId} saved to SQL Server.",
            eventId);

            return true;
        }

        public async Task<List<TelemetryEventEntity>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
        {
            limit = Math.Clamp(limit, 1, 1000);

            await using var database =
            await _dbContextFactory.CreateDbContextAsync(
            cancellationToken);

            return await database.Events
            .AsNoTracking()
            .OrderByDescending(item => item.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);
        }
    }
}
