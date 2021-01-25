using Microsoft.AspNetCore.Mvc;

namespace MicroService_Face_3_0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthCheckController : ControllerBase
    {
        /// <summary>
        /// Health check
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("Index")]
        public IActionResult Index()
        {
            return Ok("Health Check: Survival!");
        }
    }
}