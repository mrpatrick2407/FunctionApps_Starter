using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;
using WebTrigger.Service;

namespace WebTrigger.Function.Durable.Activity
{
    public static class TaskActivityTriggers
    {
        public static readonly string DBConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString")!;
        public static readonly string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        public static readonly string TaskContainer = "task";
        public static readonly string EscalateContainer = "escalatedtask";
        public static readonly TaskService taskService;
        public static readonly NotificationService notificationService;
        public static readonly UserService userService;
        // Other configurations
        public static readonly string DatabaseId = Environment.GetEnvironmentVariable("DatabaseId")!;
        public static readonly string ContainerName = Environment.GetEnvironmentVariable("TaskContainer")!;
        public static readonly string SendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY")!;
        public static readonly string TwilioAccountSid = Environment.GetEnvironmentVariable("ACCOUNT_SID")!;
        public static readonly string TwilioAuthToken = Environment.GetEnvironmentVariable("AUTH_TOKEN")!;
        static TaskActivityTriggers()
        {
            taskService = new(new CosmosDbService<TaskModel>(DBConnectionString, DatabaseId, TaskContainer),new CosmosDbService<EscalateTask>(DBConnectionString, DatabaseId, EscalateContainer));
            notificationService = new(SendGridApiKey, TwilioAccountSid, TwilioAuthToken);
            userService = new(ConnectionString, "User");
        }
        [Function(nameof(CreateTaskDurable))]
        public static async Task CreateTaskDurable([ActivityTrigger] TaskModel taskDetails)
        {
            Console.WriteLine($"Creating task for user: {taskDetails.userId}");
            await taskService.CreateTaskAsync(taskDetails);

        }
        [Function(nameof(CheckPriorityAndNotify))]
        public static async Task CheckPriorityAndNotify([ActivityTrigger] TaskModel taskDetails)
        {
            if (taskDetails.Priority == "High")
            {
                Console.WriteLine($"High priority task. Sending notification for task: {taskDetails.userId}");
                var user =await userService.GetUserByUserId(taskDetails.userId!);
                if(user !=null && !string.IsNullOrEmpty(user!.email))
                {
                    await notificationService.SendPriorityTaskMail(taskDetails, user.email);
                }               
            }
        }
        [Function(nameof(EscalateTask))]
        public static async Task EscalateTask([ActivityTrigger] TaskModel taskDetails)
        {
            Console.WriteLine($"Escalating task {taskDetails.userId} with task id {taskDetails.id}");
            var user = await userService.GetUserByUserId(taskDetails.userId!);
            if (user != null && !string.IsNullOrEmpty(user!.email))
            {
                await notificationService.SendEscalationMail(taskDetails, user.email);
                EscalateTask escalateTask = new EscalateTask() { AssignedTo=user.email,id=taskDetails.id,Name=user.firstName,Deadline=taskDetails.Deadline};
                await taskService.EscalateTask(escalateTask);
            }
        }
        [Function(nameof(CheckTaskStatus))]
        public static async Task<StatusModel?> CheckTaskStatus([ActivityTrigger] string taskId)
        {
            var task=  await taskService.GetTaskByIdAsync(taskId);
            if(task!=null && task.Status != null)
            {
                return task.Status;
            }
            return StatusModel.Pending;
        }
    }
}
