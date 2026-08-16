using AgendaWithAspNetCoreMVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace AgendaWithAspNetCoreMVC.Models;

public class CalendarEventRequest : IValidatableObject
{
	public Guid? Id { get; set; }

	[Required]
	[StringLength(200)]
	public string Title { get; set; } = string.Empty;

	[StringLength(2000)]
	public string? Description { get; set; }

	[StringLength(500)]
	public string? Location { get; set; }

	[Required]
	public DateTime Start { get; set; }

	[Required]
	public DateTime End { get; set; }

	public bool AllDay { get; set; }

	[StringLength(20)]
	public string Color { get; set; } = "#0d6efd";

	public Guid? CategoryId { get; set; }

	public CalendarRecurrenceType RecurrenceType { get; set; } = CalendarRecurrenceType.None;

	[Range(1, 999)]
	public int RecurrenceInterval { get; set; } = 1;

	public DateTime? RecurrenceUntil { get; set; }

	public CalendarReminderType ReminderType { get; set; } = CalendarReminderType.None;

	[StringLength(4000)]
	public string? Notes { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (End < Start)
		{
			yield return new ValidationResult(
				"La data/ora di fine non può essere precedente all'inizio.", [nameof(End), nameof(Start)]);
		}

		if (RecurrenceType != CalendarRecurrenceType.None && RecurrenceUntil.HasValue && RecurrenceUntil.Value < Start)
		{
			yield return new ValidationResult(
				"La data di fine ricorrenza deve essere successiva alla data iniziale.", [nameof(RecurrenceUntil), nameof(Start)]);
		}

		if (string.IsNullOrWhiteSpace(Title))
		{
			yield return new ValidationResult("Il titolo è obbligatorio.", [nameof(Title)]);
		}
	}
}

//public class CalendarEventRequest
//{
//	public Guid? Id { get; set; }

//	[Required]
//	[StringLength(200)]
//	public string Title { get; set; } = string.Empty;

//	[StringLength(2000)]
//	public string? Description { get; set; }

//	[StringLength(500)]
//	public string? Location { get; set; }

//	[Required]
//	public DateTime Start { get; set; }

//	[Required]
//	public DateTime End { get; set; }

//	public bool AllDay { get; set; }

//	[StringLength(20)]
//	public string Color { get; set; } = "#0d6efd";
//}