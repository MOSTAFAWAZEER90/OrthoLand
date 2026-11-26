using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrthoFlowAI.Application.DTOs.Common;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Infrastructure.Data;

namespace OrthoFlowAI.Api.Controllers;

/// <summary>
/// Small id + name lists for pickers in the UI (choose a patient or doctor by name
/// instead of typing an id). Tenant-filtered and returns only what a picker needs.
/// </summary>
[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController(ApplicationDbContext db) : ControllerBase
{
    private const int MaxResults = 20;

    /// <summary>Search patients by name or phone; or fetch one by id.</summary>
    [HttpGet("patients")]
    public async Task<IActionResult> Patients([FromQuery] string? q, [FromQuery] string? id)
    {
        var query = db.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(id))
            query = query.Where(p => p.Id == id);
        else if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => (p.FirstName + " " + p.LastName).ToLower().Contains(term)
                                     || (p.Phone != null && p.Phone.Contains(term)));
        }

        var items = await query
            .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
            .Take(MaxResults)
            .Select(p => new LookupItem { Id = p.Id, Name = p.FirstName + " " + p.LastName, Detail = p.Phone })
            .ToListAsync();

        return Ok(ApiResponse<List<LookupItem>>.SuccessResponse(items));
    }

    /// <summary>Doctors of the caller's clinic.</summary>
    [HttpGet("doctors")]
    public async Task<IActionResult> Doctors([FromQuery] string? q, [FromQuery] string? id)
    {
        var query = db.Doctors.AsNoTracking().Where(d => d.User != null);
        if (!string.IsNullOrWhiteSpace(id))
            query = query.Where(d => d.Id == id);
        else if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(d => (d.User!.FirstName + " " + d.User.LastName).ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(d => d.User!.FirstName)
            .Take(MaxResults)
            .Select(d => new LookupItem { Id = d.Id, Name = "Dr. " + d.User!.FirstName + " " + d.User.LastName, Detail = d.Specialty })
            .ToListAsync();

        return Ok(ApiResponse<List<LookupItem>>.SuccessResponse(items));
    }
}
