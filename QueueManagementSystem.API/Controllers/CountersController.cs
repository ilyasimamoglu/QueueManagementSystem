using Microsoft.AspNetCore.Mvc;
using QueueManagementSystem.Business;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountersController : ControllerBase
    {
        private readonly ICounterService _counterService;

        public CountersController(ICounterService counterService)
        {
            _counterService = counterService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var result = _counterService.GetAll();
            return Ok(result);
        }

        [HttpPost]
        public IActionResult Post(Counter counter)
        {
            _counterService.Add(counter);
            return Ok("Gişe başarıyla eklendi.");
        }
    }
}