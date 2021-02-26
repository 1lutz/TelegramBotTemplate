using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBotTemplate.Models
{
    public class ServiceInfo
    {
        public int PendingUpdates { get; set; }

        public DateTime LastErrorDate { get; set; }

        public string LastErrorMessage { get; set; }
    }
}
