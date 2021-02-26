using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBotTemplate.Models
{
    public class ReplyInfo
    {
        public long UserID { get; set; }

        public int MessageID { get; set; }

        public bool HasKeyboard { get; set; }

        public ReplyInfo() { }

        public ReplyInfo(long userId, int messageId, bool hasKeyboard)
        {
            UserID = userId;
            MessageID = messageId;
            HasKeyboard = hasKeyboard;
        }
    }
}
