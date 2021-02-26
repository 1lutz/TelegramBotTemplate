using System.Threading.Tasks;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public abstract class AbstractMessengerDialog
    {
        public abstract Task<IMessengerResponse> HandleCallbackAsync(Models.User user, string command, string[] args);

        public abstract Task<IMessengerResponse> HandleCommandAsync(Models.User user, string command);

        public abstract Task<IMessengerResponse> HandleMessageAsync(Models.User user, string text);

        protected IMessengerResponse Text(string text, bool silent = false) => new TextMessageResponse(text, null, silent);

        protected IMessengerResponse TextWithKeyboard(string text, Keyboard keyboard, bool silent = false) => new TextMessageResponse(text, keyboard, silent);

        protected IMessengerResponse EditLatestMessage(string newText) => new EditMessageResponse(newText, null);

        protected IMessengerResponse EditLatestMessage(string newText, Keyboard newKeyboard) => new EditMessageResponse(newText, newKeyboard);

        protected IMessengerResponse Nothing() => new EmptyMessageResponse();

        protected IMessengerResponse DeleteLastRequest() => new DeleteLastRequestResponse();

        protected IMessengerResponse Combine(params IMessengerResponse[] replies) => new CombinedMessageResponse(replies);
    }
}
