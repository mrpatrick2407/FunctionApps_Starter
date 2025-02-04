using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using WebTrigger.Function.Durable.Activity;
using WebTrigger.Model;
using static WebTrigger.Function.Durable.Activity.TaskActivityTriggers;

namespace WebTrigger.Function.Durable.Orchestrator
{
    public static class TaskManagementOrchestrator
    {
        [Function(nameof(TaskManagementOrchestrator))]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {

            var taskDetails = context.GetInput<DynamicClass>();

            bool isValidSession = await context.CallSubOrchestratorAsync<bool>(nameof(SessionOrchestrator), taskDetails!.sessionId);
            if (!isValidSession)
            {
                throw new InvalidOperationException("Invalid session. Cannot assign task.");
            }
            await context.CallActivityAsync(nameof(TaskActivityTriggers.CreateTaskDurable), taskDetails.task);

            await context.CallActivityAsync(nameof(TaskActivityTriggers.CheckPriorityAndNotify), taskDetails.task);

            if (taskDetails!.task!.Status.Equals(StatusModel.Pending))
            {
                var timer = context.CreateTimer(taskDetails.task.Deadline!.Value.AddMinutes(-15), CancellationToken.None);
                await timer;
                var taskModel = await context.CallActivityAsync<StatusModel>(nameof(TaskActivityTriggers.CheckTaskStatus), taskDetails.task.id);
                if (!taskModel.Equals(StatusModel.Completed))
                {
                    await context.CallActivityAsync(nameof(TaskActivityTriggers.EscalateTask), taskDetails.task);
                }
            }
        }
        [Function(nameof(BulkImportOrchestrator))]
        public static async Task BulkImportOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {

            var csvData = context.GetInput<string>();

            var importData = await context.CallActivityAsync<IEnumerable<TaskModel>>(nameof(TaskActivityTriggers.ReadCSV), csvData);

            if (importData.Count() > 1)
            {
                await context.CallActivityAsync(nameof(TaskActivityTriggers.ScaleRU), new ScaleRUInput() { count=importData.Count(), autoscale =false});

                await context.CallActivityAsync(nameof(TaskActivityTriggers.ImportCSV), importData);
            }
        }
    }

    public class DynamicClass
    {
        public TaskModel ?task { get; set; }
        public string ?sessionId {  get; set; }
    }
}
