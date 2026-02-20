using System.Security.Claims;

namespace HWSETA_Impact_Hub.Infrastructure.Identity
{
    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _http;

        public CurrentUserService(IHttpContextAccessor http)
        {
            _http = http;
        }

        public bool IsAuthenticated =>
            _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

        public string? UserId =>
            _http.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? Email =>
            _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
            ?? _http.HttpContext?.User?.Identity?.Name;

        public string? Role =>
            _http.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
    }
}
