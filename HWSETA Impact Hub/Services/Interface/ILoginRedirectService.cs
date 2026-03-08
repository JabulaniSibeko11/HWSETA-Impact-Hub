namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface ILoginRedirectService
    {
        Task<(string controller, string action)> GetRedirectAsync(string userId);
    }
}
