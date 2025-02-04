using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PartitionKey = Microsoft.Azure.Cosmos.PartitionKey;

namespace WebTrigger.Service
{
    public class CosmosDbService<T>
    {
        private CosmosClient _cosmosclient;
        public Container _container;
        public CosmosDbService(string connectionString,string databaseId,string containerName) {
            _cosmosclient = new CosmosClient(connectionString);
            _container = _cosmosclient.GetContainer(databaseId,containerName);
        }
        public async Task AddItemAsync(T obj,string ParitionKeyValue)
        {
            await _container.CreateItemAsync(obj, new PartitionKey(ParitionKeyValue));
        }
         public async Task BulkInsert(IEnumerable<T> data,string partitionKeyPath)
            {
            var grouped = data.GroupBy(item => GetPartitionKeyValue(item, partitionKeyPath)).Select(group=>new { Key=group.Key,Value=group}).ToDictionary(x=>x.Key,y=>y.Value);
            foreach (var group in grouped)
            {
                var bulkTrans = _container.CreateTransactionalBatch(new(group.Key));
                foreach (var item in group.Value)
                {
                    bulkTrans.CreateItem<T>(item);
                }
                var response=await bulkTrans.ExecuteAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Batch insert failed for partition key: {group.Key}, Status: {response.StatusCode}");
                }
                else
                {
                    Console.WriteLine($"Batch insert passed for partition key: {group.Key}, Status: {response.StatusCode}");
                }
            }
        }
        private string GetPartitionKeyValue(T item, string partitionKeyPath)
        {
            var property = typeof(T).GetProperty(partitionKeyPath);
            return property?.GetValue(item)?.ToString() ?? throw new Exception("Partition key not found");
        }

        public async Task<T?> GetItemAsync(string id)
        {
            var queryDefinition = new QueryDefinition($"Select * from c where c.id='{id}'");
            var iterator = _container.GetItemQueryIterator<T>(queryDefinition);
            var results = new List<T>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results.FirstOrDefault();
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
        public async Task AdjustThroughputAsync(Container container,int recordCount, bool enableAutoscale = false)
        {
            int targetThroughput = CalculateThroughput(recordCount, enableAutoscale);

            var currentThroughput = await container.ReadThroughputAsync();

            if (currentThroughput.HasValue)
            {
                if (enableAutoscale)
                {
                    await container.ReplaceThroughputAsync(ThroughputProperties.CreateAutoscaleThroughput(targetThroughput));
                }
                else if (currentThroughput != targetThroughput)
                {
                    await container.ReplaceThroughputAsync(ThroughputProperties.CreateManualThroughput(targetThroughput));
                }
            }
        }

        private int CalculateThroughput(int count, bool enableAutoscale)
        {
            if (enableAutoscale)
            {
                return Math.Min(50000, count * 10);
            }
            else
            {
                if (count < 1000) return 400; 
                if (count < 5000) return 1000;
                if (count < 10000) return 5000;
                return 10000; 
            }
        }

    }
}
