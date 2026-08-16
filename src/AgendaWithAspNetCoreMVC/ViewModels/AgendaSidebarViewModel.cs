using AgendaWithAspNetCoreMVC.Models;

namespace AgendaWithAspNetCoreMVC.ViewModels;

public class AgendaSidebarViewModel
{
	public string InitialView { get; set; } = "dayGridMonth";
	public DateTime InitialDate { get; set; } = DateTime.Today;
	public string Title => "Agenda personale";

	public string? Query { get; set; }
	public Guid? CategoryId { get; set; }
	public List<CalendarCategory> Categories { get; set; } = [];
	public List<CalendarEvent> DayEvents { get; set; } = [];
}