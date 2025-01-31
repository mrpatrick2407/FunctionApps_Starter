using WebTrigger.Model;

namespace WebTrigger.Service
{
    public class TaskService 
    {
        private readonly CosmosDbService<TaskModel> _cosmosDbService;
        private readonly CosmosDbService<EscalateTask> _escalateTaskService;

        public TaskService(CosmosDbService<TaskModel> cosmosDbService,CosmosDbService<EscalateTask> escalateTaskService)
        {
            _cosmosDbService = cosmosDbService;
            _escalateTaskService = escalateTaskService;
        }

        public async Task CreateTaskAsync(TaskModel task)
        {
            task.UpdatedAt = task.UpdatedAt ?? DateTime.Now;
            task.id = string.IsNullOrEmpty(task.id) ? Guid.NewGuid().ToString() : task.id;
            await _cosmosDbService.AddItemAsync(task, task.userId!);
        }

        public async Task UpdateTaskAsync(string id, TaskModel task)
        {
            await _cosmosDbService.UpdateItemAsync(id, task, task.userId!);
        }

        public async Task<TaskModel?> GetTaskByIdAsync(string id)
        {
            return await _cosmosDbService.GetItemAsync(id);
        }
        public async Task EscalateTask(EscalateTask escalateTask)
        {
            await _escalateTaskService.AddItemAsync(escalateTask,escalateTask.id!);
        }
        public async Task<IEnumerable<TaskModel>> GetTasksByUserIdAsync(string userId)
        {
            string query = $"SELECT * FROM c WHERE c.userId = '{userId}'";
            return await _cosmosDbService.GetItemsByQueryAsync(query);
        }
    }

}
