using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotTemplate.Models;
using TelegramBotTemplate.Models.Replies;
using TelegramBotTemplate.Services;

namespace TelegramBotTemplate.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BotController : ControllerBase
    {
        private readonly IMessengerService _messenger;
        private readonly IUserRegistry _userRegistry;

        public BotController(IMessengerService messenger, IUserRegistry userRegistry)
        {
            _messenger = messenger;
            _userRegistry = userRegistry;
        }

        // GET: api/bot
        [HttpGet]
        public Task<ServiceInfo> Info()
        {
            return _messenger.GetInfoAsync();
        }

        // POST api/bot
        [HttpPost]
        public async Task<IActionResult> HandleUpdateAsync(Update update)
        {
            try
            {
                await _messenger.HandleUpdateAsync(update);
                await _userRegistry.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await _messenger.SendSystemNotificationAsync(ex.ToString());

                if (update.Type == UpdateType.Message)
                {
                    await _messenger.SendTextMessageAsync(update.Message.Chat.Id, "Ein interner Fehler ist aufgetreten. Bitte versuche es später erneut.");
                }
            }
            return Ok();
        }
    }
}
