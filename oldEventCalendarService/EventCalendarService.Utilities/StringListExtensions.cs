using System;

namespace EventCalendarService.Utilities;

public static class StringListExtensions
{
	public static bool Contains(this string source, string toCheck, StringComparison comp)
	{
		return source.IndexOf(toCheck, comp) >= 0;
	}
}
