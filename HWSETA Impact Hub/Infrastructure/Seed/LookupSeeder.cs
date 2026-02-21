using DocumentFormat.OpenXml.Spreadsheet;
using HWSETA_Impact_Hub.Data;
using HWSETA_Impact_Hub.Domain.Entities;
using HWSETA_Impact_Hub.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace HWSETA_Impact_Hub.Infrastructure.Seed
{
    public static class LookupSeeder
    {
        public static async Task SeedAsync(ApplicationDbContext db)
        {
            await Seed(db.Provinces,
                ("EC", "Eastern Cape"),
                ("FS", "Free State"),
                ("GP", "Gauteng"),
                ("KZN", "KwaZulu-Natal"),
                ("LP", "Limpopo"),
                ("MP", "Mpumalanga"),
                ("NC", "Northern Cape"),
                ("NW", "North West"),
                ("WC", "Western Cape"));

            await Seed(db.Genders,
                ("M", "Male"),
                ("F", "Female"),
                ("O", "Other"));

            await Seed(db.Races,
                ("BL", "Black African"),
                ("CL", "Coloured"),
                ("IN", "Indian / Asian"),
                ("WH", "White"),
                ("OT", "Other"));

            await Seed(db.CitizenshipStatuses,
                ("SA", "South African Citizen"),
                ("PR", "Permanent Resident"),
                ("FR", "Foreign National"));

            await Seed(db.DisabilityStatuses,
                ("Y", "Yes"),
                ("N", "No"));

            await Seed(db.DisabilityTypes,
                ("VIS", "Visual"),
                ("HEA", "Hearing"),
                ("PHY", "Physical"),
                ("INT", "Intellectual"),
                ("PSY", "Psychosocial"),
                ("OTH", "Other"));

            await Seed(db.EducationLevels,
                ("NONE", "No Schooling"),
                ("PRI", "Primary"),
                ("SEC", "Secondary"),
                ("MAT", "Matric"),
                ("CERT", "Certificate"),
                ("DIP", "Diploma"),
                ("DEG", "Degree"),
                ("PG", "Postgraduate"));

            await Seed(db.EmploymentStatuses,
                ("EMP", "Employed"),
                ("UNE", "Unemployed"),
                ("SEL", "Self-Employed"),
                ("STU", "Student"));

            await Seed(db.QualificationTypes,
                ("LRN", "Learnership"),
                ("INT", "Internship"),
                ("SKL", "Skills Programme"));

            await Seed(db.FundingTypes,
                ("HW", "HWSETA Funded"),
                ("EMP", "Employer Funded"),
                ("NSF", "NSF Funded"));

            await Seed(db.EmployerRegistrationTypes,
                ("PTY", "Private Company"),
                ("NPC", "Non-Profit"),
                ("GOV", "Government"));

            await Seed(db.DocumentTypes,
                ("ID", "ID Copy"),
                ("PAS", "Passport"),
                ("CERT", "Qualification Certificate"),
                ("AGRE", "Agreement"));
        }

        private static async Task Seed<T>(
            DbSet<T> set,
            params (string code, string name)[] values
        ) where T : LookupBase, new()
        {
            foreach (var (code, name) in values)
            {
                if (!await set.AnyAsync(x => x.Code == code))
                {
                    set.Add(new T
                    {
                        Code = code,
                        Name = name,
                        IsActive = true,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                }
            }
        }
    }
}
