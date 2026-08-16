using AgendaWithAspNetCoreMVC.Enums;
using AgendaWithAspNetCoreMVC.Models;

namespace AgendaWithAspNetCoreMVC.Services;

public class MockCalendarService : ICalendarService
{
	private static readonly List<CalendarCategory> _categories =
	[
		new CalendarCategory { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Lavoro", Color = "#0d6efd" },
		new CalendarCategory { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), Name = "Personale", Color = "#198754" },
		new CalendarCategory { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), Name = "Salute", Color = "#dc3545" }
	];

	private static readonly List<CalendarEvent> _events =
	[
		new CalendarEvent
		{
			Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
			Title = "Riunione team",
			Description = "Weekly sync",
			Location = "Sala 1",
			Start = DateTime.Today.AddHours(9),
			End = DateTime.Today.AddHours(10),
			AllDay = false,
			Color = "#0d6efd",
			CategoryId = _categories[0].Id,
			Category = _categories[0],
			RecurrenceType = CalendarRecurrenceType.Weekly,
			RecurrenceInterval = 1,
			RecurrenceUntil = DateTime.Today.AddMonths(2),
			ReminderType = CalendarReminderType.FifteenMinutesBefore,
			Notes = "Portare report aggiornato"
		},
		new CalendarEvent
		{
			Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
			Title = "Evento multi-giorno",
			Description = "Da lunedì a mercoledì",
			Location = "Online",
			Start = DateTime.Today.AddDays(1).Date,
			End = DateTime.Today.AddDays(3).Date.AddHours(23).AddMinutes(59),
			AllDay = true,
			Color = "#198754",
			CategoryId = _categories[1].Id,
			Category = _categories[1],
			RecurrenceType = CalendarRecurrenceType.None,
			ReminderType = CalendarReminderType.None
		},
		new CalendarEvent
		{
			Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
			Title = "Dentista",
			Description = "Controllo annuale",
			Location = "Studio medico",
			Start = DateTime.Today.AddDays(2).AddHours(15),
			End = DateTime.Today.AddDays(2).AddHours(16),
			AllDay = false,
			Color = "#dc3545",
			CategoryId = _categories[2].Id,
			Category = _categories[2],
			ReminderType = CalendarReminderType.OneDayBefore,
			Notes = "Portare tessera sanitaria"
		}
	];

	public Task<List<CalendarEvent>> GetAllAsync()
		=> Task.FromResult(ExpandRecurring(_events).OrderBy(x => x.Start).ToList());

	public Task<List<CalendarCategory>> GetCategoriesAsync()
		=> Task.FromResult(_categories.OrderBy(x => x.Name).ToList());

	public Task<List<CalendarEvent>> SearchAsync(CalendarSearchRequest request)
	{
		var data = ExpandRecurring(_events).AsQueryable();

		if (!string.IsNullOrWhiteSpace(request.Query))
		{
			var q = request.Query.Trim();
			data = data.Where(x =>
				x.Title.Contains(q, StringComparison.OrdinalIgnoreCase) ||
				(x.Description != null && x.Description.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
				(x.Location != null && x.Location.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
				(x.Notes != null && x.Notes.Contains(q, StringComparison.OrdinalIgnoreCase)));
		}

		if (request.CategoryId.HasValue)
		{
			data = data.Where(x => x.CategoryId == request.CategoryId.Value);
		}

		if (request.Day.HasValue)
		{
			var d = request.Day.Value.Date;
			data = data.Where(x => x.Start.Date <= d && x.End.Date >= d);
		}

		return Task.FromResult(data.OrderBy(x => x.Start).ToList());
	}

	public Task<CalendarEvent?> GetByIdAsync(Guid id)
		=> Task.FromResult(ExpandRecurring(_events).FirstOrDefault(x => x.Id == id));

	public Task<CalendarEvent> CreateAsync(CalendarEventRequest request)
	{
		var category = _categories.FirstOrDefault(x => x.Id == request.CategoryId);

		var ev = new CalendarEvent
		{
			Id = Guid.NewGuid(),
			Title = request.Title,
			Description = request.Description,
			Location = request.Location,
			Start = request.Start,
			End = request.End,
			AllDay = request.AllDay,
			Color = string.IsNullOrWhiteSpace(request.Color) ? category?.Color ?? "#0d6efd" : request.Color,
			CategoryId = request.CategoryId,
			Category = category,
			RecurrenceType = request.RecurrenceType,
			RecurrenceInterval = request.RecurrenceInterval,
			RecurrenceUntil = request.RecurrenceUntil,
			ReminderType = request.ReminderType,
			Notes = request.Notes
		};

		_events.Add(ev);
		return Task.FromResult(ev);
	}

	public Task<CalendarEvent?> UpdateAsync(CalendarEventRequest request)
	{
		if (request.Id is null)
			return Task.FromResult<CalendarEvent?>(null);

		var ev = _events.FirstOrDefault(x => x.Id == request.Id.Value);
		if (ev is null)
			return Task.FromResult<CalendarEvent?>(null);

		var category = _categories.FirstOrDefault(x => x.Id == request.CategoryId);

		ev.Title = request.Title;
		ev.Description = request.Description;
		ev.Location = request.Location;
		ev.Start = request.Start;
		ev.End = request.End;
		ev.AllDay = request.AllDay;
		ev.Color = string.IsNullOrWhiteSpace(request.Color) ? category?.Color ?? ev.Color : request.Color;
		ev.CategoryId = request.CategoryId;
		ev.Category = category;
		ev.RecurrenceType = request.RecurrenceType;
		ev.RecurrenceInterval = request.RecurrenceInterval;
		ev.RecurrenceUntil = request.RecurrenceUntil;
		ev.ReminderType = request.ReminderType;
		ev.Notes = request.Notes;

		return Task.FromResult<CalendarEvent?>(ev);
	}

	public Task<bool> DeleteAsync(Guid id)
	{
		var ev = _events.FirstOrDefault(x => x.Id == id);
		if (ev is null) return Task.FromResult(false);

		_events.Remove(ev);
		return Task.FromResult(true);
	}

	public Task<bool> MoveAsync(CalendarEventMoveRequest request)
	{
		var ev = _events.FirstOrDefault(x => x.Id == request.Id);
		if (ev is null) return Task.FromResult(false);

		ev.Start = request.Start;
		ev.End = request.End;
		ev.AllDay = request.AllDay;
		return Task.FromResult(true);
	}

	private static List<CalendarEvent> ExpandRecurring(List<CalendarEvent> source)
	{
		var result = new List<CalendarEvent>();

		foreach (var ev in source)
		{
			if (ev.RecurrenceType == CalendarRecurrenceType.None)
			{
				result.Add(ev);
				continue;
			}

			var until = ev.RecurrenceUntil ?? DateTime.Today.AddMonths(6);
			var currentStart = ev.Start;
			var currentEnd = ev.End;

			while (currentStart <= until)
			{
				result.Add(new CalendarEvent
				{
					Id = ev.Id,
					Title = ev.Title,
					Description = ev.Description,
					Location = ev.Location,
					Start = currentStart,
					End = currentEnd,
					AllDay = ev.AllDay,
					Color = ev.Color,
					CategoryId = ev.CategoryId,
					Category = ev.Category,
					RecurrenceType = ev.RecurrenceType,
					RecurrenceInterval = ev.RecurrenceInterval,
					RecurrenceUntil = ev.RecurrenceUntil,
					ReminderType = ev.ReminderType,
					Notes = ev.Notes
				});

				currentStart = ev.RecurrenceType switch
				{
					CalendarRecurrenceType.Daily => currentStart.AddDays(ev.RecurrenceInterval),
					CalendarRecurrenceType.Weekly => currentStart.AddDays(7 * ev.RecurrenceInterval),
					CalendarRecurrenceType.Monthly => currentStart.AddMonths(ev.RecurrenceInterval),
					CalendarRecurrenceType.Yearly => currentStart.AddYears(ev.RecurrenceInterval),
					_ => currentStart
				};

				currentEnd = ev.RecurrenceType switch
				{
					CalendarRecurrenceType.Daily => currentEnd.AddDays(ev.RecurrenceInterval),
					CalendarRecurrenceType.Weekly => currentEnd.AddDays(7 * ev.RecurrenceInterval),
					CalendarRecurrenceType.Monthly => currentEnd.AddMonths(ev.RecurrenceInterval),
					CalendarRecurrenceType.Yearly => currentEnd.AddYears(ev.RecurrenceInterval),
					_ => currentEnd
				};
			}
		}

		return result;
	}
}

//public class MockCalendarService : ICalendarService
//{
//	private static readonly List<CalendarEvent> _events =
//	[
//		new CalendarEvent
//		{
//			Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
//			Title = "Riunione team",
//			Description = "Weekly sync",
//			Location = "Sala 1",
//			Start = DateTime.Today.AddHours(9),
//			End = DateTime.Today.AddHours(10),
//			AllDay = false,
//			Color = "#0d6efd"
//		},
//		new CalendarEvent
//		{
//			Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
//			Title = "Evento multi-giorno",
//			Description = "Da lunedì a mercoledì",
//			Location = "Online",
//			Start = DateTime.Today.AddDays(1).Date,
//			End = DateTime.Today.AddDays(3).Date.AddHours(23).AddMinutes(59),
//			AllDay = true,
//			Color = "#198754"
//		},
//		new CalendarEvent
//		{
//			Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
//			Title = "Dentista",
//			Description = "Controllo annuale",
//			Location = "Studio medico",
//			Start = DateTime.Today.AddDays(2).AddHours(15),
//			End = DateTime.Today.AddDays(2).AddHours(16),
//			AllDay = false,
//			Color = "#dc3545"
//		}
//	];

//	public Task<List<CalendarEvent>> GetAllAsync()
//		=> Task.FromResult(_events.OrderBy(x => x.Start).ToList());

//	public Task<CalendarEvent?> GetByIdAsync(Guid id)
//		=> Task.FromResult(_events.FirstOrDefault(x => x.Id == id));

//	public Task<CalendarEvent> CreateAsync(CalendarEventRequest request)
//	{
//		var ev = new CalendarEvent
//		{
//			Id = Guid.NewGuid(),
//			Title = request.Title,
//			Description = request.Description,
//			Location = request.Location,
//			Start = request.Start,
//			End = request.End,
//			AllDay = request.AllDay,
//			Color = request.Color
//		};

//		_events.Add(ev);
//		return Task.FromResult(ev);
//	}

//	public Task<CalendarEvent?> UpdateAsync(CalendarEventRequest request)
//	{
//		if (request.Id is null)
//			return Task.FromResult<CalendarEvent?>(null);

//		var ev = _events.FirstOrDefault(x => x.Id == request.Id.Value);
//		if (ev is null)
//			return Task.FromResult<CalendarEvent?>(null);

//		ev.Title = request.Title;
//		ev.Description = request.Description;
//		ev.Location = request.Location;
//		ev.Start = request.Start;
//		ev.End = request.End;
//		ev.AllDay = request.AllDay;
//		ev.Color = request.Color;

//		return Task.FromResult<CalendarEvent?>(ev);
//	}

//	public Task<bool> DeleteAsync(Guid id)
//	{
//		var ev = _events.FirstOrDefault(x => x.Id == id);
//		if (ev is null)
//			return Task.FromResult(false);

//		_events.Remove(ev);
//		return Task.FromResult(true);
//	}

//	public Task<bool> MoveAsync(CalendarEventMoveRequest request)
//	{
//		var ev = _events.FirstOrDefault(x => x.Id == request.Id);
//		if (ev is null)
//			return Task.FromResult(false);

//		ev.Start = request.Start;
//		ev.End = request.End;
//		ev.AllDay = request.AllDay;

//		return Task.FromResult(true);
//	}
//}