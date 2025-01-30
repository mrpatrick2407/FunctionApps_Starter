using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

public class AIService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<AIService> _logger;
    private readonly string _aiAppKey;
    private readonly string _aiAppId;
    private readonly string _appInsightsApi;

    public AIService(TelemetryClient telemetryClient, ILogger<AIService> logger, string aiAppKey, string aiAppId, string appInsightsApi)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _aiAppKey = aiAppKey ?? throw new ArgumentNullException(nameof(aiAppKey));
        _aiAppId = aiAppId ?? throw new ArgumentNullException(nameof(aiAppId));
        _appInsightsApi = appInsightsApi ?? throw new ArgumentNullException(nameof(appInsightsApi));
    }

    public async Task QueryMetricsAndTrackAsync(string query, string requestId, string name)
    {
        try
        {
            var rows = await GetMetricsFromApplicationInsightsAsync(query, requestId);

            if (rows == null || rows.Count == 0)
            {
                _logger.LogError($"[Error]: No data found for the query: {query}");
                throw new FormatException("No data returned from the query.");
            }

            await TrackMetricsAsync(rows, requestId, name);
        }
        catch (Exception ex)
        {
            var exceptionTelemetry = new ExceptionTelemetry(ex)
            {
                Context = { Operation = { Id = requestId } }
            };

            exceptionTelemetry.Properties.Add("TestName", name);
            exceptionTelemetry.Properties.Add("TestQuery", query);
            exceptionTelemetry.Properties.Add("TestRequestId", requestId);

            _telemetryClient.TrackException(exceptionTelemetry);
            _logger.LogError($"[Error]: Client Request ID {requestId}: {ex.Message}");
            throw;
        }
        finally
        {
            _telemetryClient.Flush();
        }
    }

    private async Task<JArray> GetMetricsFromApplicationInsightsAsync(string query, string requestId)
    {
        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Add("x-api-key", _aiAppKey);
            httpClient.DefaultRequestHeaders.Add("x-ms-app", "FunctionTemplate");
            httpClient.DefaultRequestHeaders.Add("x-ms-clientrequest-id", requestId);

            string apiPath = $"{_appInsightsApi}/{_aiAppId}/query?clientId={requestId}&timespan=P1D&query={query}";
            using (var httpResponse = await httpClient.GetAsync(apiPath))
            {
                httpResponse.EnsureSuccessStatusCode();

                var resultJson = await httpResponse.Content.ReadAsAsync<JToken>();
                var rows = resultJson.SelectToken("Tables[0].Rows");

                return rows as JArray;
            }
        }
    }

    private async Task TrackMetricsAsync(JArray rows, string requestId, string name)
    {
        foreach (var row in rows)
        {
            double value;
            if (double.TryParse(row[0]?.ToString(), out value))
            {
                string metricName = row[1]?.ToString() ?? "Unknown Metric";

                _logger.LogInformation($"[Verbose]: Metric '{metricName}' result is {value}");

             
                var metricTelemetry = new MetricTelemetry(metricName, value);
                _telemetryClient.TrackMetric(metricTelemetry);
            }
            else
            {
                _logger.LogError($"[Error]: Unable to parse the value for row: {row}");
            }
        }

        _logger.LogInformation($"Metric telemetry for {name} is sent.");
    }
}
