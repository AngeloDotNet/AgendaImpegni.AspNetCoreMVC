using AgendaWithAspNetCoreMVC.Enums;

namespace AgendaWithAspNetCoreMVC.Models;

//public class CalendarEvent
//{
//	public Guid Id { get; set; } = Guid.NewGuid();
//	public string Title { get; set; } = string.Empty;
//	public string? Description { get; set; }
//	public string? Location { get; set; }
//	public DateTime Start { get; set; }
//	public DateTime End { get; set; }
//	public bool AllDay { get; set; }
//	public string Color { get; set; } = "#0d6efd";
//}

public class CalendarEvent
{
	public Guid Id { get; set; } = Guid.NewGuid();

	public string Title { get; set; } = string.Empty;
	public string? Description { get; set; }
	public string? Location { get; set; }

	public DateTime Start { get; set; }
	public DateTime End { get; set; }
	public bool AllDay { get; set; }

	public string Color { get; set; } = "#0d6efd";

	public Guid? CategoryId { get; set; }
	public CalendarCategory? Category { get; set; }

	public CalendarRecurrenceType RecurrenceType { get; set; } = CalendarRecurrenceType.None;
	public int RecurrenceInterval { get; set; } = 1;
	public DateTime? RecurrenceUntil { get; set; }

	public CalendarReminderType ReminderType { get; set; } = CalendarReminderType.None;
	public string? Notes { get; set; }
}