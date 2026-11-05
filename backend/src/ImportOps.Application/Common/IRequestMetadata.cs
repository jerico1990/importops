namespace ImportOps.Application.Common;

public interface IRequestMetadata
{
    string? IpAddress { get; }
    string? UserAgent { get; }
    Guid? CorrelationId { get; }
}
