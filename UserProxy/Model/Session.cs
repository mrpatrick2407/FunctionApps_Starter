using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Model
{
    public class Session
    {
        public string? id { get; set; }
        public string? userId {  get; set; }
        public string? status { get; set; }
        public string? expiresAt;

    }
}
