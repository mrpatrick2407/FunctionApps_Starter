using Microsoft.Azure.Cosmos;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Service
{
    public class CosmosDbService<T>
    {
        private CosmosClient _cosmosclient;
        private Container _container;
        public CosmosDbService(string connectionString,string databaseId,string containerName) {
            _cosmosclient = new CosmosClient(connectionString);
            _container = _cosmosclient.GetContainer(databaseId,containerName);
        }
        public async Task AddItemAsync(T obj)
        {
            await _container.CreateItemAsync(obj, new PartitionKey(obj?.GetType().GetProperty("userId")?.GetValue(obj, null)?.ToString()));
        }
        public async Task<T> GetItemAsync(string id)
        {
            var val = await _container.ReadItemAsync<T>(id,new(id));
            return val.Resource;
        }
        public async Task<int> GetTotalCount()
        {
            var query = new QueryDefinition("SELECT VALUE COUNT(2) FROM c");
            using (var iterator = _container.GetItemQueryIterator<int>(query))
            {
                if (iterator.HasMoreResults)
                {                
                    var response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault(); 
                }
            }

            return 0;
        }
    }
}
