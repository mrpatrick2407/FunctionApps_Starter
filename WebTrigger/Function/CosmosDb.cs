using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using WebTrigger.Model;

namespace WebTrigger.Function
{
    public class CosmosDb
    {
        private readonly ILogger _logger;

        public CosmosDb(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CosmosDb>();
        }

        [Function("CosmosDbTrigger")]
        public void Run([CosmosDBTrigger(
            databaseName: "theodatabase",
            containerName: "theo",
            Connection = "CosmosDBConnectionString",
            LeaseContainerName = "leases",
            CreateLeaseContainerIfNotExists = true)] IReadOnlyList<User> input)
        {
            if (input != null && input.Count > 0)
            {
                _logger.LogInformation("Documents modified: " + input.Count);
                _logger.LogInformation("First document Id: " + input[0]);
            }
        }
    }

}
