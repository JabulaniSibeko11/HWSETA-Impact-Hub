using HWSETA_Impact_Hub.Services.Interface;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HWSETA_Impact_Hub.Services.Implementations
{
    public sealed class SaIdParserService : ISaIdParserService
    {
        public SaIdParseResult Parse(string? idNumber)
        {
            var result = new SaIdParseResult();

            var id = (idNumber ?? "").Trim().Replace(" ", "").Replace("-", "");
            result.NormalisedId = id;

            if (string.IsNullOrWhiteSpace(id))
            {
                result.Error = "ID number is required.";
                return result;
            }

            if (!Regex.IsMatch(id, @"^\d{13}$"))
            {
                result.Error = "South African ID number must be exactly 13 digits.";
                return result;
            }

            // DOB
            var yy = id[..2];
            var mm = id.Substring(2, 2);
            var dd = id.Substring(4, 2);

            if (!int.TryParse(yy, out var yyNum) ||
                !int.TryParse(mm, out var mmNum) ||
                !int.TryParse(dd, out var ddNum))
            {
                result.Error = "ID number contains an invalid date section.";
                return result;
            }

            var currentYear2 = DateTime.Today.Year % 100;
            var century = yyNum <= currentYear2 ? 2000 : 1900;
            var fullYear = century + yyNum;

            if (!DateTime.TryParseExact(
                    $"{fullYear:D4}{mm}{dd}",
                    "yyyyMMdd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var dob))
            {
                result.Error = "ID number contains an invalid date of birth.";
                return result;
            }

            // Gender sequence
            var genderDigits = id.Substring(6, 4);
            if (!int.TryParse(genderDigits, out var seq))
            {
                result.Error = "ID number contains an invalid gender sequence.";
                return result;
            }

            result.IsMale = seq >= 5000;

            // Citizenship
            var citizenshipDigit = id[10];
            if (citizenshipDigit != '0' && citizenshipDigit != '1')
            {
                result.Error = "ID number contains an invalid citizenship digit.";
                return result;
            }

            result.IsSouthAfricanCitizen = citizenshipDigit == '0';

            // Luhn checksum validation
            if (!IsValidChecksum(id))
            {
                result.Error = "ID number checksum is invalid.";
                return result;
            }

            result.DateOfBirth = dob;
            result.IsValid = true;
            return result;
        }

        private static bool IsValidChecksum(string id)
        {
            // SA ID uses Luhn-like checksum
            var sumOdd = 0;
            for (int i = 0; i < 12; i += 2)
                sumOdd += id[i] - '0';

            var evenConcat = "";
            for (int i = 1; i < 12; i += 2)
                evenConcat += id[i];

            var doubled = (int.Parse(evenConcat) * 2).ToString();

            var sumEven = doubled.Sum(c => c - '0');
            var total = sumOdd + sumEven;
            var checkDigit = (10 - (total % 10)) % 10;

            return checkDigit == (id[12] - '0');
        }
    }
}
