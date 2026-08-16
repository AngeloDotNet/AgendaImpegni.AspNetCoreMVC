namespace AgendaWithAspNetCoreMVC.Models;

public class CalendarEventDto
{
	public string Id { get; set; } = string.Empty;
	public string Title { get; set; } = string.Empty;
	public DateTime Start { get; set; }
	public DateTime? End { get; set; }
	public bool AllDay { get; set; }
	public string? Color { get; set; }
	public string? Description { get; set; }
	public string? Location { get; set; }
}