using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace OrthoFlowAI.Infrastructure.Identity;

public class ApplicationRoleManager : RoleManager<IdentityRole>
{
    private readonly UserManager<ApplicationIdentityUser> _userManager;

    public ApplicationRoleManager(
        IRoleStore<IdentityRole> store,
        IEnumerable<IRoleValidator<IdentityRole>> roleValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        ILogger<RoleManager<IdentityRole>> logger,
        UserManager<ApplicationIdentityUser> userManager)
        : base(store, roleValidators, keyNormalizer, errors, logger)
    {
        _userManager = userManager;
    }

    public async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            Domain.Enums.UserRole.SuperAdmin.ToString(),
            Domain.Enums.UserRole.ClinicOwner.ToString(),
            Domain.Enums.UserRole.Doctor.ToString(),
            Domain.Enums.UserRole.Receptionist.ToString(),
            Domain.Enums.UserRole.Assistant.ToString(),
            Domain.Enums.UserRole.Patient.ToString()
        };

        foreach (var role in roles)
        {
            if (!await RoleExistsAsync(role))
            {
                await CreateAsync(new IdentityRole
                {
                    Name = role,
                    NormalizedName = role.ToUpperInvariant()
                });
            }
        }
    }

    public async Task<bool> AssignUserRoleAsync(string userId, string role)
    {
        if (!await RoleExistsAsync(role))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        if (await _userManager.IsInRoleAsync(user, role))
        {
            return true;
        }

        var result = await _userManager.AddToRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<bool> RemoveUserRoleAsync(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || !await _userManager.IsInRoleAsync(user, role))
        {
            return false;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, role);
        return result.Succeeded;
    }

    public async Task<IList<string>> GetUserRolesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new List<string>();
        }

        return await _userManager.GetRolesAsync(user);
    }
}
