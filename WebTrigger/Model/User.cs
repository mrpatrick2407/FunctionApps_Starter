using Azure;
using Azure.Data.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebTrigger.Model
{
    public class User : ITableEntity
    {
        public string? Text { get; set; }

        public string? PartitionKey { get => "theo"; set { } }
        private string? _rowKey;
        public string? RowKey
        {
            get;set;
        }
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? profilePicUrl { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
    public class ApplicationInsightResult
    {
        public int? TotalRequests { get; set; }
        public int ?FailedRequests { get; set; }
        public int ?TotalExceptions { get; set; }
    }

    public class UserDTO
    {
        public string? Text { get; set; }

        public string? PartitionKey { get => "theo"; set { } }
        private string? _rowKey;
        public string? RowKey
        {
            get
            {
                if (_rowKey == null)
                {
                    _rowKey = Guid.NewGuid().ToString();
                }
                return _rowKey;
            }
            set { }
        }
        public string? firstName { get; set; }
        public string? lastName { get; set; }
        public string? profilePicUrl { get; set; }
        public string? email { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

    }

    public class Queue
    {
        public string? ProfilePicName { get; set; }
        public string? ProfilePicUrl { get; set; }
    }
    public class Email
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone {get;set;}
        public string? RowKey { get; set; }
        public string? EmailRecipient { get; set; }
        public string? Message { get; set; }
        public string? Subject {  get; set; }
        public string? PhoneMessage { get; set; }
        public string? HtmlMessage { get; set; }
    }
    public class Phone
    {
        public string? PhoneNumber { get; set; }
        public string? PhoneMessage { get; set; }

    }
    public class TaskModel
    {
        public string? id { get; set; }
        public string? userId { get; set; }
        public DateTime? Deadline { get; set; }
        public string? Type { get; set; } = "Task";
        public string? Title { get; set; }
        public string? Description { get; set; }
        [System.Text.Json.Serialization.JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusModel? Status { get; set; } = StatusModel.Pending;  // Using TaskStatus enum
        public string? Priority { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    public class EscalateTask
    {
        public string? id;
        public string ?AssignedTo;
        public string ?Name;
        public DateTime? Deadline;
    }
    public enum StatusModel
    {
        Pending,
        Completed,
        In_Progress,
        InProgress
    }
    public class Device
    {
        public string? Type { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
    }
}
