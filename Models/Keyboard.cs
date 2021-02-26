using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramBotTemplate.Models
{
    public class Keyboard
    {
        private List<InlineKeyboardButton> _row;
        private readonly List<List<InlineKeyboardButton>> _grid;

        private Keyboard()
        {
            _row = new List<InlineKeyboardButton>();
            _grid = new List<List<InlineKeyboardButton>>();
        }

        public static Keyboard Create() => new Keyboard();

        public static Keyboard Create(params string[] buttons)
        {
            Keyboard tastatur = new Keyboard();
            foreach (string button in buttons) tastatur.Append(button);
            return tastatur;
        }

        public Keyboard Append(string text, string callback)
        {
            _row.Add(InlineKeyboardButton.WithCallbackData(text, callback));
            return this;
        }

        public Keyboard Append(string text) => Append(text, text);

        public Keyboard AppendLine()
        {
            _grid.Add(_row);
            _row = new List<InlineKeyboardButton>();
            return this;
        }

        public Keyboard AppendLine(string text, string callback) => Append(text, callback).AppendLine();

        public Keyboard AppendLine(string text) => AppendLine(text, text);

        public InlineKeyboardMarkup Build()
        {
            _grid.Add(_row);
            return new InlineKeyboardMarkup(_grid);
        }
    }
}
