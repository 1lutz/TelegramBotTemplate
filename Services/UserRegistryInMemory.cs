using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramBotTemplate.Models;

namespace TelegramBotTemplate.Services
{
    public class UserRegistryInMemory : IUserRegistry
    {
        private readonly ConcurrentDictionary<long, User> _allUsers = new ConcurrentDictionary<long, User>();

        private User CreateNewUser(long chatId, string name)
        {
            return new User()
            {
                DialogState = DialogState.New,
                UserID = chatId,
                Name = name,
                LastReply = new ReplyInfo(chatId, -1, false)
            };
        }

        public Task<User> GetUserByChatIdAsync(long chatId, string name)
        {
            User user = _allUsers.GetOrAdd(chatId, CreateNewUser, name);
            return Task.FromResult(user);
        }

        public Task<IEnumerable<User>> GetAllUsersAsync()
        {
            IEnumerable<User> users = _allUsers.Values.ToList();
            return Task.FromResult(users);
        }

        public Task SaveChangesAsync()
        {
            return Task.CompletedTask;
        }
    }
}
