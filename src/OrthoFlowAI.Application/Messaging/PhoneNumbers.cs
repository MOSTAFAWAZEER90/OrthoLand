namespace OrthoFlowAI.Application.Messaging;

/// <summary>
/// Normalises phone numbers to international digits for WhatsApp (e.g. "201012345678").
/// Egyptian clinics store numbers in many local forms: 01012345678, +20 101 234 5678,
/// 00201012345678, 201012345678 …
/// </summary>
public static class PhoneNumbers
{
    public const string EgyptCountryCode = "20";

    /// <summary>
    /// Returns digits only with the country code (no "+"), or null when the input cannot
    /// be a valid number. Local numbers starting with a single 0 get <paramref name="defaultCountryCode"/>.
    /// </summary>
    public static string? ToInternationalDigits(string? phone, string defaultCountryCode = EgyptCountryCode)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;

        var trimmed = phone.Trim();
        var hasPlus = trimmed.StartsWith('+');
        var digits = new string(trimmed.Where(char.IsDigit).Select(ToAsciiDigit).ToArray());
        if (digits.Length == 0) return null;

        string result;
        if (hasPlus) result = digits;
        else if (digits.StartsWith("00")) result = digits[2..];
        else if (digits.StartsWith('0')) result = defaultCountryCode + digits[1..];
        else if (defaultCountryCode == EgyptCountryCode && digits.Length == 10 && digits.StartsWith('1'))
            result = EgyptCountryCode + digits; // "1012345678" written without the leading 0
        else result = digits;

        // E.164 allows at most 15 digits; anything under 8 is not a real number.
        if (result.Length is < 8 or > 15) return null;

        // Egyptian mobiles: 20 + 1[0125] + 8 digits.
        if (result.StartsWith(EgyptCountryCode) && result.Length > 2 && result[2] == '1'
            && !(result.Length == 12 && "0125".Contains(result[3])))
            return null;

        return result;
    }

    /// <summary>"+201012345678" form, or null.</summary>
    public static string? ToE164(string? phone, string defaultCountryCode = EgyptCountryCode)
        => ToInternationalDigits(phone, defaultCountryCode) is { } d ? "+" + d : null;

    // Arabic-Indic (٠-٩) and Eastern Arabic-Indic (۰-۹) digits typed on Arabic keyboards.
    private static char ToAsciiDigit(char c) => c switch
    {
        >= '٠' and <= '٩' => (char)('0' + (c - '٠')),
        >= '۰' and <= '۹' => (char)('0' + (c - '۰')),
        _ => c,
    };
}
