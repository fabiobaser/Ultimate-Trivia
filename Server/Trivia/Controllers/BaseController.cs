using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Trivia.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public abstract class BaseController : ControllerBase
    {
    }
}