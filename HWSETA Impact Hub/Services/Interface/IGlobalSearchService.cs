using HWSETA_Impact_Hub.Models.ViewModels.Search;

namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface IGlobalSearchService
    {
        Task<GlobalSearchResponseVm> SearchAsync(string query, string currentUserId, CancellationToken ct);
    }
}
