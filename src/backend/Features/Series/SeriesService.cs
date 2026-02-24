using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Series.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Series;

public class SeriesService
{
    private readonly AppDbContext _db;

    public SeriesService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SeriesListItemDto>> GetAllAsync(string ownerUserId)
    {
        var series = await _db.Series
            .Where(s => s.OwnerUserId == ownerUserId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var seriesIds = series.Select(s => s.SeriesId).ToList();

        var metrics = await _db.SeriesMetrics
            .Where(m => seriesIds.Contains(m.SeriesId))
            .ToDictionaryAsync(m => m.SeriesId);

        var sessionCounts = await _db.Sessions
            .Where(s => seriesIds.Contains(s.SeriesId))
            .GroupBy(s => s.SeriesId)
            .Select(g => new { SeriesId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SeriesId, x => x.Count);

        var reconcileIssueSet = (await _db.Sessions
            .Where(s => seriesIds.Contains(s.SeriesId)
                && (s.ReconcileStatus == ReconcileStatus.Retrying
                    || s.ReconcileStatus == ReconcileStatus.Disabled))
            .Select(s => s.SeriesId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        return series.Select(s =>
        {
            var m = metrics.TryGetValue(s.SeriesId, out var sm) ? sm : null;
            return new SeriesListItemDto(
                s.SeriesId,
                s.Title,
                s.Status.ToString(),
                sessionCounts.TryGetValue(s.SeriesId, out var count) ? count : 0,
                m?.TotalRegistrations ?? 0,
                m?.TotalAttendees ?? 0,
                m?.UniqueAccountsInfluenced ?? 0,
                reconcileIssueSet.Contains(s.SeriesId),
                s.CreatedAt,
                s.UpdatedAt);
        });
    }

    public async Task<SeriesResponseDto?> GetByIdAsync(Guid id, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        return series is null ? null : ToResponseDto(series);
    }

    public async Task<SeriesResponseDto> CreateAsync(CreateSeriesRequest req, string ownerUserId)
    {
        var series = new Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Title = req.Title,
            Status = SeriesStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Series.Add(series);
        await _db.SaveChangesAsync();
        return ToResponseDto(series);
    }

    public async Task<SeriesResponseDto?> UpdateAsync(Guid id, UpdateSeriesRequest req, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return null;

        series.Title = req.Title;
        series.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToResponseDto(series);
    }

    public async Task<bool> DeleteAsync(Guid id, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return false;

        _db.Series.Remove(series);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(SeriesResponseDto? series, string? errorCode)> PublishAsync(Guid id, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return (null, "series_not_found");

        series.Status = SeriesStatus.Published;
        series.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // TODO-SPEC-200: implement in Phase 3 — create Teams webinar
        return (ToResponseDto(series), null);
    }

    private static SeriesResponseDto ToResponseDto(Domain.Entities.Series s) =>
        new(s.SeriesId, s.Title, s.Status.ToString(), s.CreatedAt, s.UpdatedAt);
}
