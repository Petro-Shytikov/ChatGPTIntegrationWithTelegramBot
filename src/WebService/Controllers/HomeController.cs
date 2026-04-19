using Microsoft.AspNetCore.Mvc;

namespace WebService.Controllers;

[ApiController]
public sealed class HomeController : ControllerBase
{
	[HttpGet("/")]
	public IActionResult Index()
	{
		return Content("Telegram ChatGPT integration service is running.", "text/plain");
	}
}
