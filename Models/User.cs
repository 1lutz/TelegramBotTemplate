using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBotTemplate.Models
{
    public class User
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long UserID { get; set; }

        public DialogState DialogState { get; set; }

        public string Name { get; set; }

        public ReplyInfo LastReply { get; set; }
    }
}
