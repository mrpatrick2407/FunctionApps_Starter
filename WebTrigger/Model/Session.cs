using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Model
{
    public class Session
    {
        private string? _id;
        public string? id { get {
                if (_id == null)
                { 
                    _id = Guid.NewGuid().ToString();
                } return _id;
            } set { } }
        public string? userId {  get; set; }
        public string? status { get; set; }
        public string? _expiresAt;
        public string? expiresAt
        {
            get
            {
                if (_expiresAt == null)
                {
                    _expiresAt = DateTime.Now.AddHours(1).ToShortDateString();
                }
                return _expiresAt;
            }
            set { }
        }

    }
}
