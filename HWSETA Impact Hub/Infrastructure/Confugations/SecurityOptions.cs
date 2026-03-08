namespace HWSETA_Impact_Hub.Infrastructure.Confugations
{
  

    public sealed class LoginRedirectsOptions
    {
        public string AdminDefaultController { get; set; } = "Admin";
        public string AdminDefaultAction { get; set; } = "Index";
        public string BeneficiaryDefaultController { get; set; } = "BeneficiaryPortal";
        public string BeneficiaryDefaultAction { get; set; } = "Index";
    }
}
