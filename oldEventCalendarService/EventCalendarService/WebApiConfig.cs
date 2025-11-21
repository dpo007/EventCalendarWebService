using System.Web.Http;

namespace EventCalendarService;

public static class WebApiConfig
{
	public static void Register(HttpConfiguration config)
	{
		config.EnableCors();
		config.MapHttpAttributeRoutes();
		config.Routes.MapHttpRoute("DefaultApi", "api/{controller}/{id}", new
		{
			id = RouteParameter.Optional
		});
	}
}
