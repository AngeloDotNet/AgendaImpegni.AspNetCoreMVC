using AgendaWithAspNetCoreMVC.Models;
using AgendaWithAspNetCoreMVC.Services;
using AgendaWithAspNetCoreMVC.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AgendaWithAspNetCoreMVC.Controllers;

//public class AgendaController : Controller
//{
//	public IActionResult Index()
//	{
//		var vm = new AgendaPageViewModel();
//		return View(vm);
//	}
//}

public class AgendaController(ICalendarService calendarService) : Controller
{
	public async Task<IActionResult> Index(string? query, Guid? categoryId)
	{
		var categories = await calendarService.GetCategoriesAsync();
		var todayEvents = await calendarService.SearchAsync(new CalendarSearchRequest
		{
			Query = query,
			CategoryId = categoryId,
			Day = DateTime.Today
		});

		//var vm = new AgendaPageViewModel
		//{
		//	Categories = categories,
		//	TodayEvents = todayEvents,
		//	Query = query,
		//	CategoryId = categoryId
		//};

		var vm = new AgendaSidebarViewModel
		{
			Query = query,
			CategoryId = categoryId,
			Categories = categories,
			DayEvents = todayEvents
		};

		return View(vm);
	}
}