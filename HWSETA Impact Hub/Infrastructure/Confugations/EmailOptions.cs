namespace HWSETA_Impact_Hub.Infrastructure.Confugations
{
    public sealed class EmailOptions
    {
        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "";
        public SmtpOptions Smtp { get; set; } = new();

        public sealed class SmtpOptions
        {
            public string Host { get; set; } = "";
            public int Port { get; set; } = 25;
            public bool EnableSsl { get; set; } = true;
            public string Username { get; set; } = "";
            public string Password { get; set; } = "";
        }
    }
}
