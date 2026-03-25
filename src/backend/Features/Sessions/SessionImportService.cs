using System.Globalization;
using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EdgeFront.Builder.Features.Sessions;

public class SessionImportService
{
    private readonly AppDbContext _db;
    private readonly MetricsRecomputeService _metricsRecompute;
    private readonly ILogger<SessionImportService> _logger;

    public SessionImportService(
        AppDbContext db,
        MetricsRecomputeService metricsRecompute,
        ILogger<SessionImportService> logger)
    {
        _db = db;
        _metricsRecompute = metricsRecompute;
        _logger = logger;
    }

    public async Task<(ImportResultDto? Result, string? ErrorCode)> ImportRegistrationsAsync(
        Guid sessionId, string ownerUserId, Stream csvStream, CancellationToken ct = default)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId, ct);

        if (session is null)
            return (null, "session_not_found");

        _logger.LogInformation(
            "CSV registration import starting. SessionId={SessionId}", sessionId);

        var lines = await ReadLinesAsync(csvStream, ct);
        if (lines.Count == 0)
            return (new ImportResultDto(0, 0, 0, 0, new List<RowErrorDto>()), null);

        var headers = ParseCsvRow(lines[0])
            .Select(h => h.Trim().ToLowerInvariant())
            .ToList();

        var emailCol = headers.IndexOf("email");
        if (emailCol < 0)
            return (null, "missing_required_column_email");

        var registeredAtCol = headers.IndexOf("registeredat");

        var errors = new List<RowErrorDto>();
        int imported = 0, skipped = 0, invalid = 0, totalRows = 0;

        var existing = await _db.NormalizedRegistrations
            .Where(r => r.SessionId == sessionId)
            .ToListAsync(ct);

        var existingByEmail = existing
            .ToDictionary(r => r.Email, StringComparer.OrdinalIgnoreCase);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            for (int i = 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                totalRows++;
                var fields = ParseCsvRow(line);
                var rowNum = i + 1; // 1-based, header is row 1

                if (emailCol >= fields.Count || string.IsNullOrWhiteSpace(fields[emailCol]))
                {
                    errors.Add(new RowErrorDto(rowNum, "Email is required."));
                    invalid++;
                    continue;
                }

                var rawEmail = fields[emailCol].Trim();
                if (!rawEmail.Contains('@'))
                {
                    errors.Add(new RowErrorDto(rowNum, "Invalid email format."));
                    invalid++;
                    continue;
                }

                var email = DomainNormalizer.NormalizeEmail(rawEmail);
                var domain = DomainNormalizer.NormalizeEmailDomain(rawEmail);

                DateTime registeredAt = DateTime.UtcNow;
                if (registeredAtCol >= 0 && registeredAtCol < fields.Count
                    && !string.IsNullOrWhiteSpace(fields[registeredAtCol]))
                {
                    if (!TryParseDateTime(fields[registeredAtCol].Trim(), out registeredAt))
                    {
                        errors.Add(new RowErrorDto(rowNum, "Invalid RegisteredAt date format."));
                        invalid++;
                        continue;
                    }
                }

                if (existingByEmail.TryGetValue(email, out var existingReg))
                {
                    existingReg.RegisteredAt = registeredAt;
                    existingReg.EmailDomain = domain;
                    skipped++;
                }
                else
                {
                    var newReg = new NormalizedRegistration
                    {
                        RegistrationId = Guid.NewGuid(),
                        SessionId = sessionId,
                        OwnerUserId = ownerUserId,
                        Email = email,
                        EmailDomain = domain,
                        RegisteredAt = registeredAt
                    };
                    _db.NormalizedRegistrations.Add(newReg);
                    existingByEmail[email] = newReg;
                    imported++;
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV registration import failed. SessionId={SessionId}", sessionId);
            await transaction.RollbackAsync(ct);
            return (null, "import_failed");
        }

        // Recompute metrics after successful import
        await _metricsRecompute.RecomputeSessionMetricsAsync(session.SessionId, ct);
        await _metricsRecompute.RecomputeSeriesMetricsAsync(session.SeriesId, ct);

        _logger.LogInformation(
            "CSV registration import completed. SessionId={SessionId} Imported={Imported} Skipped={Skipped} Invalid={Invalid}",
            sessionId, imported, skipped, invalid);

        var result = new ImportResultDto(totalRows, imported, skipped, invalid, errors);
        return (result, null);
    }

    public async Task<(ImportResultDto? Result, string? ErrorCode)> ImportAttendanceAsync(
        Guid sessionId, string ownerUserId, Stream csvStream, CancellationToken ct = default)
    {
        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId, ct);

        if (session is null)
            return (null, "session_not_found");

        _logger.LogInformation(
            "CSV attendance import starting. SessionId={SessionId}", sessionId);

        var lines = await ReadLinesAsync(csvStream, ct);
        if (lines.Count == 0)
            return (new ImportResultDto(0, 0, 0, 0, new List<RowErrorDto>()), null);

        var headers = ParseCsvRow(lines[0])
            .Select(h => h.Trim().ToLowerInvariant())
            .ToList();

        var emailCol = headers.IndexOf("email");
        if (emailCol < 0)
            return (null, "missing_required_column_email");

        var attendedCol = headers.IndexOf("attended");
        var durationSecondsCol = headers.IndexOf("durationseconds");
        var firstJoinAtCol = headers.IndexOf("firstjoinat");
        var lastLeaveAtCol = headers.IndexOf("lastleaveat");

        var errors = new List<RowErrorDto>();
        int imported = 0, skipped = 0, invalid = 0, totalRows = 0;

        var existing = await _db.NormalizedAttendances
            .Where(a => a.SessionId == sessionId)
            .ToListAsync(ct);

        var existingByEmail = existing
            .ToDictionary(a => a.Email, StringComparer.OrdinalIgnoreCase);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            for (int i = 1; i < lines.Count; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                totalRows++;
                var fields = ParseCsvRow(line);
                var rowNum = i + 1;

                if (emailCol >= fields.Count || string.IsNullOrWhiteSpace(fields[emailCol]))
                {
                    errors.Add(new RowErrorDto(rowNum, "Email is required."));
                    invalid++;
                    continue;
                }

                var rawEmail = fields[emailCol].Trim();
                if (!rawEmail.Contains('@'))
                {
                    errors.Add(new RowErrorDto(rowNum, "Invalid email format."));
                    invalid++;
                    continue;
                }

                var email = DomainNormalizer.NormalizeEmail(rawEmail);
                var domain = DomainNormalizer.NormalizeEmailDomain(rawEmail);

                bool attended = true;
                if (attendedCol >= 0 && attendedCol < fields.Count
                    && !string.IsNullOrWhiteSpace(fields[attendedCol]))
                {
                    var val = fields[attendedCol].Trim().ToLowerInvariant();
                    attended = val is "true" or "1" or "yes";
                }

                int? durationSeconds = null;
                if (durationSecondsCol >= 0 && durationSecondsCol < fields.Count
                    && !string.IsNullOrWhiteSpace(fields[durationSecondsCol]))
                {
                    if (int.TryParse(fields[durationSecondsCol].Trim(), out var ds))
                        durationSeconds = ds;
                    else
                    {
                        errors.Add(new RowErrorDto(rowNum, "Invalid DurationSeconds value."));
                        invalid++;
                        continue;
                    }
                }

                DateTime? firstJoinAt = null;
                if (firstJoinAtCol >= 0 && firstJoinAtCol < fields.Count
                    && !string.IsNullOrWhiteSpace(fields[firstJoinAtCol]))
                {
                    if (TryParseDateTime(fields[firstJoinAtCol].Trim(), out var fj))
                        firstJoinAt = fj;
                    else
                    {
                        errors.Add(new RowErrorDto(rowNum, "Invalid FirstJoinAt date format."));
                        invalid++;
                        continue;
                    }
                }

                DateTime? lastLeaveAt = null;
                if (lastLeaveAtCol >= 0 && lastLeaveAtCol < fields.Count
                    && !string.IsNullOrWhiteSpace(fields[lastLeaveAtCol]))
                {
                    if (TryParseDateTime(fields[lastLeaveAtCol].Trim(), out var ll))
                        lastLeaveAt = ll;
                    else
                    {
                        errors.Add(new RowErrorDto(rowNum, "Invalid LastLeaveAt date format."));
                        invalid++;
                        continue;
                    }
                }

                if (existingByEmail.TryGetValue(email, out var existingAtt))
                {
                    existingAtt.Attended = attended;
                    existingAtt.DurationSeconds = durationSeconds;
                    existingAtt.FirstJoinAt = firstJoinAt;
                    existingAtt.LastLeaveAt = lastLeaveAt;
                    existingAtt.EmailDomain = domain;
                    skipped++;
                }
                else
                {
                    var newAtt = new NormalizedAttendance
                    {
                        AttendanceId = Guid.NewGuid(),
                        SessionId = sessionId,
                        OwnerUserId = ownerUserId,
                        Email = email,
                        EmailDomain = domain,
                        Attended = attended,
                        DurationSeconds = durationSeconds,
                        FirstJoinAt = firstJoinAt,
                        LastLeaveAt = lastLeaveAt
                    };
                    _db.NormalizedAttendances.Add(newAtt);
                    existingByEmail[email] = newAtt;
                    imported++;
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV attendance import failed. SessionId={SessionId}", sessionId);
            await transaction.RollbackAsync(ct);
            return (null, "import_failed");
        }

        // Recompute metrics after successful import
        await _metricsRecompute.RecomputeSessionMetricsAsync(session.SessionId, ct);
        await _metricsRecompute.RecomputeSeriesMetricsAsync(session.SeriesId, ct);

        _logger.LogInformation(
            "CSV attendance import completed. SessionId={SessionId} Imported={Imported} Skipped={Skipped} Invalid={Invalid}",
            sessionId, imported, skipped, invalid);

        var result = new ImportResultDto(totalRows, imported, skipped, invalid, errors);
        return (result, null);
    }

    private static async Task<List<string>> ReadLinesAsync(Stream stream, CancellationToken ct)
    {
        var lines = new List<string>();
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// Parses a single CSV row, handling basic double-quote field wrapping.
    /// </summary>
    private static List<string> ParseCsvRow(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote ("")
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static bool TryParseDateTime(string value, out DateTime result)
    {
        // Try ISO 8601 first, then common formats
        string[] formats =
        [
            "o",                         // ISO 8601 roundtrip
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd",
            "M/d/yyyy h:mm:ss tt",
            "M/d/yyyy HH:mm:ss",
            "M/d/yyyy",
            "MM/dd/yyyy HH:mm:ss",
            "MM/dd/yyyy"
        ];

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result))
        {
            result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
            return true;
        }

        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out result))
        {
            result = DateTime.SpecifyKind(result, DateTimeKind.Utc);
            return true;
        }

        result = default;
        return false;
    }
}
