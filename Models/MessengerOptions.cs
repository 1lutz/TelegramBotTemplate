using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBotTemplate.Models
{
    public class MessengerOptions
    {
        public string ApiToken { get; set; }

        public long OwnerID { get; set; }

        public string WebhookURL { get; set; }
    }
}
