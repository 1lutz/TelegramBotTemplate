using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Models;

namespace TelegramBotTemplate.Services
{
    public interface IUserRegistry
    {
        Task<IEnumerable<User>> GetAllUsersAsync();

        Task<User> GetUserByChatIdAsync(long chatId, string name);

        Task SaveChangesAsync();
    }
}
