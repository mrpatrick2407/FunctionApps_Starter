using WebTrigger.Model;

namespace WebTrigger.Service
{
    public class TaskService 
    {
        private readonly CosmosDbService<TaskModel> _cosmosDbService;

        public TaskService(CosmosDbService<TaskModel> cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
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

        public async Task<TaskModel> GetTaskByIdAsync(string id)
        {
            return await _cosmosDbService.GetItemAsync(id);
        }

        public async Task<IEnumerable<TaskModel>> GetTasksByUserIdAsync(string userId)
        {
            string query = $"SELECT * FROM c WHERE c.UserId = '{userId}'";
            return await _cosmosDbService.GetItemsByQueryAsync(query);
        }
    }

}
