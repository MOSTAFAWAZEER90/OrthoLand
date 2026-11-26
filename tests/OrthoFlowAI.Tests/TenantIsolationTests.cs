using Microsoft.EntityFrameworkCore;

namespace OrthoFlowAI.Tests;

/// <summary>
/// Regression tests for clinic (tenant) data isolation. Before the fix, the
/// soft-delete query filter silently replaced the tenant filter, so every clinic
/// could read every other clinic's patients.
/// </summary>
public class TenantIsolationTests
{
    [Fact]
    public async Task Clinic_user_only_sees_own_clinic_patients()
    {
        var db = new TestDb();
        await db.SeedTwoClinicsAsync();

        await using var clinicA = db.Create(TestDb.ClinicA);
        var names = await clinicA.Patients.Select(p => p.FirstName).OrderBy(n => n).ToListAsync();

        Assert.Equal(new[] { "A1", "A2" }, names);
    }

    [Fact]
    public async Task Clinic_user_cannot_load_other_clinic_patient_by_id()
    {
        var db = new TestDb();
        await db.SeedTwoClinicsAsync();

        string bId;
        await using (var admin = db.Create())
            bId = (await admin.Patients.SingleAsync(p => p.FirstName == "B1")).Id;

        await using var clinicA = db.Create(TestDb.ClinicA);
        Assert.Null(await clinicA.Patients.FirstOrDefaultAsync(p => p.Id == bId));
    }

    [Fact]
    public async Task Soft_deleted_rows_stay_hidden_together_with_tenant_filter()
    {
        var db = new TestDb();
        await db.SeedTwoClinicsAsync();

        await using var clinicA = db.Create(TestDb.ClinicA);
        Assert.DoesNotContain(await clinicA.Patients.ToListAsync(), p => p.IsDeleted);

        await using var noTenant = db.Create();
        Assert.DoesNotContain(await noTenant.Patients.ToListAsync(), p => p.IsDeleted);
    }

    [Fact]
    public async Task Without_tenant_all_clinics_are_visible_for_admin_and_background_jobs()
    {
        var db = new TestDb();
        await db.SeedTwoClinicsAsync();

        await using var noTenant = db.Create();
        Assert.Equal(3, await noTenant.Patients.CountAsync());
    }

    [Fact]
    public async Task Filter_uses_each_context_instance_own_tenant()
    {
        // EF caches the model (and the filter) once per context type; make sure the
        // tenant value is still read from the current instance, not the first one.
        var db = new TestDb();
        await db.SeedTwoClinicsAsync();

        await using var a = db.Create(TestDb.ClinicA);
        await using var b = db.Create(TestDb.ClinicB);

        Assert.Equal(2, await a.Patients.CountAsync());
        Assert.Equal(1, await b.Patients.CountAsync());
    }

    [Fact]
    public async Task New_rows_without_clinic_are_stamped_with_current_tenant()
    {
        var db = new TestDb();
        await using (var clinicA = db.Create(TestDb.ClinicA))
        {
            clinicA.Patients.Add(TestDb.Patient(clinicId: "", name: "New"));
            await clinicA.SaveChangesAsync();
        }

        await using var noTenant = db.Create();
        var saved = await noTenant.Patients.SingleAsync(p => p.FirstName == "New");
        Assert.Equal(TestDb.ClinicA, saved.ClinicId);
    }

    [Fact]
    public async Task Writing_another_clinics_row_is_rejected()
    {
        var db = new TestDb();
        await using var clinicA = db.Create(TestDb.ClinicA);
        clinicA.Patients.Add(TestDb.Patient(TestDb.ClinicB, "Intruder"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => clinicA.SaveChangesAsync());
    }

    [Fact]
    public void Blank_clinic_id_means_no_tenant()
    {
        var db = new TestDb();
        using var context = db.Create("  ");
        Assert.Null(context.CurrentClinicId);
    }
}
