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
        public async Task CreateSessionAsync(string rowID)
        {
            Session session = new();
            session.id =Convert.ToString(await _cosmosDbService.GetTotalCount()+1);
            session.userId = rowID;
            session.status = "Active";
            session.expiresAt=DateTime.Now.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ");
            await _cosmosDbService.AddItemAsync(session);
        }

        public async Task<bool> ValidateSessionAsync(string userId)
        {
            var session = await _cosmosDbService.GetItemAsync(userId);
            if (session == null || DateTime.Parse(session.expiresAt!) < DateTime.UtcNow)
                return false;
            return true;
        }
    }
}
