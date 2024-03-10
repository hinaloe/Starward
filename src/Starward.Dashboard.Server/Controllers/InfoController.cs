using Microsoft.AspNetCore.Mvc;

namespace Starward.Dashboard.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InfoController : ControllerBase
{

    private readonly ILogger<InfoController> _logger;

    public InfoController(ILogger<InfoController> logger)
    {
        _logger = logger;
    }


    [HttpGet]
    public object Get()
    {
        return new { Program.AppVersion, Program.DatabaseVersion };
    }



}
