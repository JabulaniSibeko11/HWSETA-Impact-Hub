using HWSETA_Impact_Hub.Infrastructure.Confugations;
using HWSETA_Impact_Hub.Services.Interface;
using Microsoft.Extensions.Options;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class GatewaySmsSenderService : ISmsSenderService
    {
        private readonly SmsOptions _opt;
        private readonly HttpClient _http;

        public GatewaySmsSenderService(IOptions<SmsOptions> opt, HttpClient http)
        {
            _opt = opt.Value;
            _http = http;
        }

        private sealed class SmsRequest
        {
            public string To { get; set; } = "";
            public string From { get; set; } = "";
            public string Text { get; set; } = "";
        }

        private sealed class SmsResponse
        {
            public string? MessageId { get; set; }
            public string? Status { get; set; }
            public string? Error { get; set; }
        }

        public async Task<(bool ok, string? providerMessageId, string? error)> SendAsync(string toMsisdn, string message, CancellationToken ct)
        {
            try
            {
                var baseUrl = _opt.Gateway.BaseUrl?.TrimEnd('/') ?? "";
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return (false, null, "SMS Gateway BaseUrl not configured.");

                var url = baseUrl + (_opt.Gateway.SendPath.StartsWith("/") ? _opt.Gateway.SendPath : "/" + _opt.Gateway.SendPath);

                using var req = new HttpRequestMessage(HttpMethod.Post, url);

                // Very generic auth header
                if (!string.IsNullOrWhiteSpace(_opt.Gateway.ApiKey))
                {
                    var authValue = _opt.Gateway.AuthHeaderValuePrefix + _opt.Gateway.ApiKey;
                    req.Headers.TryAddWithoutValidation(_opt.Gateway.AuthHeaderName, authValue);
                }

                req.Content = JsonContent.Create(new SmsRequest
                {
                    To = toMsisdn,
                    From = _opt.DefaultSenderId,
                    Text = message
                });

                var res = await _http.SendAsync(req, ct);
                var body = await res.Content.ReadFromJsonAsync<SmsResponse>(cancellationToken: ct);

                if (!res.IsSuccessStatusCode)
                    return (false, null, body?.Error ?? $"SMS failed: HTTP {(int)res.StatusCode}");

                return (true, body?.MessageId, null);
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}