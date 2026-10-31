using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required, StringLength(120)]
    [Display(Name = "Full name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Department { get; set; }

    [Required, DataType(DataType.Password), MinLength(12)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
