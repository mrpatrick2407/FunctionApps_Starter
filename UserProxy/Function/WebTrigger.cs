using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using WebTrigger.Model;
using WebTrigger.Service;
namespace WebTrigger.Function
{
    public class WebTrigger
    {
        private readonly ILogger<WebTrigger> _logger;
        private readonly HttpClient _client;
        private UserService _userService;
        private readonly IConfiguration _configuration;
        public WebTrigger(IConfiguration configuration,HttpClient client,UserService userService, ILogger<WebTrigger> logger)
        {
            _client = client;
            _logger = logger;
            _userService = userService;
            _configuration = configuration;
        }

        [Function("GetUserById")]
        public async Task<IActionResult> GetUserById(
    [HttpTrigger(AuthorizationLevel.Function, "get",Route ="getUser/{userId}")] HttpRequestData req,string userId,
    FunctionContext context)
        {
            var user = await _userService.GetUserByUserId(userId);
            return new JsonResult(user);
        }

    }
}
