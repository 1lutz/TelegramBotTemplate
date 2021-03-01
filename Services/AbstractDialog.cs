using System.Threading.Tasks;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public abstract class AbstractDialog
    {
        public abstract Task<IMessengerResponse> HandleCallbackAsync(Models.User user, string command, string[] args);

        public abstract Task<IMessengerResponse> HandleCommandAsync(Models.User user, string command, string[] args);

        public abstract Task<IMessengerResponse> HandleMessageAsync(Models.User user, string text);

        protected static IMessengerResponse Text(string text, bool silent = false) => new TextMessageResponse(text, null, silent);

        protected static IMessengerResponse TextWithKeyboard(string text, Keyboard keyboard, bool silent = false) => new TextMessageResponse(text, keyboard, silent);

        protected static IMessengerResponse EditLatestMessage(string newText) => new EditMessageResponse(newText, null);

        protected static IMessengerResponse EditLatestMessage(string newText, Keyboard newKeyboard) => new EditMessageResponse(newText, newKeyboard);

        protected static IMessengerResponse Nothing() => new EmptyMessageResponse();

        protected static IMessengerResponse DeleteLastRequest() => new DeleteLastRequestResponse();

        protected static IMessengerResponse Combine(params IMessengerResponse[] replies) => new CombinedMessageResponse(replies);
    }
}
