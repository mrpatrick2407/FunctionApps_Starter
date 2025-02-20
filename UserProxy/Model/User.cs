using Azure;
using Azure.Data.Tables;
using System.Text.Json.Serialization;

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

}
