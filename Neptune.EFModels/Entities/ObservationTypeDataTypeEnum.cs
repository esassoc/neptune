namespace Neptune.EFModels.Entities;

public enum ObservationTypeDataTypeEnum
{
    PassFail,
    Numeric,
    Text
}

public class ObservationTypeDataTypeValidationResult
{
    public bool IsValid { get; set; }
    public object? ParsedValue { get; set; }
    public string? ErrorMessage { get; set; }
}

public static class ObservationTypeDataTypeEnumExtensions
{
    public static string ToDisplayString(this ObservationTypeDataTypeEnum value)
    {
        return value switch
        {
            ObservationTypeDataTypeEnum.PassFail => "Pass/Fail",
            ObservationTypeDataTypeEnum.Numeric => "Numeric",
            ObservationTypeDataTypeEnum.Text => "Text",
            _ => value.ToString()
        };
    }

    public static ObservationTypeDataTypeValidationResult ValidateAndParse(this ObservationTypeDataTypeEnum dataType, string value)
    {
        switch (dataType)
        {
            case ObservationTypeDataTypeEnum.PassFail:
                if (value is not null && (value.ToUpperInvariant().Trim() == "PASS" || value.ToUpperInvariant().Trim() == "FAIL"))
                    return new ObservationTypeDataTypeValidationResult { IsValid = true, ParsedValue = value.ToUpperInvariant().Trim() == "PASS" ? "true" : "false" };
                return new ObservationTypeDataTypeValidationResult { IsValid = false, ErrorMessage = "Value must be 'PASS' or 'FAIL'." };

            case ObservationTypeDataTypeEnum.Numeric:
                if (decimal.TryParse(value, out var dec))
                    //We'll return the initial value here, we really just wanted to ensure it was a valid number
                    return new ObservationTypeDataTypeValidationResult { IsValid = true, ParsedValue = value };
                return new ObservationTypeDataTypeValidationResult { IsValid = false, ErrorMessage = "Value must be a valid number." };

            case ObservationTypeDataTypeEnum.Text:
                if (value != null)
                    return new ObservationTypeDataTypeValidationResult { IsValid = true, ParsedValue = value };
                return new ObservationTypeDataTypeValidationResult { IsValid = false, ErrorMessage = "Value cannot be null." };

            default:
                return new ObservationTypeDataTypeValidationResult { IsValid = false, ErrorMessage = "Unknown data type." };
        }
    }
}