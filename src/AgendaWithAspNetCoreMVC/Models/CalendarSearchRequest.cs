namespace AgendaWithAspNetCoreMVC.Models;

public class CalendarSearchRequest
{
	public string? Query { get; set; }
	public Guid? CategoryId { get; set; }
	public DateTime? Day { get; set; }
}