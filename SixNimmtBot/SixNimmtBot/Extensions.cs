using Database;
using SixNimmtBot.Models.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace SixNimmtBot
{

    public static class ExtensionMethods
    {
        // Random List members
        public static List<T> Shuffle<T>(this List<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        public static List<T> Shuffle<T>(this List<T> list, int numberOfTimes)
        {
            for (int i = 0; i < numberOfTimes; i++)
                list.Shuffle();
            return list;
        }

        public static T Random<T>(this IEnumerable<T> list)
        {
            return list.ElementAtOrDefault(Helpers.RandomNum(list.Count()));
        }

        public static T Pop<T>(this List<T> list)
        {
            T r = list.First();
            list.RemoveAt(0);
            return r;
        }


        public static void LogError(this Exception e, long? chatId = null, long? userId = null, bool noStackTrace = false)
        {
            string m = "Error occured." + Environment.NewLine;
            if (chatId != null)
                m += $"ChatId: {chatId}" + Environment.NewLine + Environment.NewLine;
            if (userId != null)
                m += $"UserId: {userId}" + Environment.NewLine + Environment.NewLine;
            var trace = e.StackTrace;

            if (e.Message.ToLower().Contains("request timed out")) return;

            do
            {
                m += e.Message + Environment.NewLine + Environment.NewLine;
                e = e.InnerException;
            }
            while (e != null);

            if (!noStackTrace)
                m += trace;

            Bot.Send(Constants.LogGroupId, m, parseMode: ParseMode.Default);
            Bot.Send(Constants.LogGroupId, e.ToString(), parseMode: ParseMode.Default);
        }

        // Player Extensions
        public static string GetName(this SNCard c)
        {
            return $"{c.Number} [{c.Bulls} 🐮]";
        }

        public static string GetName(this SNPlayer player)
        {
            var name = player.Name;
            if (!String.IsNullOrEmpty(player.Username))
                return $"<a href=\"https://telegram.me/{player.Username}\">{name.FormatHTML()}</a>";
            return name.ToBold();
        }

        public static string GetName(this User player)
        {
            var name = player.FirstName;
            if (!String.IsNullOrEmpty(player.Username))
                return $"<a href=\"https://telegram.me/{player.Username}\">{name.FormatHTML()}</a>";
            return name.ToBold();
        }

        public static string GetMention(this SNPlayer player)
        {
            var name = player.Name;
            return !player.Virtual ? $"<a href=\"tg://user?id={player.TelegramId}\">{name.FormatHTML()}</a>" : name.FormatHTML();
        }

        public static string GetMention(this User player)
        {
            var name = player.FirstName;
            return $"<a href=\"tg://user?id={player.Id}\">{name.FormatHTML()}</a>";
        }

        public static void GenerateCardsInHand(this SNPlayer p, SNDeck deck)
        {
            for (int i = 0; i < 9; i++)
            {
                p.CardsInHand.Add(deck.Pop());
                p.CardsInHand.Last().PlayedBy = p;
            }
        }

        // Bot Extensions
        public static string ToBold(this object str)
        {
            if (str == null)
                return null;
            return $"<b>{str.ToString().FormatHTML()}</b>";
        }

        public static string ToItalic(this object str)
        {
            if (str == null)
                return null;
            return $"<i>{str.ToString().FormatHTML()}</i>";
        }

        public static string ToCode(this object str)
        {
            if (str == null)
                return null;
            return $"<code>{str.ToString().FormatHTML()}</code>";
        }

        public static string FormatHTML(this string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        // Deck Extensions
        public static void InitiateDeck(this SNDeck deck)
        {
            for (int i = 1; i <= 104; i++)
            {
                var card = new SNCard(i);
                deck.Cards.Add(card);
            }
            deck.Cards.Shuffle(5);
        }

        public static SNCard Pop(this SNDeck deck)
        {
            return deck.Cards.Pop();
        }

        public static string ToEmoji(this int num)
        {
            var x = "";
            switch (num)
            {
                case 1:
                    x = "🥇";
                    break;
                case 2:
                    x = "🥈";
                    break;
                case 3:
                    x = "🥉";
                    break;
                case 4:
                    x = "4⃣";
                    break;
                case 5:
                    x = "5⃣";
                    break;
                case 6:
                    x = "6️⃣";
                    break;
                case 7:
                    x = "7️⃣";
                    break;
                case 8:
                    x = "8️⃣";
                    break;
                case 9:
                    x = "9️⃣";
                    break;
                case 10:
                    x = "🔟";
                    break;
            }
            return $"▃▄▅▇{x}▇▅▄▃";
        }
    }
}
