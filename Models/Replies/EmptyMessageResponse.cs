using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Models.Replies
{
    public class EmptyMessageResponse : IMessengerResponse
    {
        public Task<ReplyInfo> SendReplyAsync(IMessengerService messenger, long chatId, ReplyInfo latestReply, int requestId)
        {
            if (latestReply.HasKeyboard)
            {
                return messenger.EditMessageAsync(latestReply, null, null);
            }
            else
            {
                return Task.FromResult(latestReply);
            }
        }
    }
}
