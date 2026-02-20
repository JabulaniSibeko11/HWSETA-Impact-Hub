namespace HWSETA_Impact_Hub.Infrastructure.Identity
{
    public sealed class SecurityOptions
    {
        public List<string> Roles { get; set; } = new();
        public Dictionary<string, List<string>> Policies { get; set; } = new();

        public RegistrationOptions Registration { get; set; } = new();
        public BootstrapAdminOptions BootstrapAdmin { get; set; } = new();

        public List<AdminMenuItem> AdminMenu { get; set; } = new();
    }



    public sealed class RegistrationOptions
    {
        public string DefaultRole { get; set; } = "Beneficiary";
        public bool AllowPublicRegistration { get; set; } = false;
    }

    public sealed class BootstrapAdminOptions
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RequireConfirmedEmail { get; set; } = false;
    }

    public sealed class AdminMenuItem
    {
        public string Title { get; set; } = "";
        public string Controller { get; set; } = "";
        public string Action { get; set; } = "";
        public string Policy { get; set; } = "";
    }
}
