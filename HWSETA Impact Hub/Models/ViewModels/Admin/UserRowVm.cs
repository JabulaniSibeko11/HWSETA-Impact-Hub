namespace HWSETA_Impact_Hub.Models.ViewModels.Admin
{
    public sealed class UserRowVm
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public bool EmailConfirmed { get; set; }
        public string Roles { get; set; } = "";
    }
}
