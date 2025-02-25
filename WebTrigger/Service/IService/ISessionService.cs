using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Service.IService
{
    public interface ISessionService
    {
        public  Task<string> CreateSessionAsync(string rowID);
        public  Task<bool> ValidateSessionAsync(string sessionId);
        public  Task<bool> GetActiveSessionAsync(string userId);
    }
}
