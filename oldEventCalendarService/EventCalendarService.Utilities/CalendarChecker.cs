using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace EventCalendarService.Utilities;

public class CalendarChecker
{
	public string CalendarFriendlyName { get; }

	public string CalendarUserUPN { get; }

	public string ClientId { get; }

	public string TenantID { get; }

	public string SecretKey { get; }

	private static List<SimpleAppointment> SimpleAppointmentList { get; set; }

	public CalendarChecker(string calendarUserUPN, string calendarName, string clientId, string tenantID, string secretKey)
	{
		CalendarFriendlyName = calendarName;
		CalendarUserUPN = calendarUserUPN;
		ClientId = clientId;
		TenantID = tenantID;
		SecretKey = secretKey;
	}

	private static async Task AppointmentLookupAsync(string calendarUserUPN, string calendarName, string clientId, string tenantID, string secretKey, DateTime startDate, DateTime endDate)
	{
		ClientCredentialProvider authProvider = new ClientCredentialProvider(ConfidentialClientApplicationBuilder.Create(clientId).WithTenantId(tenantID).WithClientSecret(secretKey)
			.Build());
		GraphServiceClient graphClient = new GraphServiceClient((IAuthenticationProvider)authProvider, (IHttpProvider)null);
		List<Calendar> calendarList = (await graphClient.Users[calendarUserUPN ?? ""].Calendars.Request().GetAsync()).ToList();
		List<QueryOption> queryOptions = new List<QueryOption>
		{
			new QueryOption("startDateTime", startDate.ToString("o")),
			new QueryOption("endDateTime", endDate.ToString("o")),
			new QueryOption("$top", "255")
		};
		foreach (Calendar calendar in calendarList.Where((Calendar c) => c.Name == calendarName))
		{
			ICalendarCalendarViewCollectionPage obj = await graphClient.Users[calendarUserUPN ?? ""].Calendars[calendar.Id ?? ""].CalendarView.Request(queryOptions).Expand("attachments").GetAsync();
			SimpleAppointmentList = new List<SimpleAppointment>();
			foreach (Event calEvent in obj)
			{
				bool isAllDay = calEvent.IsAllDay.HasValue && calEvent.IsAllDay.Value;
				DateTime startDateAdjusted;
				DateTime endDateAdjusted;
				if (!isAllDay)
				{
					startDateAdjusted = DateTime.Parse(calEvent.Start.DateTime).ToLocalTime();
					endDateAdjusted = DateTime.Parse(calEvent.End.DateTime).ToLocalTime();
				}
				else
				{
					startDateAdjusted = DateTime.Parse(calEvent.Start.DateTime);
					endDateAdjusted = DateTime.Parse(calEvent.End.DateTime);
				}
				string eventBody = calEvent.Body.Content;
				foreach (Attachment attachment in calEvent.Attachments)
				{
					if (attachment.IsInline.HasValue && attachment.IsInline.Value && attachment is FileAttachment && attachment.ContentType.Contains("image"))
					{
						FileAttachment fileAttachment = attachment as FileAttachment;
						byte[] contentBytes = fileAttachment.ContentBytes;
						string imageContentIDToReplace = "cid:" + fileAttachment.ContentId;
						eventBody = eventBody.Replace(imageContentIDToReplace, $"data:image;base64,{Convert.ToBase64String(contentBytes)}");
					}
				}
				SimpleAppointment appointment = new SimpleAppointment(startDateAdjusted, endDateAdjusted, calEvent.Location.DisplayName, calEvent.Subject, eventBody, calEvent.Id, isAllDay, GetColouring(calEvent));
				SimpleAppointmentList.Add(appointment);
			}
		}
	}

	public static DateTime GetNextWeekday(DateTime start, System.DayOfWeek day)
	{
		int daysToAdd = (day - start.DayOfWeek + 7) % 7;
		return start.AddDays(daysToAdd);
	}

	public async Task<List<SimpleAppointment>> GetTodaysAppointments()
	{
		return await GetRangeOfAppointments(DateTime.Today, DateTime.Today.AddHours(23.0).AddMinutes(59.0));
	}

	public async Task<List<SimpleAppointment>> GetRangeOfAppointments(DateTime startDate, DateTime endDate)
	{
		await AppointmentLookupAsync(CalendarUserUPN, CalendarFriendlyName, ClientId, TenantID, SecretKey, startDate, endDate);
		return SimpleAppointmentList;
	}

	private static string GetColouring(Event appointment)
	{
		if (appointment.Categories.Contains("Holiday") || appointment.Categories.Contains("Holidays"))
		{
			return "#41DC6A";
		}
		if (appointment.Categories.Contains("Payday"))
		{
			return "#FBB117";
		}
		if (appointment.Categories.Contains("Community Event") || appointment.Categories.Contains("Giving Back"))
		{
			return "#D82231";
		}
		if (appointment.Categories.Contains("Webinar") || appointment.Categories.Contains("Staff Webinar"))
		{
			return "#F47A20";
		}
		return string.Empty;
	}
}
