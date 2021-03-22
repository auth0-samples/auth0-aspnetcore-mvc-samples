using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SampleWebApiApp.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ValuesController : ControllerBase
    {
        [Authorize("Scope2")]
        [HttpGet]
        public string Get()
        {
            return "Hello World";
        }
    }
}
