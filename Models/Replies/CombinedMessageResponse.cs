using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Models.Replies
{
    public class CombinedMessageResponse : IMessengerResponse
    {
        private readonly IMessengerResponse[] _responses;

        public CombinedMessageResponse(IMessengerResponse[] responses)
        {
            _responses = responses;
        }

        public async Task<ReplyInfo> SendReplyAsync(IMessengerService messenger, long chatId, ReplyInfo latestReply, int requestId)
        {
            foreach (IMessengerResponse response in _responses)
            {
                latestReply = await response.SendReplyAsync(messenger, chatId, latestReply, requestId);
            }
            return latestReply;
        }
    }
}
