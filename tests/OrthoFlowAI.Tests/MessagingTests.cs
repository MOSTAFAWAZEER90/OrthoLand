using OrthoFlowAI.Application.Messaging;
using OrthoFlowAI.Infrastructure.WhatsApp;

namespace OrthoFlowAI.Tests;

public class PhoneNumbersTests
{
    [Theory]
    [InlineData("01012345678", "201012345678")]
    [InlineData("010 1234 5678", "201012345678")]
    [InlineData("+20 101 234 5678", "201012345678")]
    [InlineData("00201112345678", "201112345678")]
    [InlineData("201212345678", "201212345678")]
    [InlineData("1512345678", "201512345678")]          // written without the leading 0
    [InlineData("٠١٠١٢٣٤٥٦٧٨", "201012345678")]         // Arabic-Indic digits
    [InlineData("+966501234567", "966501234567")]       // non-Egyptian, kept as is
    public void Normalises_common_formats(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumbers.ToInternationalDigits(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0101234")]           // too short
    [InlineData("01312345678")]       // 013 is not an Egyptian mobile prefix
    public void Rejects_invalid_numbers(string? input)
    {
        Assert.Null(PhoneNumbers.ToInternationalDigits(input));
    }

    [Fact]
    public void E164_has_plus_prefix()
    {
        Assert.Equal("+201012345678", PhoneNumbers.ToE164("01012345678"));
    }
}

public class PatientMessagesTests
{
    // 2026-12-01 08:00 UTC = 10:00 in Cairo (UTC+2 in winter).
    private static readonly DateTime ApptUtc = new(2026, 12, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Appointment_reminder_is_arabic_and_in_cairo_time_by_default()
    {
        var msg = PatientMessages.AppointmentReminder("أحمد", "عيادة النور", "سارة", ApptUtc, language: null);

        Assert.Equal("ar", msg.Language);
        Assert.Contains("أحمد", msg.Text);
        Assert.Contains("عيادة النور", msg.Text);
        Assert.Contains("10:00", msg.Text); // not the UTC 08:00
        Assert.Equal(4, msg.TemplateParameters.Count);
    }

    [Fact]
    public void English_clinics_get_english_text()
    {
        var msg = PatientMessages.AppointmentReminder("Ahmed", "Smile", null, ApptUtc, "en");

        Assert.Equal("en", msg.Language);
        Assert.Contains("10:00 AM", msg.Text);
        Assert.DoesNotContain("Dr.", msg.Text); // no doctor given
    }

    [Theory]
    [InlineData(1500, true, "1,500 ج.م")]
    [InlineData(1500.5, true, "1,500.50 ج.م")]
    [InlineData(2083, false, "EGP 2,083")]
    public void Money_format(decimal amount, bool arabic, string expected)
    {
        Assert.Equal(expected, PatientMessages.Money(amount, arabic));
    }

    [Fact]
    public void Installment_messages_mention_number_and_amount()
    {
        var soon = PatientMessages.InstallmentDueSoon("منى", "عيادة", 3, 2083m, ApptUtc, "ar");
        var late = PatientMessages.InstallmentOverdue("منى", "عيادة", 3, 625m, ApptUtc, "ar");

        Assert.Contains("3", soon.Text);
        Assert.Contains("2,083 ج.م", soon.Text);
        Assert.Contains("625 ج.م", late.Text);
        Assert.Equal(PatientMessageKind.InstallmentOverdue, late.Kind);
    }

    [Fact]
    public void Click_to_chat_link_is_encoded()
    {
        var url = WhatsAppLinks.ClickToChat("01012345678", "أهلاً & شكراً");

        Assert.NotNull(url);
        Assert.StartsWith("https://wa.me/201012345678?text=", url);
        Assert.DoesNotContain(" ", url);
        Assert.DoesNotContain("&", url!.Split("?text=")[1]);
    }

    [Fact]
    public void Click_to_chat_is_null_for_invalid_phone()
    {
        Assert.Null(WhatsAppLinks.ClickToChat("123", "hi"));
    }
}

public class WhatsAppPayloadTests
{
    private static PatientMessage Msg() => new(PatientMessageKind.AppointmentReminder, "ar", "s", "النص",
        new[] { "أحمد", "عيادة", "الثلاثاء", "10:00" });

    [Fact]
    public void Uses_template_when_configured()
    {
        var meta = new MetaWhatsAppSettings { Templates = { ["AppointmentReminder"] = "appt_ar" } };

        var json = System.Text.Json.JsonSerializer.Serialize(MetaWhatsAppSender.BuildPayload("201012345678", Msg(), meta));

        Assert.Contains("\"type\":\"template\"", json);
        Assert.Contains("\"name\":\"appt_ar\"", json);
        Assert.Contains("\"code\":\"ar\"", json);
    }

    [Fact]
    public void Falls_back_to_text_without_template()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(
            MetaWhatsAppSender.BuildPayload("201012345678", Msg(), new MetaWhatsAppSettings()));

        Assert.Contains("\"type\":\"text\"", json);
        Assert.Contains("\"to\":\"201012345678\"", json);
    }

    [Fact]
    public void Metadata_round_trips_the_structured_message()
    {
        var original = Msg();
        var back = WhatsAppNotificationChannel.FromMetadata(original.Subject, original.Text,
            WhatsAppNotificationChannel.ToMetadata(original));

        Assert.Equal(original.Kind, back.Kind);
        Assert.Equal(original.Language, back.Language);
        Assert.Equal(original.TemplateParameters, back.TemplateParameters);
    }
}
