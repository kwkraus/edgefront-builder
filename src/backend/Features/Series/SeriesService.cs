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

        var sessionCountsByStatus = await _db.Sessions
            .Where(s => seriesIds.Contains(s.SeriesId))
            .GroupBy(s => new { s.SeriesId, s.Status })
            .Select(g => new { g.Key.SeriesId, g.Key.Status, Count = g.Count() })
            .ToListAsync();

        var sessionCounts = sessionCountsByStatus
            .GroupBy(x => x.SeriesId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Count));

        var draftSessionCounts = sessionCountsByStatus
            .Where(x => x.Status == SessionStatus.Draft)
            .ToDictionary(x => x.SeriesId, x => x.Count);
            
        return series.Select(s =>
        {
            var m = metrics.TryGetValue(s.SeriesId, out var sm) ? sm : null;
            return new SeriesListItemDto(
                s.SeriesId,
                s.Title,
                s.Status.ToString(),
                sessionCounts.TryGetValue(s.SeriesId, out var count) ? count : 0,
                draftSessionCounts.TryGetValue(s.SeriesId, out var draftCount) ? draftCount : 0,
                m?.TotalRegistrations ?? 0,
                m?.TotalAttendees ?? 0,
                m?.UniqueAccountsInfluenced ?? 0,
                false,
                s.CreatedAt,
                s.UpdatedAt);
        });
    }

    public async Task<SeriesResponseDto?> GetByIdAsync(Guid id, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return null;

        var draftCount = await _db.Sessions
            .CountAsync(s => s.SeriesId == id && s.Status == SessionStatus.Draft);
        return ToResponseDto(series, draftCount);
    }

    public async Task<SeriesResponseDto> CreateAsync(CreateSeriesRequest req, string ownerUserId)
    {
        var series = new Domain.Entities.Series
        {
            SeriesId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            Title = req.Title,
            Status = SeriesStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Series.Add(series);
        await _db.SaveChangesAsync();
        return ToResponseDto(series, draftSessionCount: 0);
    }

    public async Task<SeriesResponseDto?> UpdateAsync(Guid id, UpdateSeriesRequest req, string ownerUserId)
    {
        var series = await _db.Series
            .FirstOrDefaultAsync(s => s.SeriesId == id && s.OwnerUserId == ownerUserId);
        if (series is null) return null;

        series.Title = req.Title;
        series.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var draftCount = await _db.Sessions
            .CountAsync(s => s.SeriesId == id && s.Status == SessionStatus.Draft);
        return ToResponseDto(series, draftCount);
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

    private static SeriesResponseDto ToResponseDto(Domain.Entities.Series s, int draftSessionCount = 0) =>
        new(s.SeriesId, s.Title, s.Status.ToString(), draftSessionCount, s.CreatedAt, s.UpdatedAt);
}
