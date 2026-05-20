using Microsoft.AspNetCore.Mvc;

namespace RureSubPostWriter.Controllers;

public class StatusController : Controller
{
    public IActionResult Index()
    {
        return Ok("Service PostWriter is working!");
    }
}
