using AgendaWithAspNetCoreMVC.Services;
using System.Text.Json.Serialization;

namespace AgendaWithAspNetCoreMVC;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		//TODO: 1) Modelli dominio EF Core

		//// Add services to the container.
		//builder.Services.AddControllersWithViews();

		//builder.Services.AddControllersWithViews();
		builder.Services
			.AddControllersWithViews()
			.AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

		builder.Services.AddScoped<ICalendarService, MockCalendarService>();

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Home/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		app.UseHttpsRedirection();
		app.UseRouting();

		app.UseAuthorization();

		app.MapStaticAssets();
		app.MapControllerRoute(name: "default",
			//pattern: "{controller=Home}/{action=Index}/{id?}")
			pattern: "{controller=Agenda}/{action=Index}/{id?}").WithStaticAssets();

		app.Run();
	}
}
