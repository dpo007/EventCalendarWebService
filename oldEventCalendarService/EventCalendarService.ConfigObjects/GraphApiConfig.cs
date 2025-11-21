namespace EventCalendarService.ConfigObjects;

internal class GraphApiConfig
{
	public string ClientId { get; set; }

	public string TenantID { get; set; }

	public string SecretKey { get; set; }

	public string CalendarUserUPN { get; set; }

	public string CalendarName { get; set; }
}
