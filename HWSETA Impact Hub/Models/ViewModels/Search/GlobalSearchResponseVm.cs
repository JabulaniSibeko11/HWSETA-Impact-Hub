namespace HWSETA_Impact_Hub.Models.ViewModels.Search
{
    public sealed class GlobalSearchResponseVm
    {
        public string Query { get; set; } = "";
        public List<GlobalSearchResultVm> Results { get; set; } = new();
    }
}
