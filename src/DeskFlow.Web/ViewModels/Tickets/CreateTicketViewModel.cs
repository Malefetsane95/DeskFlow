using System.ComponentModel.DataAnnotations;

namespace DeskFlow.Web.ViewModels.Tickets;

public class CreateTicketViewModel
{
    [Required, StringLength(160, MinimumLength = 5)]
    [Display(Name = "What do you need help with?")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(5000, MinimumLength = 20)]
    [Display(Name = "Tell us what's happening")]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Choose a category.")]
    [Display(Name = "Which area is this about?")]
    public int CategoryId { get; set; }
}
