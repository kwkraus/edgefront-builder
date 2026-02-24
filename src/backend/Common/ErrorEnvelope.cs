namespace EdgeFront.Builder.Common;

public record ErrorEnvelope(string ErrorCode, string Message, string CorrelationId, object? Details = null);
