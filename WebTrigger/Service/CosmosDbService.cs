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
        public async Task AddItemAsync(T obj,string ParitionKeyValue)
        {
            await _container.CreateItemAsync(obj, new PartitionKey());
        }
        public async Task<T> GetItemAsync(string id)
        {
            var val = await _container.ReadItemAsync<T>(id,new(id));
            return val.Resource;
        }
        public async Task<IEnumerable<T>> GetItemsByQueryAsync(string query)
        {
            var queryDefinition = new QueryDefinition(query);
            var iterator = _container.GetItemQueryIterator<T>(queryDefinition);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
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
        public async Task DeleteItemAsync(string id, string partitionKey)
        {
            await _container.DeleteItemAsync<T>(id, new PartitionKey(partitionKey));
        }

        public async Task UpdateItemAsync(string id, T obj, string partitionKey)
        {
            await _container.UpsertItemAsync(obj, new PartitionKey(partitionKey));
        }
    }
}
