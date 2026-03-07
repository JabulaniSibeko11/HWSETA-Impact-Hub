namespace HWSETA_Impact_Hub.Services.Interface
{
    public interface ISaIdParserService
    {
        SaIdParseResult Parse(string? idNumber);
    }

    public sealed class SaIdParseResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public bool? IsMale { get; set; }
        public bool? IsSouthAfricanCitizen { get; set; }

        public string NormalisedId { get; set; } = "";
    }
}
