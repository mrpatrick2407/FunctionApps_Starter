using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebTrigger.Model;
using WebTrigger.Service;

namespace WebTrigger.Function
{
    public class TaskFunction
    {
        private readonly ILogger<TaskFunction> _logger;
        private readonly TaskService _taskService;
        private readonly SessionService _sessionService;

        public TaskFunction(ILogger<TaskFunction> logger, TaskService taskService, SessionService sessionService)
        {
            _logger = logger;
            _taskService = taskService;
            _sessionService = sessionService;
        }

        private async Task<bool> IsSessionValid(HttpRequest req)
        {
            if (!req.Headers.TryGetValue("SessionId", out var sessionId) || string.IsNullOrEmpty(sessionId))
            {
                return false;
            }
            return await _sessionService.ValidateSessionAsync(sessionId!);
        }

        [Function("CreateTask")]
        public async Task<IActionResult> CreateTaskAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            if (!await IsSessionValid(req))
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<TaskModel>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (task == null || string.IsNullOrEmpty(task.userId))
            {
                return new BadRequestObjectResult("Invalid task data.");
            }

            await _taskService.CreateTaskAsync(task);
            return new OkObjectResult("Task created successfully.");
        }

        [Function("UpdateTaskStatus")]
        public async Task<IActionResult> UpdateTaskStatusAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            if (!await IsSessionValid(req))
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestData = JsonSerializer.Deserialize<UpdateTaskRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (requestData == null || string.IsNullOrEmpty(requestData.Id) || string.IsNullOrEmpty(requestData.Status.ToString()))
            {
                return new BadRequestObjectResult("Invalid update request.");
            }

            var task = await _taskService.GetTaskByIdAsync(requestData.Id);
            if (task == null)
            {
                return new NotFoundObjectResult("Task not found.");
            }

            task.Status = requestData.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskService.UpdateTaskAsync(requestData.Id, task);
            return new OkObjectResult("Task status updated successfully.");
        }
    }

    public class UpdateTaskRequest
    {
        public string Id { get; set; }
        public StatusModel Status { get; set; }
    }

}
