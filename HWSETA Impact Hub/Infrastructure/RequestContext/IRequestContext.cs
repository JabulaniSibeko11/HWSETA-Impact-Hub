namespace HWSETA_Impact_Hub.Infrastructure.RequestContext
{
    public interface IRequestContext
    {
        string CorrelationId { get; }
        string? IpAddress { get; }
        string? UserAgent { get; }
        string? Path { get; }
        string? Method { get; }
    }
}
