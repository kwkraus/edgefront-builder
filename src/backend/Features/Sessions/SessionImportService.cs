using System.Globalization;
using System.Net.Mail;
using EdgeFront.Builder.Domain;
using EdgeFront.Builder.Domain.Entities;
using EdgeFront.Builder.Features.Sessions.Dtos;
using EdgeFront.Builder.Features.Metrics;
using EdgeFront.Builder.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualBasic.FileIO;

namespace EdgeFront.Builder.Features.Sessions;

public class SessionImportService
{
    private readonly AppDbContext _db;
    private readonly MetricsRecomputeService _metricsRecomputeService;
    private readonly ILogger _logger;

    public SessionImportService(
        AppDbContext db,
        MetricsRecomputeService metricsRecomputeService,
        ILogger<SessionImportService>? logger = null)
    {
        _db = db;
        _metricsRecomputeService = metricsRecomputeService;
        _logger = logger ?? NullLogger<SessionImportService>.Instance;
    }

    public Task<SessionImportOutcome> ReplaceRegistrationsAsync(
        Guid sessionId,
        string ownerUserId,
        IFormFile? file,
        CancellationToken ct = default)
        => ReplaceImportAsync(
            sessionId,
            ownerUserId,
            file,
            SessionImportType.Registrations,
            ParseRegistrationsAsync,
            async (resolvedSessionId, entities, cancellationToken) =>
            {
                var existing = await _db.NormalizedRegistrations
                    .Where(x => x.SessionId == resolvedSessionId)
                    .ToListAsync(cancellationToken);
                _db.NormalizedRegistrations.RemoveRange(existing);
                _db.NormalizedRegistrations.AddRange((IReadOnlyList<NormalizedRegistration>)entities);
            },
            ct);

    public Task<SessionImportOutcome> ReplaceAttendanceAsync(
        Guid sessionId,
        string ownerUserId,
        IFormFile? file,
        CancellationToken ct = default)
        => ReplaceImportAsync(
            sessionId,
            ownerUserId,
            file,
            SessionImportType.Attendance,
            ParseAttendanceAsync,
            async (resolvedSessionId, entities, cancellationToken) =>
            {
                var existing = await _db.NormalizedAttendances
                    .Where(x => x.SessionId == resolvedSessionId)
                    .ToListAsync(cancellationToken);
                _db.NormalizedAttendances.RemoveRange(existing);
                _db.NormalizedAttendances.AddRange((IReadOnlyList<NormalizedAttendance>)entities);
            },
            ct);

    public Task<SessionImportOutcome> ReplaceQaAsync(
        Guid sessionId,
        string ownerUserId,
        IFormFile? file,
        CancellationToken ct = default)
        => ReplaceImportAsync(
            sessionId,
            ownerUserId,
            file,
            SessionImportType.Qa,
            ParseQaAsync,
            async (resolvedSessionId, entities, cancellationToken) =>
            {
                var existing = await _db.NormalizedQaEntries
                    .Where(x => x.SessionId == resolvedSessionId)
                    .ToListAsync(cancellationToken);
                _db.NormalizedQaEntries.RemoveRange(existing);
                _db.NormalizedQaEntries.AddRange((IReadOnlyList<NormalizedQaEntry>)entities);
            },
            ct);

