using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Models.Replies
{
    public class EditMessageResponse : IMessengerResponse
    {
        private readonly string _newText;
        private readonly Keyboard _newKeyboard;

        public EditMessageResponse(string newText, Keyboard newKeyboard)
        {
            _newText = newText;
            _newKeyboard = newKeyboard;
        }

        public Task<ReplyInfo> SendReplyAsync(IMessengerService messenger, long chatId, ReplyInfo latestReply, int requestId)
        {
            return messenger.EditMessageAsync(latestReply, _newText, _newKeyboard);
        }
    }
}
