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

        /// <summary>
        /// Bootstrap Icons class name e.g. "bi-people" or "bi-mortarboard".
        /// When null the _AdminMenu partial falls back to its built-in controller→icon map.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Optional red badge counter shown on the nav link (e.g. pending approvals).
        /// Null or zero hides the badge entirely.
        /// </summary>
        public int? BadgeCount { get; set; }
    }
}