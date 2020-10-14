using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UltimateTrivia.Controllers
{
    [Produces(MediaTypeNames.Application.Json)]
    [ApiController]
    [AllowAnonymous] // TODO require auth
    [Route("api/v{version:apiVersion}/[controller]")]
    public abstract class BaseApiController : ControllerBase
    {
    }
}