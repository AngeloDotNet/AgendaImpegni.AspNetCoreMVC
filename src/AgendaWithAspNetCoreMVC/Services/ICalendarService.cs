using AgendaWithAspNetCoreMVC.Models;

namespace AgendaWithAspNetCoreMVC.Services;

//public interface ICalendarService
//{
//	Task<List<CalendarEvent>> GetAllAsync();
//	Task<CalendarEvent?> GetByIdAsync(Guid id);
//	Task<CalendarEvent> CreateAsync(CalendarEventRequest request);
//	Task<CalendarEvent?> UpdateAsync(CalendarEventRequest request);
//	Task<bool> DeleteAsync(Guid id);
//	Task<bool> MoveAsync(CalendarEventMoveRequest request);
//}

public interface ICalendarService
{
	Task<List<CalendarEvent>> GetAllAsync();
	Task<List<CalendarEvent>> SearchAsync(CalendarSearchRequest request);
	Task<List<CalendarCategory>> GetCategoriesAsync();
	Task<CalendarEvent?> GetByIdAsync(Guid id);
	Task<CalendarEvent> CreateAsync(CalendarEventRequest request);
	Task<CalendarEvent?> UpdateAsync(CalendarEventRequest request);
	Task<bool> DeleteAsync(Guid id);
	Task<bool> MoveAsync(CalendarEventMoveRequest request);
}