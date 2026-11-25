using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using OrthoFlowAI.Api.Serialization;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Entities;

namespace OrthoFlowAI.Tests;

public class EntityJsonModifiersTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { EntityJsonModifiers.Apply } },
    };

    [Fact]
    public void Patient_response_does_not_leak_other_patients_clinic_or_device_token()
    {
        var doctor = new Doctor { Specialty = "Ortho" };
        var patient = new Patient { FirstName = "Omar", DeviceToken = "secret-push-token", Doctor = doctor, Clinic = new Clinic { Name = "Smile" } };
        doctor.Patients.Add(patient);
        doctor.Patients.Add(new Patient { FirstName = "OtherPatient" });

        var json = JsonSerializer.Serialize(patient, Options);

        Assert.Contains("\"firstName\":\"Omar\"", json);
        Assert.Contains("\"specialty\":\"Ortho\"", json);        // single related record kept
        Assert.DoesNotContain("OtherPatient", json);              // doctor's patient list removed
        Assert.DoesNotContain("secret-push-token", json);
        Assert.DoesNotContain("\"clinic\"", json);
    }

    [Fact]
    public void Clinic_itself_keeps_its_fields_but_not_its_lists()
    {
        var clinic = new Clinic { Name = "Smile" };
        clinic.Patients.Add(new Patient { FirstName = "Hidden" });

        var json = JsonSerializer.Serialize(clinic, Options);

        Assert.Contains("\"name\":\"Smile\"", json);
        Assert.DoesNotContain("Hidden", json);
    }

    [Fact]
    public void Dtos_are_not_touched()
    {
        var plan = new InstallmentPlanResponse { Installments = { new InstallmentResponse { Number = 1 } } };

        var json = JsonSerializer.Serialize(plan, Options);

        Assert.Contains("\"installments\":[{", json);
    }
}
