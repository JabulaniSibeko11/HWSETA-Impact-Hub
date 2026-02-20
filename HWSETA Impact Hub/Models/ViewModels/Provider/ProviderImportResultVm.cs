namespace HWSETA_Impact_Hub.Models.ViewModels.Provider
{
    public sealed class ProviderImportResultVm
    {
        public int TotalRows { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
