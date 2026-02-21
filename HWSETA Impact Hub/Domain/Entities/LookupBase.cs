using HWSETA_Impact_Hub.Domain.Common;

namespace HWSETA_Impact_Hub.Domain.Entities
{
    public abstract class LookupBase : BaseEntity
    {
        public string Code { get; set; } = "";   // e.g. "GP"
        public string Name { get; set; } = "";   // e.g. "Gauteng"
        public bool IsActive { get; set; } = true;
    }
    public sealed class Province : LookupBase { }
    public sealed class Gender : LookupBase { }
    public sealed class Race : LookupBase { }
    public sealed class CitizenshipStatus : LookupBase { }
    public sealed class DisabilityStatus : LookupBase { }  // Yes/No
    public sealed class DisabilityType : LookupBase { }    // Visual/Hearing/etc
    public sealed class EducationLevel : LookupBase { }
    public sealed class EmploymentStatus : LookupBase { }
    public sealed class QualificationType : LookupBase { } // Learnership/Internship/Skills
    public sealed class FundingType : LookupBase { }
    public sealed class EmployerRegistrationType : LookupBase { } // PTY/NPC/GOV
    public sealed class DocumentType : LookupBase { } // ID Copy/Certificate/etc
}
