using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.People;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Sessions;

public class SessionService
{
    private readonly AppDbContext _db;

    public SessionService(AppDbContext db, ILogger<SessionService>? logger = null)
    {
        _db = db;
    }

    public async Task<IEnumerable<SessionListItemDto>> GetBySeriesAsync(Guid seriesId, string ownerUserId, string ownerDisplayName = "")
    {
        var sessions = await _db.Sessions
            .Where(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId)
            .OrderBy(s => s.StartsAt)
            .ToListAsync();

        var sessionIds = sessions.Select(s => s.SessionId).ToList();

        var metrics = await _db.SessionMetrics
            .Where(m => sessionIds.Contains(m.SessionId))
            .ToDictionaryAsync(m => m.SessionId);

        var presentersBySession = await _db.Set<SessionPresenter>()
            .AsNoTracking()
            .Where(p => sessionIds.Contains(p.SessionId))
            .Select(p => new { p.SessionId, p.DisplayName, p.Email })
            .ToListAsync();

        var coordinatorsBySession = await _db.Set<SessionCoordinator>()
            .AsNoTracking()
            .Where(c => sessionIds.Contains(c.SessionId))
            .Select(c => new { c.SessionId, c.DisplayName, c.Email })
            .ToListAsync();

        var presenterLookup = presentersBySession
            .GroupBy(p => p.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var coordinatorLookup = coordinatorsBySession
            .GroupBy(c => c.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return sessions.Select(s =>
        {
            var m = metrics.TryGetValue(s.SessionId, out var sm) ? sm : null;
            var presenters = presenterLookup.TryGetValue(s.SessionId, out var pl) ? pl : [];
            var coordinators = coordinatorLookup.TryGetValue(s.SessionId, out var cl) ? cl : [];

            return new SessionListItemDto(
                s.SessionId,
                s.Title,
                s.StartsAt,
                s.EndsAt,
                m?.TotalRegistrations ?? 0,
                m?.TotalAttendees ?? 0,
                presenters.Count,
                coordinators.Count,
                ownerDisplayName,
                presenters.Select(p => new PersonSummary(p.DisplayName, p.Email)).ToList(),
                coordinators.Select(c => new PersonSummary(c.DisplayName, c.Email)).ToList());
        });
    }

    public async Task<SessionResponseDto?> GetByIdAsync(Guid sessionId, string ownerUserId)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null) return null;

        var (presenters, coordinators) = await GetRolesAsync(sessionId);
        return ToResponseDto(session, presenters, coordinators);
    }

    public async Task<(SessionResponseDto? session, string? errorCode)> CreateAsync(
        Guid seriesId, CreateSessionRequest req, string ownerUserId)
    {
        if (req.EndsAt <= req.StartsAt)
            return (null, "invalid_time_range");

        var seriesExists = await _db.Series
            .AnyAsync(s => s.SeriesId == seriesId && s.OwnerUserId == ownerUserId);
        if (!seriesExists)
            return (null, "series_not_found");

        var session = new Session
        {
            SessionId = Guid.NewGuid(),
            SeriesId = seriesId,
            OwnerUserId = ownerUserId,
            Title = req.Title,
            StartsAt = req.StartsAt.Kind == DateTimeKind.Utc ? req.StartsAt : req.StartsAt.ToUniversalTime(),
            EndsAt = req.EndsAt.Kind == DateTimeKind.Utc ? req.EndsAt : req.EndsAt.ToUniversalTime()
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();
        return (ToResponseDto(session), null);
    }

    public async Task<(SessionResponseDto? session, string? errorCode)> UpdateAsync(
        Guid sessionId, UpdateSessionRequest req, string ownerUserId)
    {
        if (req.EndsAt <= req.StartsAt)
            return (null, "invalid_time_range");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        session.Title = req.Title;
        session.StartsAt = req.StartsAt.Kind == DateTimeKind.Utc ? req.StartsAt : req.StartsAt.ToUniversalTime();
        session.EndsAt = req.EndsAt.Kind == DateTimeKind.Utc ? req.EndsAt : req.EndsAt.ToUniversalTime();

        await _db.SaveChangesAsync();

        var (presenters, coordinators) = await GetRolesAsync(sessionId);
        return (ToResponseDto(session, presenters, coordinators), null);
    }

    public async Task<(SessionResponseDto? session, string? errorCode)> UpdateTitleAsync(
        Guid sessionId, UpdateSessionTitleRequest req, string ownerUserId)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        session.Title = req.Title;

        await _db.SaveChangesAsync();

        var (presenters, coordinators) = await GetRolesAsync(sessionId);
        return (ToResponseDto(session, presenters, coordinators), null);
    }

