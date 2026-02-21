namespace HWSETA_Impact_Hub.Domain.Entities
{
    public enum FormStatus
    {
        Draft = 1,
        Published = 2,
        Archived = 3
    }

    public enum FormFieldType
    {
        // Text
        ShortText = 1,
        LongText = 2,
        Email = 3,
        Phone = 4,
        Url = 5,

        // Numbers & dates
        Number = 10,
        Decimal = 11,
        Date = 12,
        Time = 13,

        // Choice
        Dropdown = 20,
        Radio = 21,
        Checkbox = 22,
        YesNo = 23,

        // Advanced
        Rating = 30,          // 1..5 etc
        FileUpload = 31,      // store meta + path
        Signature = 32,       // later (base64 or file)
        LikertMatrix = 33,    // later
    }

    public enum ConditionOperator
    {
        Equals = 1,
        NotEquals = 2,
        Contains = 3,
        GreaterThan = 4,
        LessThan = 5,
        IsAnswered = 6,
        IsNotAnswered = 7
    }
}
