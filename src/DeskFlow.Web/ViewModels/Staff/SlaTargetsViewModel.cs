using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.ViewModels.Staff;

public sealed class SlaTargetsViewModel
{
    public List<SlaTargetEditRowViewModel> Rules { get; init; } = [];
}

public sealed class SlaTargetEditRowViewModel : IValidatableObject
{
    public int PriorityId { get; init; }
    public string PriorityName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a response target.")]
    [Range(typeof(decimal), "0.25", "720", ErrorMessage = "Use a value from 0.25 to 720 hours.")]
    [Display(Name = "First response (hours)")]
    public decimal? ResponseTargetHours { get; init; }

    [Required(ErrorMessage = "Enter a resolution target.")]
    [Range(typeof(decimal), "0.25", "720", ErrorMessage = "Use a value from 0.25 to 720 hours.")]
    [Display(Name = "Resolution (hours)")]
    public decimal? ResolutionTargetHours { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ResponseTargetHours is decimal responseHours && responseHours * 60m % 15m != 0)
        {
            yield return new ValidationResult(
                "Use 15-minute increments, such as 1.25 hours.", [nameof(ResponseTargetHours)]);
        }

        if (ResolutionTargetHours is decimal resolutionHours && resolutionHours * 60m % 15m != 0)
        {
            yield return new ValidationResult(
                "Use 15-minute increments, such as 1.25 hours.", [nameof(ResolutionTargetHours)]);
        }
    }
}
