using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBotTemplate.Models;

namespace TelegramBotTemplate.Services
{
    public interface IMessengerService
    {
        void StartReceiving();

        Task<ServiceInfo> GetInfoAsync();

        Task<ReplyInfo> SendTextMessageAsync(long chatId, string text, Keyboard keyboard = null, bool silent = false);

        Task<ReplyInfo> EditMessageAsync(ReplyInfo reply, string newText, Keyboard newKeyboard);

        Task DeleteMessageAsync(long chatId, int messageId);

        Task AnswerCallbackAsync(string callbackId);

        Task SendSystemNotificationAsync(string text, bool silent = false);

        Task HandleUpdateAsync(Update update);
    }
}
