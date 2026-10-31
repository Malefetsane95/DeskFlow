using System.ComponentModel.DataAnnotations;
using DeskFlow.Web.Models;

namespace DeskFlow.Web.ViewModels.Tickets;

public class AddTicketCommentViewModel
{
    [Required, StringLength(4000, MinimumLength = 1)]
    [Display(Name = "Your reply")]
    public string Body { get; set; } = string.Empty;

    [EnumDataType(typeof(CommentVisibility))]
    public CommentVisibility Visibility { get; set; } = CommentVisibility.Public;
}