    public async Task<bool> DeleteAsync(Guid sessionId, string ownerUserId)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null) return false;

        _db.Sessions.Remove(session);

        await _db.SaveChangesAsync();
        return true;
    }

    // --- Presenter / Coordinator role management (SPEC-210) ---

    public async Task<List<SessionPresenterDto>> GetPresentersAsync(Guid sessionId)
    {
        return await _db.SessionPresenters
            .Where(p => p.SessionId == sessionId)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new SessionPresenterDto(p.SessionPresenterId, p.EntraUserId, p.DisplayName, p.Email))
            .ToListAsync();
    }

    public async Task<List<SessionCoordinatorDto>> GetCoordinatorsAsync(Guid sessionId)
    {
        return await _db.SessionCoordinators
            .Where(c => c.SessionId == sessionId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new SessionCoordinatorDto(c.SessionCoordinatorId, c.EntraUserId, c.DisplayName, c.Email))
            .ToListAsync();
    }

    public async Task<(List<SessionPresenterDto>? presenters, string? errorCode)> SetPresentersAsync(
        Guid sessionId, string ownerUserId, SetPresentersRequest req)
    {
        if (req.People is null)
            return (null, "people_required");

        var duplicates = req.People
            .GroupBy(p => p.EntraUserId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicates.Count > 0)
            return (null, "duplicate_entra_user_id");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        // Remove existing presenters
        var existing = await _db.SessionPresenters
            .Where(p => p.SessionId == sessionId)
            .ToListAsync();
        _db.SessionPresenters.RemoveRange(existing);

        // Insert new presenters
        var now = DateTime.UtcNow;
        var newPresenters = req.People.Select(p => new SessionPresenter
        {
            SessionPresenterId = Guid.NewGuid(),
            SessionId = sessionId,
            EntraUserId = p.EntraUserId,
            DisplayName = p.DisplayName,
            Email = p.Email,
            CreatedAt = now
        }).ToList();
        _db.SessionPresenters.AddRange(newPresenters);
        await _db.SaveChangesAsync();

        return (newPresenters.Select(p =>
            new SessionPresenterDto(p.SessionPresenterId, p.EntraUserId, p.DisplayName, p.Email)).ToList(), null);
    }

    public async Task<(List<SessionCoordinatorDto>? coordinators, string? errorCode)> SetCoordinatorsAsync(
        Guid sessionId, string ownerUserId, SetCoordinatorsRequest req)
    {
        if (req.People is null)
            return (null, "people_required");

        var duplicates = req.People
            .GroupBy(p => p.EntraUserId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();
        if (duplicates.Count > 0)
            return (null, "duplicate_entra_user_id");

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId);
        if (session is null)
            return (null, "session_not_found");

        // Remove existing coordinators
        var existing = await _db.SessionCoordinators
            .Where(c => c.SessionId == sessionId)
            .ToListAsync();
        _db.SessionCoordinators.RemoveRange(existing);

        // Insert new coordinators
        var now = DateTime.UtcNow;
        var newCoordinators = req.People.Select(p => new SessionCoordinator
        {
            SessionCoordinatorId = Guid.NewGuid(),
            SessionId = sessionId,
            EntraUserId = p.EntraUserId,
            DisplayName = p.DisplayName,
            Email = p.Email,
            CreatedAt = now
        }).ToList();
        _db.SessionCoordinators.AddRange(newCoordinators);
        await _db.SaveChangesAsync();

        return (newCoordinators.Select(c =>
            new SessionCoordinatorDto(c.SessionCoordinatorId, c.EntraUserId, c.DisplayName, c.Email)).ToList(), null);
    }

    // --- Helpers ---

    private async Task<(List<SessionPresenterDto>, List<SessionCoordinatorDto>)> GetRolesAsync(Guid sessionId)
    {
        var presenters = await GetPresentersAsync(sessionId);
        var coordinators = await GetCoordinatorsAsync(sessionId);
        return (presenters, coordinators);
    }

    private static SessionResponseDto ToResponseDto(
        Session s,
        List<SessionPresenterDto>? presenters = null,
        List<SessionCoordinatorDto>? coordinators = null) =>
        new(s.SessionId, s.SeriesId, s.Title, s.StartsAt, s.EndsAt,
            presenters ?? new List<SessionPresenterDto>(),
            coordinators ?? new List<SessionCoordinatorDto>());
}
