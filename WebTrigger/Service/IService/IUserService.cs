using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;

namespace WebTrigger.Service.IService
{
    public interface IUserService
    {
        public  Task<bool> UserExists(string email);
        public  Task RegisterUser(User user);
        public  Task<User?> GetUserByUserId(string userID);
        public Task<User?> GetUserByEmail(string email);
    }
}
