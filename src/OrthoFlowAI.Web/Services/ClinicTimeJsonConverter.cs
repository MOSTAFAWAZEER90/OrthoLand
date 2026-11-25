using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrthoFlowAI.Web.Services;

/// <summary>
/// The API stores and returns times in UTC; people at the clinic think in Cairo time.
/// Pages showed UTC (a 09:00 appointment appeared as 07:00). This converter makes every
/// DateTime the Web reads clinic-local, and turns clinic-local times it sends back into
/// UTC, independent of the web server's own time zone.
/// </summary>
public sealed class ClinicTimeJsonConverter : JsonConverter<DateTime>
{
    public static readonly TimeZoneInfo ClinicZone = FindZone();

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetDateTime();
        if (value.Kind == DateTimeKind.Local) value = value.ToUniversalTime();
        if (value.Kind != DateTimeKind.Utc) return value; // no zone info: leave as is

        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(value, ClinicZone), DateTimeKind.Unspecified);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            // Unspecified/Local values come from the UI and mean clinic time.
            _ => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), ClinicZone),
        };
        writer.WriteStringValue(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
    }

    private static TimeZoneInfo FindZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Local;
    }
}
