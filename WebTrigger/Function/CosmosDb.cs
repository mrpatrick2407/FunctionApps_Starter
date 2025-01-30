using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WebTrigger.Model;
using WebTrigger.Service;

namespace WebTrigger.Function
{
    public class TaskTrigger
    {
        private readonly UserService _userService;
        private readonly TaskService _taskService;
        private readonly NotificationService _notificationService;

        public TaskTrigger(UserService userService, TaskService taskService, NotificationService notificationService)
        {
            _userService = userService;
            _taskService = taskService;
            _notificationService = notificationService;
        }

        [Function("TaskTrigger")]
        public async Task Run(
            [CosmosDBTrigger(
            databaseName: "theodatabase",
            containerName: "task",
            Connection = "CosmosDBConnectionString",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)]
        IReadOnlyList<TaskModel> input,
            FunctionContext context)
        {
            if (input == null || !input.Any()) return;

            var logger = context.GetLogger("TaskTrigger");

            var groupedTasks = input.GroupBy(task => task.userId);

            foreach (var group in groupedTasks)
            {
                string userId = group.Key!;
                var user = await _userService.GetUserByUserId(userId!);
                if (user == null) continue;

                var tasks = await _taskService.GetTasksByUserIdAsync(userId);

                int pendingTasks = tasks.Count(t => t.Status == StatusModel.Pending);
                int updatedTasks = group.Count();

                int priorityTasks = tasks.Count(t => t.Priority?.Equals("High", StringComparison.OrdinalIgnoreCase) == true);

                if (!string.IsNullOrEmpty(user.email))
                {
                    await _notificationService.SendTaskSummaryEmail(pendingTasks, updatedTasks, priorityTasks, user.email!, user.firstName!);
                }
                logger.LogInformation($"Sent Task Summary Email to {user.email}");
            }
        }

    }

}
