using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Trivial.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public abstract class BaseController : ControllerBase
    {
    }
}