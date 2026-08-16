using System.ComponentModel.DataAnnotations;

namespace AgendaWithAspNetCoreMVC.Models;

public class CalendarEventMoveRequest
{
	[Required]
	public Guid Id { get; set; }

	[Required]
	public DateTime Start { get; set; }

	[Required]
	public DateTime End { get; set; }

	public bool AllDay { get; set; }
}