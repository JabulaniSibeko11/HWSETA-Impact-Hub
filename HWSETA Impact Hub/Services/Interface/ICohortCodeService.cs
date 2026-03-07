namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface ICohortCodeService
    {
        Task<string> GenerateNextAsync(CancellationToken ct);
    }
}
