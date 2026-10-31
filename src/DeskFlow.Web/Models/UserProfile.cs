using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(32)]
    public string? Phone { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
