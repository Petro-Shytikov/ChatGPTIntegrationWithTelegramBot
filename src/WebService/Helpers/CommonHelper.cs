namespace WebService.Helpers;

internal static class CommonHelper
{
	public static string ServiceVersion =>
		typeof(CommonHelper).Assembly.GetName().Version?.ToString() ?? "unknown";
}