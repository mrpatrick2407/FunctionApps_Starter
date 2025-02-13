using WebTrigger.Model;

namespace WebTrigger.Service
{
    public class TaskService 
    {
        public readonly CosmosDbService<TaskModel> _taskCosmosService;
        public readonly CosmosDbService<EscalateTask> _escalateTaskService;

        public TaskService(CosmosDbService<TaskModel> cosmosDbService,CosmosDbService<EscalateTask> escalateTaskService)
        {
            _taskCosmosService = cosmosDbService;
            _escalateTaskService = escalateTaskService;
        }

        public async Task CreateTaskAsync(TaskModel task)
        {
            task.UpdatedAt = task.UpdatedAt ?? DateTime.Now;
            task.id = string.IsNullOrEmpty(task.id) ? Guid.NewGuid().ToString() : task.id;
            await _taskCosmosService.AddItemAsync(task, task.userId!);
        }

        public async Task UpdateTaskAsync(string id, TaskModel task)
        {
            await _taskCosmosService.UpdateItemAsync(id, task, task.userId!);
        }

        public async Task<TaskModel?> GetTaskByIdAsync(string id)
        {
            return await _taskCosmosService.GetItemAsync(id);
        }
        public async Task EscalateTask(EscalateTask escalateTask)
        {
            await _escalateTaskService.AddItemAsync(escalateTask,escalateTask.id!);
        }
        public async Task<IEnumerable<TaskModel>> GetTasksByUserIdAsync(string userId)
        {
            string query = $"SELECT * FROM c WHERE c.userId = '{userId}'";
            return await _taskCosmosService.GetItemsByQueryAsync(query);
        }

        public async Task<IEnumerable<TaskModel>> GetTasks()
        {
            string query = $"SELECT * FROM c";
            return await _taskCosmosService.GetItemsByQueryAsync(query);
        }
    }

}
