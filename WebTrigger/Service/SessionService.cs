using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;

namespace WebTrigger.Service
{
    public class SessionService
    {
        private readonly CosmosDbService<Session> _cosmosDbService;

        public SessionService(CosmosDbService<Session> cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }
        public async Task<string> CreateSessionAsync(string rowID)
        {
            Session session = new();
            if (string.IsNullOrEmpty(session.id)) session.id = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(session.userId)) session.userId = rowID;
            if (string.IsNullOrEmpty(session.status)) session.status = "Active";
            if (string.IsNullOrEmpty(session.expiresAt)) session.expiresAt = DateTime.Now.AddHours(1).ToString("o");
            await _cosmosDbService.AddItemAsync(session,session.userId);
            return session.id;
        }

        public async Task<bool> ValidateSessionAsync(string sessionId)
        {
            var query = $"SELECT * FROM c WHERE c.id = '{sessionId}'";
            var enumerable = await _cosmosDbService.GetItemsByQueryAsync(query);
            return enumerable.Any(row =>
            {
                var expiresUtc = DateTime.Parse(row.expiresAt!).ToUniversalTime(); 
                var serverTimeZone = TimeZoneInfo.Local; 
                var expiresLocal = TimeZoneInfo.ConvertTimeFromUtc(expiresUtc, serverTimeZone); 
                var result = expiresLocal > DateTime.Now;
                return result;
            });        
        }

        public async Task<bool> GetActiveSessionAsync(string userId)
        {
            var query = $"SELECT * FROM c WHERE c.userId = '{userId}'";
            var enumerbale=await _cosmosDbService.GetItemsByQueryAsync(query);
            return enumerbale.Any(ro =>DateTime.Parse(ro.expiresAt!) > DateTime.Now);
        }
    }
}
