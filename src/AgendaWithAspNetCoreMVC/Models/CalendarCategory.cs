namespace AgendaWithAspNetCoreMVC.Models;

public class CalendarCategory
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; } = string.Empty;
	public string Color { get; set; } = "#0d6efd";
}