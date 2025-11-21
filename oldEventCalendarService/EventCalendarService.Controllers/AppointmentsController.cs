using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using EventCalendarService.ConfigObjects;
using EventCalendarService.Utilities;
using Newtonsoft.Json;

namespace EventCalendarService.Controllers;

[EnableCors("*", "*", "*")]
public class AppointmentsController : ApiController
{
	private CalendarChecker CalChecker()
	{
		string configFile = AppDomain.CurrentDomain.BaseDirectory + "apiConfig.json";
		GraphApiConfig apiConfig = new GraphApiConfig();
		if (!File.Exists(configFile))
		{
			apiConfig.CalendarName = "Event Calendar";
			File.WriteAllText(configFile, JsonConvert.SerializeObject(apiConfig));
		}
		apiConfig = JsonConvert.DeserializeObject<GraphApiConfig>(File.ReadAllText(configFile));
		return new CalendarChecker(apiConfig.CalendarUserUPN, apiConfig.CalendarName, apiConfig.ClientId, apiConfig.TenantID, apiConfig.SecretKey);
	}

	public async Task<List<SimpleAppointment>> Get()
	{
		return await CalChecker().GetTodaysAppointments();
	}

	public async Task<List<SimpleAppointment>> Get(DateTime startDate, DateTime endDate)
	{
		return await CalChecker().GetRangeOfAppointments(startDate, endDate);
	}
}
