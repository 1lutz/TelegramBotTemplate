﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;

namespace TelegramBotTemplate.Services
{
    public class HelloWorldDialog : AbstractDispatchingDialog
    {
        public HelloWorldDialog(ILogger<AbstractDispatchingDialog> logger) : base(logger)
        {
        }

        public IMessengerResponse Start(User user)
        {
            return Text($"Hello {user.Name}!");
        }

        public override Task<IMessengerResponse> HandleMessageAsync(User user, string text)
        {
            return Task.FromResult(Text("You said:\r\n" + text));
        }
    }
}
