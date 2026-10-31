using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.ViewModels.Account;

public class ProfileViewModel
{
    [Required, StringLength(120)]
    [Display(Name = "Full name")]
    public string DisplayName { get; set; } = string.Empty;

    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Department { get; set; }

    [Phone, StringLength(32)]
    public string? Phone { get; set; }
}
