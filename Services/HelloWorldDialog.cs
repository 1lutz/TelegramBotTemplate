using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public class HelloWorldDialog : AbstractMessengerDialog
    {
        public override Task<IMessengerResponse> HandleCommandAsync(User user, string command)
        {
            switch (command)
            {
                case "start":
                    return Task.FromResult(Text($"Hello {user.Name}!"));

                case "help":
                    return Task.FromResult(Text(@"These are all available commands:
/help - Shows this page"));

                default:
                    return Task.FromResult(Text("Unrecognized command. You can use /help to show all available commands."));
            }
        }

        public override Task<IMessengerResponse> HandleMessageAsync(User user, string text)
        {
            return Task.FromResult(Text("You said:\r\n" + text));
        }

        public override Task<IMessengerResponse> HandleCallbackAsync(User user, string command, string[] args)
        {
            return Task.FromResult(Nothing());
        }
    }
}
