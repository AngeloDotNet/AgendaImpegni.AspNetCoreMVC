using AgendaWithAspNetCoreMVC.Models;
using AgendaWithAspNetCoreMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgendaWithAspNetCoreMVC.Controllers;

[Route("api/calendar")]
[ApiController]
public class CalendarApiController(ICalendarService calendarService) : ControllerBase
{
	[HttpGet]
	public async Task<IActionResult> GetAll([FromQuery] string? query, [FromQuery] Guid? categoryId, [FromQuery] DateTime? day)
	{
		if (!string.IsNullOrWhiteSpace(query) || categoryId.HasValue || day.HasValue)
		{
			var filtered = await calendarService.SearchAsync(new CalendarSearchRequest
			{
				Query = query,
				CategoryId = categoryId,
				Day = day
			});

			return Ok(ToFullCalendarResult(filtered));
		}

		var events = await calendarService.GetAllAsync();
		return Ok(ToFullCalendarResult(events));
	}

	[HttpGet("categories")]
	public async Task<IActionResult> Categories()
	{
		var categories = await calendarService.GetCategoriesAsync();
		return Ok(categories);
	}

	[HttpGet("{id:guid}")]
	public async Task<IActionResult> GetById(Guid id)
	{
		var ev = await calendarService.GetByIdAsync(id);
		if (ev is null) return NotFound();
		return Ok(ev);
	}

	[HttpPost]
	public async Task<IActionResult> Create([FromBody] CalendarEventRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		var ev = await calendarService.CreateAsync(request);
		return Ok(ev);
	}

	[HttpPut("{id:guid}")]
	public async Task<IActionResult> Update(Guid id, [FromBody] CalendarEventRequest request)
	{
		request.Id = id;

		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		var ev = await calendarService.UpdateAsync(request);
		if (ev is null) return NotFound();

		return Ok(ev);
	}

	[HttpDelete("{id:guid}")]
	public async Task<IActionResult> Delete(Guid id)
	{
		var ok = await calendarService.DeleteAsync(id);
		if (!ok) return NotFound();

		return Ok();
	}

	[HttpPost("move")]
	public async Task<IActionResult> Move([FromBody] CalendarEventMoveRequest request)
	{
		if (!ModelState.IsValid)
			return BadRequest(ModelState);

		var ok = await calendarService.MoveAsync(request);
		if (!ok) return NotFound();

		return Ok();
	}

	private static object ToFullCalendarResult(IEnumerable<CalendarEvent> events)
		=> events.Select(x => new
		{
			id = x.Id.ToString(),
			title = x.Title,
			start = x.Start,
			end = x.End,
			allDay = x.AllDay,
			backgroundColor = x.Category?.Color ?? x.Color,
			borderColor = x.Category?.Color ?? x.Color,
			extendedProps = new
			{
				description = x.Description,
				location = x.Location,
				notes = x.Notes,
				categoryId = x.CategoryId,
				categoryName = x.Category?.Name,
				reminderType = x.ReminderType.ToString(),
				recurrenceType = x.RecurrenceType.ToString()
			}
		});
}

//[Route("api/calendar")]
//[ApiController]
//public class CalendarApiController(ICalendarService calendarService) : ControllerBase
//{
//	[HttpGet]
//	public async Task<IActionResult> GetAll()
//	{
//		var events = await calendarService.GetAllAsync();

//		return Ok(events.Select(x => new
//		{
//			id = x.Id.ToString(),
//			title = x.Title,
//			start = x.Start,
//			end = x.End,
//			allDay = x.AllDay,
//			backgroundColor = x.Color,
//			borderColor = x.Color,
//			extendedProps = new
//			{
//				description = x.Description,
//				location = x.Location
//			}
//		}));

//		//var result = events.Select(x => new CalendarEventDto
//		//{
//		//	Id = x.Id.ToString(),
//		//	Title = x.Title,
//		//	Start = x.Start,
//		//	End = x.End,
//		//	AllDay = x.AllDay,
//		//	Color = x.Color,
//		//	Description = x.Description,
//		//	Location = x.Location
//		//});

//		//return Ok(result);
//	}

//	[HttpGet("{id:guid}")]
//	public async Task<IActionResult> GetById(Guid id)
//	{
//		var ev = await calendarService.GetByIdAsync(id);

//		if (ev is null) return NotFound();

//		return Ok(ev);
//	}

//	[HttpPost]
//	public async Task<IActionResult> Create([FromBody] CalendarEventRequest request)
//	{
//		if (!ModelState.IsValid)
//			return BadRequest(ModelState);

//		var ev = await calendarService.CreateAsync(request);
//		return Ok(ev);
//	}

//	[HttpPut("{id:guid}")]
//	public async Task<IActionResult> Update(Guid id, [FromBody] CalendarEventRequest request)
//	{
//		request.Id = id;

//		if (!ModelState.IsValid)
//			return BadRequest(ModelState);

//		var ev = await calendarService.UpdateAsync(request);
//		if (ev is null) return NotFound();

//		return Ok(ev);
//	}

//	[HttpDelete("{id:guid}")]
//	public async Task<IActionResult> Delete(Guid id)
//	{
//		var ok = await calendarService.DeleteAsync(id);
//		if (!ok) return NotFound();

//		return Ok();
//	}

//	[HttpPost("move")]
//	public async Task<IActionResult> Move([FromBody] CalendarEventMoveRequest request)
//	{
//		if (!ModelState.IsValid)
//			return BadRequest(ModelState);

//		var ok = await calendarService.MoveAsync(request);
//		if (!ok) return NotFound();

//		return Ok();
//	}
//}