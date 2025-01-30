using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using WebTrigger.Model;
using WebTrigger.Service;

namespace WebTrigger.Function
{
    public class ApplicationInsights
    {
        private readonly ILogger<ApplicationInsights> _logger;
        private readonly AIService _applicationService;
        private readonly NotificationService _notificationService;
        public ApplicationInsights(ILogger<ApplicationInsights> logger, AIService applicationService,NotificationService notificationService)
        {
            _logger = logger;
            _applicationService = applicationService;
            _notificationService=notificationService;
        }

        [Function("PowerBI")]
        public async Task<IActionResult> Power_BI([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string requestId=Guid.NewGuid().ToString();
            _logger.LogInformation($"PowerBI requestID: {requestId}");
            var result = await _applicationService.GetMetricsFromApplicationInsightsAsync("requests\r\n| where timestamp > ago(1d)\r\n| summarize Row = 1, \r\n            TotalRequests = sum(itemCount), \r\n            FailedRequests = sum(toint(success == false)), \r\n            RequestsDuration = iff(isnan(avg(duration)), \"------\", tostring(round(avg(duration), 2)))\r\n| join kind=inner (\r\n    exceptions\r\n    | where timestamp > ago(1d)\r\n    | summarize Row = 1, TotalExceptions = sum(itemCount)\r\n) on Row\r\n| project TotalRequests, FailedRequests, TotalExceptions\r\n", requestId);

            if (result == null)
            {
                return new NotFoundObjectResult("Application Insight summary not found.");
            }

            var totalRequests = result[0][0]?.Value<int>();
            var failedRequests = result[0][1]?.Value<int>();
            var totalExceptions = result[0][2]?.Value<int>();

            var response = new
            {
                TotalRequests = totalRequests,
                FailedRequests = failedRequests,
                TotalExceptions = totalExceptions
            };

            return new JsonResult(response);
        }
        [Function("ScheduledAnalystics")]
        public async Task ScheduledAnalystics([TimerTrigger("*/120 * * * *")] TimerInfo myTimer)
        {
            string requestId = Guid.NewGuid().ToString();
            _logger.LogInformation($"PowerBI requestID for Sending Mail : {requestId}");
            var query = "requests\r\n| where timestamp > ago(1d)\r\n| summarize Row = 1, \r\n            TotalRequests = sum(itemCount), \r\n            FailedRequests = sum(toint(success == false)), \r\n            RequestsDuration = iff(isnan(avg(duration)), \"------\", tostring(round(avg(duration), 2)))\r\n| join kind=inner (\r\n    exceptions\r\n    | where timestamp > ago(1d)\r\n    | summarize Row = 1, TotalExceptions = sum(itemCount)\r\n) on Row\r\n| project TotalRequests, FailedRequests, TotalExceptions\r\n";
            var result = await _applicationService.GetMetricsFromApplicationInsightsAsync(
                query, requestId
            );
            var totalRequests = result![0][0]?.Value<int?>();
            var failedRequests = result[0][1]?.Value<int?>();
            var totalExceptions = result[0][2]?.Value<int?>();
            ApplicationInsightResult emailRequest = new()
            {
                TotalExceptions = totalExceptions,
                TotalRequests = totalRequests,
                FailedRequests= failedRequests,
            };

            await _notificationService.SendApplicationInsightSummaryEmail(emailRequest,"FunctionApp",Environment.GetEnvironmentVariable("ADMIN_EMAIL")!);
            await _applicationService.TrackMetricAsync(new JArray(result[0][1]!), requestId,query,"requestsPerHour");
        }
    }

}
