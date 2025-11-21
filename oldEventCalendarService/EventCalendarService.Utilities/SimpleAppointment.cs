using System;
using System.Runtime.Serialization;

namespace EventCalendarService.Utilities;

[DataContract]
public class SimpleAppointment
{
	[DataMember]
	public string Location { get; set; }

	[DataMember]
	public string HtmlColour { get; set; }

	[DataMember]
	public double End { get; set; }

	[DataMember]
	public double Start { get; set; }

	[DataMember]
	public string Subject { get; set; }

	[DataMember]
	public string Body { get; set; }

	[DataMember]
	public string Id { get; set; }

	[DataMember]
	public bool AllDay { get; set; }

	public SimpleAppointment(DateTime start, DateTime end, string location, string subject, string body, string id, bool isAllDayEvent, string categoryHtmlColour)
	{
		Id = id;
		Start = start.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
		End = end.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
		Subject = subject;
		Body = body;
		AllDay = isAllDayEvent;
		HtmlColour = categoryHtmlColour;
		Location = location;
	}
}
