using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;

namespace WebTrigger.Service.IService
{
    public interface IMessageService
    {
       public Task SendMessage(Phone phone);
    }
}
