using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Models.Replies
{
    public interface IMessengerResponse
    {
        Task<ReplyInfo> SendReplyAsync(
            IMessengerService messenger,
            long chatId,
            ReplyInfo latestReply,
            int requestId
        );
    }
}
