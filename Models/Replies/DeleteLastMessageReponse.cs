using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Models.Replies
{
    public class DeleteLastMessageReponse : IMessengerResponse
    {
        public async Task<ReplyInfo> SendReplyAsync(IMessengerService messenger, long chatId, ReplyInfo latestReply, int requestId)
        {
            //return messenger.DeleteMessageAsync(latestReply, requestId);
            await messenger.DeleteMessageAsync(latestReply);
            return latestReply;
        }
    }
}