    private async Task<SessionImportOutcome> ReplaceImportAsync<T>(
        Guid sessionId,
        string ownerUserId,
        IFormFile? file,
        SessionImportType importType,
        Func<Guid, string, IFormFile, DateTime, CancellationToken, Task<IReadOnlyList<T>>> parseAsync,
        Func<Guid, IReadOnlyList<T>, CancellationToken, Task> replaceAsync,
        CancellationToken ct)
    {
        if (file is null)
        {
            return SessionImportOutcome.ValidationFailed(
                "validation_error",
                "A CSV file is required.",
                new { field = "file" });
        }

        if (file.Length <= 0)
        {
            return SessionImportOutcome.ValidationFailed(
                "validation_error",
                "The uploaded CSV file is empty.",
                new { field = "file", fileName = file.FileName });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return SessionImportOutcome.ValidationFailed(
                "validation_error",
                "Only CSV uploads are supported.",
                new { field = "file", fileName = file.FileName });
        }

        var session = await _db.Sessions
            .AsNoTracking()
            .Where(s => s.SessionId == sessionId && s.OwnerUserId == ownerUserId)
            .Select(s => new { s.SessionId, s.SeriesId })
            .FirstOrDefaultAsync(ct);

        if (session is null)
        {
            return SessionImportOutcome.NotFound(
                "session_not_found",
                "Session not found.");
        }

        var importedAt = DateTime.UtcNow;
        IReadOnlyList<T> entities;

        try
        {
            entities = await parseAsync(session.SessionId, ownerUserId, file, importedAt, ct);
        }
        catch (CsvImportValidationException ex)
        {
            _logger.LogWarning(
                "Session import validation failed. SessionId={SessionId} ImportType={ImportType} FileName={FileName} ErrorCode={ErrorCode}",
                session.SessionId,
                importType,
                file.FileName,
                ex.ErrorCode);

            return SessionImportOutcome.ValidationFailed(ex.ErrorCode, ex.Message, ex.Details);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        await replaceAsync(session.SessionId, entities, ct);
        await UpsertImportSummaryAsync(session.SessionId, importType, file.FileName, entities.Count, importedAt, ct);
        await _db.SaveChangesAsync(ct);

        await _metricsRecomputeService.RecomputeSessionMetricsAsync(session.SessionId, useTransaction: false, ct);
        await _metricsRecomputeService.RecomputeSeriesMetricsAsync(session.SeriesId, useTransaction: false, ct);

        await tx.CommitAsync(ct);

        _logger.LogInformation(
            "Session import completed. SessionId={SessionId} ImportType={ImportType} FileName={FileName} RowCount={RowCount}",
            session.SessionId,
            importType,
            file.FileName,
            entities.Count);

        return SessionImportOutcome.Success(
            new SessionImportSummaryDto(importType.ToString(), file.FileName, entities.Count, importedAt));
    }

    private async Task UpsertImportSummaryAsync(
        Guid sessionId,
        SessionImportType importType,
        string fileName,
        int rowCount,
        DateTime importedAt,
        CancellationToken ct)
    {
        var existing = await _db.SessionImportSummaries
            .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.ImportType == importType, ct);

        if (existing is null)
        {
            _db.SessionImportSummaries.Add(new SessionImportSummary
            {
                SessionImportSummaryId = Guid.NewGuid(),
                SessionId = sessionId,
                ImportType = importType,
                FileName = fileName,
                RowCount = rowCount,
                ImportedAt = importedAt
            });
            return;
        }

        existing.FileName = fileName;
        existing.RowCount = rowCount;
        existing.ImportedAt = importedAt;
    }

    private static async Task<IReadOnlyList<NormalizedRegistration>> ParseRegistrationsAsync(
        Guid sessionId,
        string ownerUserId,
        IFormFile file,
        DateTime importedAt,
        CancellationToken ct)
    {
        var csv = await ReadCsvAsync(file, ct);
        var emailIndex = csv.RequireHeader(
            "email",
            ["email", "emailaddress", "registrantemail", "attendeeemail"]);
        var registeredAtIndex = csv.FindHeader(
            "registeredAt",
            ["registeredat", "registrationtime", "registeredon", "registrationdate", "createdat"]);

        var rows = csv.Rows
            .Select(row =>
            {
                var email = ParseRequiredEmail(row, emailIndex, "email");
                var registeredAt = registeredAtIndex is null
                    ? importedAt
                    : ParseOptionalUtcDateTime(row, registeredAtIndex.Value, "registeredAt") ?? importedAt;

                return new
                {
                    Email = email,
                    RegisteredAt = registeredAt
                };
            })
            .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NormalizedRegistration
            {
                RegistrationId = Guid.NewGuid(),
                SessionId = sessionId,
                OwnerUserId = ownerUserId,
                Email = g.Key,
                EmailDomain = DomainNormalizer.NormalizeEmailDomain(g.Key),
                RegisteredAt = g.Min(x => x.RegisteredAt)
            })
            .ToList();

