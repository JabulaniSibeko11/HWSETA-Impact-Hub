namespace HWSETA_Impact_Hub.Infrastructure.Confugations
{
    public sealed class SmsOptions
    {
        public string Provider { get; set; } = "Dev";
        public string DefaultSenderId { get; set; } = "HWSETA";
        public GatewayOptions Gateway { get; set; } = new();

        public sealed class GatewayOptions
        {
            public string BaseUrl { get; set; } = "";
            public string ApiKey { get; set; } = "";
            public string ApiSecret { get; set; } = "";
            public string SendPath { get; set; } = "/send";
            public string AuthHeaderName { get; set; } = "Authorization";
            public string AuthHeaderValuePrefix { get; set; } = "Bearer ";
        }
    }
}
