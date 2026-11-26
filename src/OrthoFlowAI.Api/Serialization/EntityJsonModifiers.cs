using System.Collections;
using System.Text.Json.Serialization.Metadata;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;

namespace OrthoFlowAI.Api.Serialization;

/// <summary>
/// Many endpoints return EF entities directly. Without this, a single patient response
/// carried their doctor with the doctor's whole patient list, the clinic record, and so
/// on. Applied once to the API's JSON options, it trims every Domain entity to its own
/// fields plus single related records (e.g. an invoice's patient), without nested lists.
/// </summary>
public static class EntityJsonModifiers
{
    /// <summary>Fields that must never leave the server.</summary>
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Patient.DeviceToken),     // push-notification token
        "IpAddress",                     // signer's IP on documents
        "UserAgent",
    };

    public static void Apply(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || !IsEntity(typeInfo.Type))
            return;

        for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
        {
            var property = typeInfo.Properties[i];
            if (ShouldHide(typeInfo.Type, property))
                typeInfo.Properties.RemoveAt(i);
        }
    }

    internal static bool IsEntity(Type type) =>
        typeof(BaseEntity).IsAssignableFrom(type) && type.Namespace == typeof(Patient).Namespace;

    private static bool ShouldHide(Type owner, JsonPropertyInfo property)
    {
        if (SensitiveProperties.Contains(property.Name))
            return true;

        var type = property.PropertyType;

        // Nested lists of other records (Doctor.Patients, Clinic.Invoices, Invoice.Payments …).
        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
        {
            var element = type.IsArray ? type.GetElementType() : type.GetGenericArguments().FirstOrDefault();
            if (element != null && IsEntity(element))
                return true;
        }

        // The tenant record itself: every row belongs to the caller's clinic already.
        if (type == typeof(Clinic) && owner != typeof(Clinic))
            return true;

        return false;
    }
}
