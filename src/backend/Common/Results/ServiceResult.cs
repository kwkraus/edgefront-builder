namespace EdgeFront.Builder.Common.Results;

public sealed class ServiceResult<T>
{
    public T? Value { get; private init; }
    public string? ErrorCode { get; private init; }
    public bool IsSuccess => ErrorCode is null;

    private ServiceResult() { }

    public static ServiceResult<T> Ok(T value) => new() { Value = value };
    public static ServiceResult<T> Fail(string errorCode) => new() { ErrorCode = errorCode };
}
