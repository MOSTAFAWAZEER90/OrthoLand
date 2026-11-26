using System.Globalization;

namespace OrthoFlowAI.Application.Messaging;

/// <summary>Kinds of patient messages. Each maps to an approved WhatsApp template.</summary>
public enum PatientMessageKind
{
    AppointmentReminder,
    InstallmentDueSoon,
    InstallmentOverdue,
    PaymentReceived,
}

/// <summary>
/// A rendered message: <see cref="Text"/> for free-form channels (wa.me links, SMS, email)
/// and <see cref="TemplateParameters"/> for WhatsApp Business templates, which Meta
/// requires for any message the clinic starts.
/// </summary>
public sealed record PatientMessage(PatientMessageKind Kind, string Language, string Subject, string Text,
    IReadOnlyList<string> TemplateParameters);

/// <summary>
/// Builds patient-facing texts in Arabic (default for Egyptian clinics) or English, with
/// times converted from UTC to the clinic's local time (Africa/Cairo by default).
/// </summary>
public static class PatientMessages
{
    public const string DefaultTimeZone = "Africa/Cairo";

    public static bool IsArabic(string? language) =>
        string.IsNullOrWhiteSpace(language)
        || language.StartsWith("ar", StringComparison.OrdinalIgnoreCase)
        || language.Equals("Arabic", StringComparison.OrdinalIgnoreCase);

    public static DateTime ToLocal(DateTime utc, string? timeZoneId = null)
    {
        var tz = FindTimeZone(timeZoneId ?? DefaultTimeZone);
        var asUtc = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(asUtc, tz);
    }

