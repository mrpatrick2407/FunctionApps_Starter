using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using WebTrigger.Function.Durable.Activity;

namespace WebTrigger.Function.Durable.Orchestrator
{
    public static class SessionOrchestrator
    {
        [Function(nameof(SessionOrchestrator))]
        public static async Task<bool> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var input = context.GetInput<string>();
            var isSessionActive = await context.CallActivityAsync<bool>(nameof(SessionActivityTriggers.ValidateSession),input);
            return isSessionActive;
        }

    }
}
