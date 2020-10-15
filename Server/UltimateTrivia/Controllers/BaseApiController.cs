using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UltimateTrivia.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [Authorize()]
    [Route("api/v{version:apiVersion}/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
    }
}