    public static PatientMessage AppointmentReminder(string patientName, string clinicName, string? doctorName,
        DateTime appointmentUtc, string? language, string? timeZoneId = null)
    {
        var local = ToLocal(appointmentUtc, timeZoneId);
        if (IsArabic(language))
        {
            var culture = Arabic();
            var date = local.ToString("dddd d MMMM", culture);
            var time = local.ToString("h:mm tt", culture);
            var doctor = string.IsNullOrWhiteSpace(doctorName) ? "" : $" مع د. {doctorName}";
            return new(PatientMessageKind.AppointmentReminder, "ar", "تذكير بموعد",
                $"أهلاً {patientName} 👋\nبنفكّرك بميعادك في {clinicName}{doctor} يوم {date} الساعة {time}.\nياريت توصل قبل الميعاد بـ 10 دقايق. لو محتاج تغيّر الميعاد كلّمنا.",
                new[] { patientName, clinicName, date, time });
        }
        else
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            var date = local.ToString("dddd, MMMM d", culture);
            var time = local.ToString("h:mm tt", culture);
            var doctor = string.IsNullOrWhiteSpace(doctorName) ? "" : $" with Dr. {doctorName}";
            return new(PatientMessageKind.AppointmentReminder, "en", "Appointment reminder",
                $"Hi {patientName},\nThis is a reminder of your appointment at {clinicName}{doctor} on {date} at {time}.\nPlease arrive 10 minutes early. Contact us if you need to reschedule.",
                new[] { patientName, clinicName, date, time });
        }
    }

    public static PatientMessage InstallmentDueSoon(string patientName, string clinicName, int number,
        decimal amount, DateTime dueUtc, string? language, string? timeZoneId = null)
    {
        var local = ToLocal(dueUtc, timeZoneId);
        if (IsArabic(language))
        {
            var date = local.ToString("d MMMM", Arabic());
            var money = Money(amount, true);
            return new(PatientMessageKind.InstallmentDueSoon, "ar", "تذكير بقسط",
                $"أهلاً {patientName}،\nبنفكّرك إن القسط رقم {number} بقيمة {money} لعيادة {clinicName} ميعاده يوم {date}.\nشكراً لالتزامك 🙏",
                new[] { patientName, number.ToString(CultureInfo.InvariantCulture), money, date, clinicName });
        }
        else
        {
            var date = local.ToString("MMMM d", CultureInfo.GetCultureInfo("en-US"));
            var money = Money(amount, false);
            return new(PatientMessageKind.InstallmentDueSoon, "en", "Instalment reminder",
                $"Hi {patientName},\nInstalment #{number} of {money} for {clinicName} is due on {date}.\nThank you!",
                new[] { patientName, number.ToString(CultureInfo.InvariantCulture), money, date, clinicName });
        }
    }

    public static PatientMessage InstallmentOverdue(string patientName, string clinicName, int number,
        decimal remaining, DateTime dueUtc, string? language, string? timeZoneId = null)
    {
        var local = ToLocal(dueUtc, timeZoneId);
        if (IsArabic(language))
        {
            var date = local.ToString("d MMMM", Arabic());
            var money = Money(remaining, true);
            return new(PatientMessageKind.InstallmentOverdue, "ar", "قسط متأخر",
                $"أهلاً {patientName}،\nالقسط رقم {number} (المتبقي {money}) كان ميعاده يوم {date} ولسه ماتدفعش.\nياريت تتواصل مع {clinicName} لترتيب الدفع.",
                new[] { patientName, number.ToString(CultureInfo.InvariantCulture), money, date, clinicName });
        }
        else
        {
            var date = local.ToString("MMMM d", CultureInfo.GetCultureInfo("en-US"));
            var money = Money(remaining, false);
            return new(PatientMessageKind.InstallmentOverdue, "en", "Overdue instalment",
                $"Hi {patientName},\nInstalment #{number} ({money} remaining) was due on {date} and is still unpaid.\nPlease contact {clinicName} to arrange payment.",
                new[] { patientName, number.ToString(CultureInfo.InvariantCulture), money, date, clinicName });
        }
    }

    public static PatientMessage PaymentReceived(string patientName, string clinicName, decimal amount,
        decimal remaining, string? language)
    {
        if (IsArabic(language))
        {
            var paid = Money(amount, true);
            var left = Money(remaining, true);
            var tail = remaining > 0 ? $"المتبقي عليك: {left}." : "كده الفاتورة اتدفعت بالكامل ✅";
            return new(PatientMessageKind.PaymentReceived, "ar", "تم استلام الدفع",
                $"أهلاً {patientName}،\nاستلمنا منك {paid}. شكراً ليك!\n{tail}\n— {clinicName}",
                new[] { patientName, paid, left, clinicName });
        }
        else
        {
            var paid = Money(amount, false);
            var left = Money(remaining, false);
            var tail = remaining > 0 ? $"Remaining balance: {left}." : "Your invoice is now fully paid ✅";
            return new(PatientMessageKind.PaymentReceived, "en", "Payment received",
                $"Hi {patientName},\nWe received your payment of {paid}. Thank you!\n{tail}\n— {clinicName}",
                new[] { patientName, paid, left, clinicName });
        }
    }

    /// <summary>"1,500 ج.م" / "EGP 1,500" — whole pounds when there are no piastres.</summary>
    public static string Money(decimal amount, bool arabic)
    {
        var format = amount == decimal.Truncate(amount) ? "#,0" : "#,0.00";
        var n = amount.ToString(format, CultureInfo.InvariantCulture);
        return arabic ? $"{n} ج.م" : $"EGP {n}";
    }

    private static CultureInfo Arabic()
    {
        // ar-EG month/day names but Latin digits, which is what Egyptian patients read on WhatsApp.
        var c = (CultureInfo)CultureInfo.GetCultureInfo("ar-EG").Clone();
        c.NumberFormat.DigitSubstitution = DigitShapes.None;
        c.DateTimeFormat.Calendar = new GregorianCalendar();
        return c;
    }

    private static TimeZoneInfo FindTimeZone(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (TimeZoneNotFoundException)
        {
            // Windows id fallback for Cairo.
            try { return TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time"); }
            catch (TimeZoneNotFoundException) { return TimeZoneInfo.Utc; }
        }
    }
}

/// <summary>Click-to-chat links (wa.me): work without any WhatsApp Business account.</summary>
public static class WhatsAppLinks
{
    public static string? ClickToChat(string? phone, string text, string defaultCountryCode = PhoneNumbers.EgyptCountryCode)
    {
        var digits = PhoneNumbers.ToInternationalDigits(phone, defaultCountryCode);
        return digits is null ? null : $"https://wa.me/{digits}?text={Uri.EscapeDataString(text)}";
    }
}
