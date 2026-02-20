namespace HWSETA_Impact_Hub.Infrastructure.RequestContext
{
    public sealed class RequestContextAccessor : IRequestContext
    {
        private readonly IHttpContextAccessor _http;

        public RequestContextAccessor(IHttpContextAccessor http)
        {
            _http = http;
        }

        public string CorrelationId =>
            _http.HttpContext?.TraceIdentifier ?? System.Guid.NewGuid().ToString("N");

        public string? IpAddress =>
            _http.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? UserAgent =>
            _http.HttpContext?.Request.Headers["User-Agent"].ToString();

        public string? Path =>
            _http.HttpContext?.Request.Path.Value;

        public string? Method =>
            _http.HttpContext?.Request.Method;
    }
}
