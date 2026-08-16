namespace AgendaWithAspNetCoreMVC.Enums;

public enum CalendarReminderType
{
	None = 0,
	AtTime = 1,
	FiveMinutesBefore = 2,
	FifteenMinutesBefore = 3,
	ThirtyMinutesBefore = 4,
	OneHourBefore = 5,
	OneDayBefore = 6
}