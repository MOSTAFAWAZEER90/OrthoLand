using System.ComponentModel.DataAnnotations;

namespace OrthoFlowAI.Application.DTOs;

public class CreateClinicRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Logo { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [Url]
    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(2000)]
    public string? BusinessHours { get; set; }

    [MaxLength(500)]
    public string? WorkingDays { get; set; }

    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    [Required]
    [MaxLength(10)]
    public string Language { get; set; } = string.Empty;
}

public class UpdateClinicRequest
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(500)]
    public string? Logo { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [Url]
    [MaxLength(200)]
    public string? Website { get; set; }

    [MaxLength(2000)]
    public string? BusinessHours { get; set; }

    [MaxLength(500)]
    public string? WorkingDays { get; set; }

    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    public bool? IsActive { get; set; }
}

public class ClinicResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Logo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? BusinessHours { get; set; }
    public string? WorkingDays { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
    public string? SubscriptionPlan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public string Language { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ClinicSettingsRequest
{
    [MaxLength(2000)]
    public string? BusinessHours { get; set; }

    [MaxLength(500)]
    public string? WorkingDays { get; set; }

    [MaxLength(7)]
    public string? PrimaryColor { get; set; }

    [MaxLength(7)]
    public string? SecondaryColor { get; set; }

    [MaxLength(10)]
    public string? Language { get; set; }

    [MaxLength(500)]
    public string? Logo { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }
}