        return rows;
    }

    private static async Task<IReadOnlyList<NormalizedAttendance>> ParseAttendanceAsync(
        Guid sessionId,
        string ownerUserId,
        IFormFile file,
        DateTime importedAt,
        CancellationToken ct)
    {
        var csv = await ReadCsvAsync(file, ct);
        var emailIndex = csv.RequireHeader(
            "email",
            ["email", "emailaddress", "attendeeemail", "registrantemail"]);
        var attendedIndex = csv.FindHeader("attended", ["attended", "hasattended", "present"]);
        var durationSecondsIndex = csv.FindHeader("durationSeconds", ["durationseconds", "secondsattended"]);
        var durationMinutesIndex = csv.FindHeader("durationMinutes", ["durationminutes", "minutesattended"]);
        var durationPercentIndex = csv.FindHeader("durationPercent", ["durationpercent", "attendancepercentage", "percentattended"]);
        var firstJoinAtIndex = csv.FindHeader("firstJoinAt", ["firstjoinat", "joinat", "firstjoinedat", "jointime"]);
        var lastLeaveAtIndex = csv.FindHeader("lastLeaveAt", ["lastleaveat", "leaveat", "lastleftat", "leavetime"]);

        var rows = csv.Rows
            .Select(row =>
            {
                var email = ParseRequiredEmail(row, emailIndex, "email");
                var durationSeconds = durationSecondsIndex is not null
                    ? ParseOptionalInt(row, durationSecondsIndex.Value, "durationSeconds")
                    : null;

                if (durationSeconds is null && durationMinutesIndex is not null)
                {
                    var minutes = ParseOptionalDecimal(row, durationMinutesIndex.Value, "durationMinutes");
                    durationSeconds = minutes is null ? null : (int)Math.Round(minutes.Value * 60m, MidpointRounding.AwayFromZero);
                }

                var firstJoinAt = firstJoinAtIndex is null
                    ? null
                    : ParseOptionalUtcDateTime(row, firstJoinAtIndex.Value, "firstJoinAt");
                var lastLeaveAt = lastLeaveAtIndex is null
                    ? null
                    : ParseOptionalUtcDateTime(row, lastLeaveAtIndex.Value, "lastLeaveAt");
                var durationPercent = durationPercentIndex is null
                    ? null
                    : ParseOptionalDecimal(row, durationPercentIndex.Value, "durationPercent");
                var attended = attendedIndex is null
                    ? true
                    : ParseRequiredBoolean(row, attendedIndex.Value, "attended");

                return new
                {
                    Email = email,
                    Attended = attended,
                    DurationSeconds = durationSeconds,
                    DurationPercent = durationPercent,
                    FirstJoinAt = firstJoinAt,
                    LastLeaveAt = lastLeaveAt
                };
            })
            .GroupBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NormalizedAttendance
            {
                AttendanceId = Guid.NewGuid(),
                SessionId = sessionId,
                OwnerUserId = ownerUserId,
                Email = g.Key,
                EmailDomain = DomainNormalizer.NormalizeEmailDomain(g.Key),
                Attended = g.Any(x => x.Attended),
                DurationSeconds = g.Where(x => x.DurationSeconds.HasValue).Select(x => x.DurationSeconds!.Value).DefaultIfEmpty().Sum(),
                DurationPercent = g.Where(x => x.DurationPercent.HasValue).Select(x => x.DurationPercent!.Value).DefaultIfEmpty().Max(),
                FirstJoinAt = g.Where(x => x.FirstJoinAt.HasValue).Select(x => x.FirstJoinAt!.Value).DefaultIfEmpty().Min(),
                LastLeaveAt = g.Where(x => x.LastLeaveAt.HasValue).Select(x => x.LastLeaveAt!.Value).DefaultIfEmpty().Max()
            })
            .Select(x =>
            {
                if (x.DurationSeconds == 0)
                    x.DurationSeconds = null;
                if (x.FirstJoinAt == default)
                    x.FirstJoinAt = null;
                if (x.LastLeaveAt == default)
                    x.LastLeaveAt = null;
                return x;
            })
            .ToList();

        return rows;
    }

    private static async Task<IReadOnlyList<NormalizedQaEntry>> ParseQaAsync(
        Guid sessionId,
        string ownerUserId,
        IFormFile file,
        DateTime importedAt,
        CancellationToken ct)
    {
        var csv = await ReadCsvAsync(file, ct);
        var questionIndex = csv.RequireHeader(
            "question",
            ["question", "questiontext", "qatext", "prompt"]);
        var answerIndex = csv.FindHeader("answer", ["answer", "answertext", "response", "responsetext"]);
        var answeredIndex = csv.FindHeader("isAnswered", ["isanswered", "answered", "answeredquestion"]);
        var askedAtIndex = csv.FindHeader("askedAt", ["askedat", "questionaskedat", "createdat"]);
        var answeredAtIndex = csv.FindHeader("answeredAt", ["answeredat", "responseat"]);
        var askedByDisplayNameIndex = csv.FindHeader("askedByDisplayName", ["askedbydisplayname", "askedby", "displayname", "name"]);
        var askedByEmailIndex = csv.FindHeader("askedByEmail", ["askedbyemail", "email", "emailaddress"]);

        return csv.Rows
            .Select(row =>
            {
                var questionText = ParseRequiredString(row, questionIndex, "question");
                var answerText = answerIndex is null
                    ? null
                    : ParseOptionalString(row, answerIndex.Value);
                var isAnswered = answeredIndex is null
                    ? !string.IsNullOrWhiteSpace(answerText)
                    : ParseRequiredBoolean(row, answeredIndex.Value, "isAnswered");
                var askedByEmail = askedByEmailIndex is null
                    ? null
                    : ParseOptionalEmail(row, askedByEmailIndex.Value, "askedByEmail");

                return new NormalizedQaEntry
                {
                    QaEntryId = Guid.NewGuid(),
                    SessionId = sessionId,
                    OwnerUserId = ownerUserId,
                    QuestionText = questionText,
                    AskedAt = askedAtIndex is null
                        ? importedAt
                        : ParseOptionalUtcDateTime(row, askedAtIndex.Value, "askedAt") ?? importedAt,
                    AskedByDisplayName = askedByDisplayNameIndex is null
                        ? null
                        : ParseOptionalString(row, askedByDisplayNameIndex.Value),
                    AskedByEmail = askedByEmail,
                    IsAnswered = isAnswered,
                    AnsweredAt = answeredAtIndex is null
                        ? null
                        : ParseOptionalUtcDateTime(row, answeredAtIndex.Value, "answeredAt"),
                    AnswerText = answerText
                };
            })
            .ToList();
    }

    private static async Task<CsvFile> ReadCsvAsync(IFormFile file, CancellationToken ct)
    {
        await using var sourceStream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await sourceStream.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        using var parser = new TextFieldParser(reader)
        {
            TextFieldType = FieldType.Delimited,
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };

        parser.SetDelimiters(",");

        if (parser.EndOfData)
        {
            throw new CsvImportValidationException(
                "validation_error",
                "The uploaded CSV file does not contain a header row.",
                new { fileName = file.FileName });
        }

        var headers = parser.ReadFields();
        if (headers is null || headers.Length == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new CsvImportValidationException(
                "validation_error",
                "The uploaded CSV file does not contain a usable header row.",
                new { fileName = file.FileName });
        }

        var normalizedHeaders = headers.Select(NormalizeHeader).ToArray();
        var rows = new List<CsvRow>();
        var lineNumber = 1;

        while (!parser.EndOfData)
        {
            ct.ThrowIfCancellationRequested();
            lineNumber++;
            string[]? fields;

            try
            {
                fields = parser.ReadFields();
            }
            catch (MalformedLineException ex)
            {
                throw new CsvImportValidationException(
                    "validation_error",
                    "The uploaded CSV file contains a malformed row.",
                    new { fileName = file.FileName, row = lineNumber, details = ex.Message });
            }

            if (fields is null)
                continue;

            if (fields.Length != headers.Length)
            {
                throw new CsvImportValidationException(
                    "validation_error",
                    "The uploaded CSV file contains a row with a different column count than the header.",
                    new { fileName = file.FileName, row = lineNumber, expectedColumns = headers.Length, actualColumns = fields.Length });
            }

            rows.Add(new CsvRow(lineNumber, fields));
        }

        return new CsvFile(file.FileName, headers, normalizedHeaders, rows);
    }

    private static string ParseRequiredEmail(CsvRow row, int index, string fieldName)
    {
        var value = ParseRequiredString(row, index, fieldName);
        return NormalizeAndValidateEmail(value, row.LineNumber, fieldName);
    }

    private static string? ParseOptionalEmail(CsvRow row, int index, string fieldName)
    {
        var value = ParseOptionalString(row, index);
        return string.IsNullOrWhiteSpace(value)
            ? null
            : NormalizeAndValidateEmail(value, row.LineNumber, fieldName);
    }

    private static string NormalizeAndValidateEmail(string value, int lineNumber, string fieldName)
    {
        try
        {
            var address = new MailAddress(value.Trim());
            return DomainNormalizer.NormalizeEmail(address.Address);
        }
        catch (FormatException)
        {
            throw new CsvImportValidationException(
                "validation_error",
                $"Row {lineNumber} contains an invalid email value.",
                new { row = lineNumber, field = fieldName, value });
        }
    }

    private static string ParseRequiredString(CsvRow row, int index, string fieldName)
    {
        var value = ParseOptionalString(row, index);
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        throw new CsvImportValidationException(
            "validation_error",
            $"Row {row.LineNumber} is missing a required {fieldName} value.",
            new { row = row.LineNumber, field = fieldName });
    }

    private static string? ParseOptionalString(CsvRow row, int index)
    {
        var value = row.Fields[index];
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DateTime? ParseOptionalUtcDateTime(CsvRow row, int index, string fieldName)
    {
        var value = ParseOptionalString(row, index);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();
        }

        throw new CsvImportValidationException(
            "validation_error",
            $"Row {row.LineNumber} contains an invalid {fieldName} value.",
            new { row = row.LineNumber, field = fieldName, value });
    }

    private static int? ParseOptionalInt(CsvRow row, int index, string fieldName)
    {
        var value = ParseOptionalString(row, index);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new CsvImportValidationException(
            "validation_error",
            $"Row {row.LineNumber} contains an invalid {fieldName} value.",
            new { row = row.LineNumber, field = fieldName, value });
    }

    private static decimal? ParseOptionalDecimal(CsvRow row, int index, string fieldName)
    {
        var value = ParseOptionalString(row, index);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        throw new CsvImportValidationException(
            "validation_error",
            $"Row {row.LineNumber} contains an invalid {fieldName} value.",
            new { row = row.LineNumber, field = fieldName, value });
    }

    private static bool ParseRequiredBoolean(CsvRow row, int index, string fieldName)
    {
        var value = ParseRequiredString(row, index, fieldName);
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "yes" or "y" or "1" => true,
            "false" or "no" or "n" or "0" => false,
            _ => throw new CsvImportValidationException(
                "validation_error",
                $"Row {row.LineNumber} contains an invalid {fieldName} value.",
                new { row = row.LineNumber, field = fieldName, value })
        };
    }

    private static string NormalizeHeader(string header)
        => new(header
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

    private sealed record CsvFile(
        string FileName,
        string[] Headers,
        string[] NormalizedHeaders,
        List<CsvRow> Rows)
    {
        public int? FindHeader(string fieldName, string[] aliases)
        {
            var normalizedAliases = aliases.Select(NormalizeHeader).ToArray();
            for (var i = 0; i < NormalizedHeaders.Length; i++)
            {
                if (normalizedAliases.Contains(NormalizedHeaders[i], StringComparer.Ordinal))
                    return i;
            }

            return null;
        }

        public int RequireHeader(string fieldName, string[] aliases)
        {
            var index = FindHeader(fieldName, aliases);
            if (index is not null)
                return index.Value;

            throw new CsvImportValidationException(
                "validation_error",
                $"The uploaded CSV file is missing a required {fieldName} column.",
                new
                {
                    fileName = FileName,
                    field = fieldName,
                    acceptedHeaders = aliases
                });
        }
    }

    private sealed record CsvRow(int LineNumber, string[] Fields);
}

public sealed record SessionImportOutcome(
    SessionImportSummaryDto? Summary,
    string? ErrorCode,
    string? Message,
    object? Details)
{
    public bool IsSuccess => Summary is not null && ErrorCode is null;

    public static SessionImportOutcome Success(SessionImportSummaryDto summary)
        => new(summary, null, null, null);

    public static SessionImportOutcome NotFound(string errorCode, string message)
        => new(null, errorCode, message, null);

    public static SessionImportOutcome ValidationFailed(string errorCode, string message, object? details = null)
        => new(null, errorCode, message, details);
}

public sealed class CsvImportValidationException : Exception
{
    public CsvImportValidationException(string errorCode, string message, object? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    public string ErrorCode { get; }
    public object? Details { get; }
}
