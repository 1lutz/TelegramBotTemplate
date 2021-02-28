using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Models.Replies
{
    public class TextMessageResponse : IMessengerResponse
    {
        private readonly string _text;
        private readonly Keyboard _keyboard;
        private readonly bool _silent;

        public TextMessageResponse(string text, Keyboard keyboard, bool silent)
        {
            _text = text;
            _keyboard = keyboard;
            _silent = silent;
        }

        public Task<ReplyInfo> SendReplyAsync(IMessengerService messenger, long chatId, ReplyInfo latestReply, int requestId)
        {
            if (latestReply.HasKeyboard)
            {
                messenger.EditMessageAsync(latestReply, null, null);
            }
            return messenger.SendTextMessageAsync(chatId, _text, _keyboard, _silent);
        }

        public override string ToString()
        {
            return _text;
        }
    }
}
