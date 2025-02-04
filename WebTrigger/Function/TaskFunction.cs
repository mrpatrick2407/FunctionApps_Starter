using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using WebTrigger.Function.Durable.Orchestrator;
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
            if (!req.Headers.TryGetValue("sessionId", out var sessionId) || string.IsNullOrEmpty(sessionId))
            {
                return false;
            }
            bool res= await _sessionService.ValidateSessionAsync(sessionId!);
            return res;
        }
        private string? GetSessionId(HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("sessionId", out var sessionId) || !sessionId.Any())
            {
                return string.Empty;
            }
            else
            {
                return sessionId.FirstOrDefault()!;
            }
        }
        [Function("CreateTask")]
        public async Task<HttpResponseData> CreateTaskAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, [DurableClient] DurableTaskClient orchestrationClient,
    FunctionContext executionContext)
        {
            //if (!await IsSessionValid(req))
            //{
            //    return new UnauthorizedResult();
            //}

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var task = JsonSerializer.Deserialize<TaskModel>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            var response = req.CreateResponse();
            if (task == null || string.IsNullOrEmpty(task.userId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                return response;
            }

            //await _taskService.CreateTaskAsync(task);
            //return new OkObjectResult("Task created successfully.");
            task.UpdatedAt = task.UpdatedAt ?? DateTime.Now;
            task.id = string.IsNullOrEmpty(task.id) ? Guid.NewGuid().ToString() : task.id;
            string instanceId = await orchestrationClient.ScheduleNewOrchestrationInstanceAsync(
nameof(TaskManagementOrchestrator),
new DynamicClass() { task = task, sessionId = GetSessionId(req)! });
            return await orchestrationClient.CreateCheckStatusResponseAsync(req,instanceId);
        }

        [Function("UpdateTask")]
        public async Task<IActionResult> UpdateTaskAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            if (!await IsSessionValid(req))
            {
                return new UnauthorizedResult();
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestData = JsonSerializer.Deserialize<TaskModel>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (requestData == null || string.IsNullOrEmpty(requestData.id) || string.IsNullOrEmpty(requestData.Status.ToString()))
            {
                return new BadRequestObjectResult("Invalid update request.");
            }

            var task = await _taskService.GetTaskByIdAsync(requestData.id);
            if (task == null)
            {
                return new NotFoundObjectResult("Task not found.");
            }

            task.Status = requestData.Status;
            task.UpdatedAt = DateTime.UtcNow;

            await _taskService.UpdateTaskAsync(requestData.id, task);
            return new OkObjectResult("Task status updated successfully.");
        }
        [Function("GetTasksByUserId")]
        public async Task<IActionResult> GetTasksByUserIdAsync(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "tasks/{userId}")] HttpRequest req, string userId)
        {
            if (!await IsSessionValid(req))
            {
                return new UnauthorizedResult();
            }

            if (string.IsNullOrEmpty(userId))
            {
                return new BadRequestObjectResult("User ID is required.");
            }

            var tasks = await _taskService.GetTasksByUserIdAsync(userId);

            if (tasks == null || !tasks.Any())
            {
                return new NotFoundObjectResult("No tasks found for this user.");
            }

            return new OkObjectResult(tasks);
        }
        [Function("BulkImport")]
        public async Task BulkImport(
    [BlobTrigger("bulk")] Stream blob, [DurableClient]DurableTaskClient client, FunctionContext context)
        {
           var csvData=new StreamReader(blob).ReadToEnd();
            string instanceID = await client.ScheduleNewOrchestrationInstanceAsync(nameof(TaskManagementOrchestrator.BulkImportOrchestrator),csvData);
            Console.WriteLine($"Bulk import started of instance id {instanceID}");
        }
    }

}
