using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Tests;

/// <summary>Creates ApplicationDbContext instances over a shared in-memory database.</summary>
internal sealed class TestDb
{
    public const string ClinicA = "clinic-a";
    public const string ClinicB = "clinic-b";

    private readonly DbContextOptions<ApplicationDbContext> _options =
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("orthoflow-" + Guid.NewGuid())
            .Options;

    public ApplicationDbContext Create(string? clinicId = null)
    {
        var context = new ApplicationDbContext(_options);
        context.SetClinicId(clinicId);
        return context;
    }

    public static Patient Patient(string clinicId, string name, bool deleted = false) => new()
    {
        ClinicId = clinicId,
        FirstName = name,
        LastName = "Test",
        IsDeleted = deleted,
    };

    /// <summary>Seeds two clinics' patients without any tenant set.</summary>
    public async Task SeedTwoClinicsAsync()
    {
        await using var db = Create();
        db.Patients.AddRange(
            Patient(ClinicA, "A1"),
            Patient(ClinicA, "A2"),
            Patient(ClinicA, "A-deleted", deleted: true),
            Patient(ClinicB, "B1"));
        await db.SaveChangesAsync();
    }
}
