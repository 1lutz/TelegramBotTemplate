using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public class TelegramService : IMessengerService
    {
        private readonly MessengerOptions _options;
        private readonly TelegramBotClient _bot;
        private readonly IUserRegistry _userRegistry;
        private readonly AbstractDialog _dialog;

        public TelegramService(IOptions<MessengerOptions> options, IUserRegistry userRegistry, AbstractDialog dialog)
        {
            _options = options.Value;
            _bot = new TelegramBotClient(_options.ApiToken);
            _userRegistry = userRegistry;
            _dialog = dialog;
        }

        public void StartReceiving()
        {
            UpdateType[] allowedUpdates = new UpdateType[] {
                UpdateType.Message, UpdateType.CallbackQuery
            };

            if (string.IsNullOrEmpty(_options.WebhookURL))
            {
#pragma warning disable CS4014
                _bot.OnUpdate += (_, e) =>
                {
                    HandleUpdateAsync(e.Update).ContinueWith(_ => _userRegistry.SaveChangesAsync(), TaskContinuationOptions.OnlyOnRanToCompletion);
                };
                _bot.StartReceiving(allowedUpdates);
#pragma warning restore CS4014
            }
            else
            {
                _bot.SetWebhookAsync(_options.WebhookURL, allowedUpdates: allowedUpdates).Wait();
            }
        }

        public async Task<ServiceInfo> GetInfoAsync()
        {
            if (string.IsNullOrEmpty(_options.WebhookURL))
            {
                return null;
            }
            WebhookInfo info = await _bot.GetWebhookInfoAsync();
            return new ServiceInfo()
            {
                PendingUpdates = info.PendingUpdateCount,
                LastErrorDate = info.LastErrorDate.ToLocalTime(),
                LastErrorMessage = info.LastErrorMessage
            };
        }

        public async Task<ReplyInfo> SendTextMessageAsync(long chatId, string text, Keyboard keyboard = null, bool silent = false)
        {
            Message m = await _bot.SendTextMessageAsync(
                chatId,
                text,
                ParseMode.Markdown,
                replyMarkup: keyboard?.Build(),
                disableNotification: silent
            );
            return new ReplyInfo(chatId, m.MessageId, keyboard != null);
        }

        public async Task<ReplyInfo> EditMessageAsync(ReplyInfo reply, string newText, Keyboard newKeyboard)
        {
            long chatId = reply.UserID;
            int mid = reply.MessageID;
            Message m;

            if (newText == null)
                m = await _bot.EditMessageReplyMarkupAsync(chatId, mid, newKeyboard?.Build());
            else
                m = await _bot.EditMessageTextAsync(chatId, mid, newText, replyMarkup: newKeyboard?.Build());

            return new ReplyInfo(chatId, m.MessageId, newKeyboard != null);
        }

        public Task DeleteMessageAsync(long chatId, int messageId)
        {
            return _bot.DeleteMessageAsync(chatId, messageId);
        }

        public Task AnswerCallbackAsync(string callbackId)
        {
            return _bot.AnswerCallbackQueryAsync(callbackId);
        }

        public Task SendSystemNotificationAsync(string text, bool silent = false)
        {
            return _bot.SendTextMessageAsync(_options.OwnerID, "System message:\r\n" + text, disableNotification: silent);
        }

        private async Task HandleMessageAsync(Message message)
        {
            long chatId = message.Chat.Id;
            string text = message.Text;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            Models.User user = await _userRegistry.GetUserByChatIdAsync(chatId, message.From.FirstName);
            IMessengerResponse response;

            if (text[0] == '/')
            {
                user.LastReply.HasKeyboard = false;
                int pos = text.IndexOf('@');

                if (pos == -1)
                    text = text.Substring(1);
                else
                    text = text.Substring(1, pos - 1);

                response = await _dialog.HandleCommandAsync(user, text);
            }
            else
            {
                response = await _dialog.HandleMessageAsync(user, text);
            }

            user.LastReply = await response.SendReplyAsync(this, chatId, user.LastReply, message.MessageId);
        }

        private async Task HandleCallbackAsync(CallbackQuery callback)
        {
            long chatId = callback.Message.Chat.Id;
            Models.User user = await _userRegistry.GetUserByChatIdAsync(chatId, callback.From.FirstName);
            user.LastReply.HasKeyboard = callback.Message.ReplyMarkup != null;
            user.LastReply.MessageID = callback.Message.MessageId;
            string command = callback.Data;
            string[] args = null;
            int pos = command.IndexOf(';');

            if (pos != -1)
            {
                command = command.Substring(0, pos);
                args = command.Substring(pos + 1).Split(';');
            }
            IMessengerResponse response;

            try
            {
                response = await _dialog.HandleCallbackAsync(user, command, args);
            }
            finally
            {
                try
                {
                    await AnswerCallbackAsync(callback.Id);
                }
                catch (Telegram.Bot.Exceptions.InvalidParameterException) { }
            }
            user.LastReply = await response.SendReplyAsync(this, chatId, user.LastReply, callback.Message.MessageId);
        }

        public async Task HandleUpdateAsync(Update update)
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                    await HandleMessageAsync(update.Message);
                    break;

                case UpdateType.CallbackQuery:
                    await HandleCallbackAsync(update.CallbackQuery);
                    break;
            }
        }
    }
}
