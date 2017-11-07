using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Message = "Ok" });
        }

        // GET api/values/Sleep?ms=10
        [HttpGet("[action]")]
        public async Task<IActionResult> Sleep([FromQuery]int ms)
        {
            var millisecondsDelay = Math.Max(1, Math.Abs(ms));
            await Task.Delay(millisecondsDelay);
            return Ok(new { Message = $"Waited for {millisecondsDelay}ms." });
        }

        // GET api/values/GetStatusCode?statuscode=404
        [HttpGet("[action]")]
        public IActionResult GetStatusCode([FromQuery]int statusCode)
        {
            return StatusCode(statusCode);
        }

        private static int _gatewayTimeoutServiceUnavailableOkCount; 

        // GET api/values/GatewayTimeoutServiceUnavailableOk
        [HttpGet("[action]")]
        public IActionResult GatewayTimeoutServiceUnavailableOk()
        {
            _gatewayTimeoutServiceUnavailableOkCount++;

            if (_gatewayTimeoutServiceUnavailableOkCount == 1)
                return StatusCode((int)HttpStatusCode.GatewayTimeout);

            if (_gatewayTimeoutServiceUnavailableOkCount == 2)
                return StatusCode((int)HttpStatusCode.ServiceUnavailable);

            _gatewayTimeoutServiceUnavailableOkCount = 0;
            return StatusCode((int)HttpStatusCode.OK);
        }
    }
}
