﻿using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;
using WebTrigger.Service.IService;

namespace WebTrigger.Service
{
    public class UserService:IUserService
    {
        private readonly TableClient _tableClient;
        public UserService(string connectionString,string tableName)
        {
            _tableClient = new TableClient(connectionString, tableName);
        }
        public async Task<bool> UserExists(string email) {
            var user =await _tableClient.QueryAsync<User>(user=>user.email!.Equals(email)).ToListAsync();
            return user.Any();
        }
        public async Task RegisterUser(User user)
        {
            user.RowKey = string.IsNullOrEmpty(user.RowKey) ?Guid.NewGuid().ToString():user.RowKey;
            await _tableClient.AddEntityAsync<User>(user);
        }
        public async Task<User?> GetUserByUserId(string userID)
        {
            var user = await _tableClient.QueryAsync<User>(user => user.RowKey!.Equals(userID)).ToListAsync();
            return user.FirstOrDefault();
        }

        public async Task<User?> GetUserByEmail(string email)
        {
            var user = await _tableClient.QueryAsync<User>(user => user.email!.Equals(email)).ToListAsync();
            return user.FirstOrDefault();
        }
    }
}
