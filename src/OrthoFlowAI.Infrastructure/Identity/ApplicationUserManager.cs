using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrthoFlowAI.Infrastructure.Identity;

public class ApplicationUserManager : UserManager<ApplicationIdentityUser>
{
    public ApplicationUserManager(
        IUserStore<ApplicationIdentityUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<ApplicationIdentityUser> passwordHasher,
        IEnumerable<IUserValidator<ApplicationIdentityUser>> userValidators,
        IEnumerable<IPasswordValidator<ApplicationIdentityUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<ApplicationIdentityUser>> logger)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
    }

    public async Task<ApplicationUserManagerResult> CreateClinicOwnerAsync(
        ApplicationIdentityUser user, string password, string clinicId)
    {
        user.ClinicId = clinicId;
        user.Role = Domain.Enums.UserRole.ClinicOwner.ToString();
        user.CreatedAt = DateTime.UtcNow;

        var result = await CreateAsync(user, password);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, Domain.Enums.UserRole.ClinicOwner.ToString());
        }

        return new ApplicationUserManagerResult(user, result);
    }

    public async Task<ApplicationUserManagerResult> CreateDoctorAsync(
        ApplicationIdentityUser user, string password, string clinicId)
    {
        user.ClinicId = clinicId;
        user.Role = Domain.Enums.UserRole.Doctor.ToString();
        user.CreatedAt = DateTime.UtcNow;

        var result = await CreateAsync(user, password);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, Domain.Enums.UserRole.Doctor.ToString());
        }

        return new ApplicationUserManagerResult(user, result);
    }

    public async Task<ApplicationUserManagerResult> CreateStaffAsync(
        ApplicationIdentityUser user, string password, string clinicId, string? branchId = null)
    {
        user.ClinicId = clinicId;
        user.BranchId = branchId;
        user.Role = Domain.Enums.UserRole.Receptionist.ToString();
        user.CreatedAt = DateTime.UtcNow;

        var result = await CreateAsync(user, password);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, Domain.Enums.UserRole.Receptionist.ToString());
        }

        return new ApplicationUserManagerResult(user, result);
    }

    public async Task<ApplicationUserManagerResult> CreatePatientUserAsync(
        ApplicationIdentityUser user, string password, string clinicId)
    {
        user.ClinicId = clinicId;
        user.Role = Domain.Enums.UserRole.Patient.ToString();
        user.CreatedAt = DateTime.UtcNow;

        var result = await CreateAsync(user, password);
        if (result.Succeeded)
        {
            await AddToRoleAsync(user, Domain.Enums.UserRole.Patient.ToString());
        }

        return new ApplicationUserManagerResult(user, result);
    }

    public async Task<ApplicationUserManagerResult> ValidateAndSignInAsync(
        string email, string password, bool lockoutOnFailure = true)
    {
        var user = await FindByEmailAsync(email);
        if (user is null)
        {
            return new ApplicationUserManagerResult(
                null!,
                IdentityResult.Failed(new IdentityError { Description = "Invalid email or password." }));
        }

        if (!user.IsActive)
        {
            return new ApplicationUserManagerResult(
                user,
                IdentityResult.Failed(new IdentityError { Description = "Account is deactivated." }));
        }

        var validPassword = await CheckPasswordAsync(user, password);
        if (!validPassword)
        {
            return new ApplicationUserManagerResult(
                user,
                IdentityResult.Failed(new IdentityError { Description = "Invalid email or password." }));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await UpdateAsync(user);

        return new ApplicationUserManagerResult(user, IdentityResult.Success);
    }

    public Task<IList<Claim>> GetUserClaimsAsync(ApplicationIdentityUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new("clinic_id", user.ClinicId ?? string.Empty),
            new("role", user.Role),
            new("branch_id", user.BranchId ?? string.Empty)
        };

        return Task.FromResult<IList<Claim>>(claims);
    }
}

public class ApplicationUserManagerResult
{
    public ApplicationIdentityUser User { get; }
    public IdentityResult IdentityResult { get; }

    public ApplicationUserManagerResult(ApplicationIdentityUser user, IdentityResult identityResult)
    {
        User = user;
        IdentityResult = identityResult;
    }

    public bool Succeeded => IdentityResult.Succeeded;
}
