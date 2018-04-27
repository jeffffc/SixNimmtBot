using Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;

namespace SixNimmtBot.Handlers
{
    partial class Handler
    {
        public static void HandleUpdates(ITelegramBotClient Bot)
        {
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnUpdate += BotOnUpdateReceived;
            Bot.OnReceiveError += BotOnReceiveError;
        }


        private static void BotOnUpdateReceived(object sender, UpdateEventArgs updateEventArgs)
        {
            // answer precheckout for donation
            if (updateEventArgs.Update.PreCheckoutQuery != null)
            {
                var pcq = updateEventArgs.Update.PreCheckoutQuery;
                if (pcq.InvoicePayload != (Constants.DonationPayload + pcq.From.Id.ToString()))
                    Bot.Api.AnswerPreCheckoutQueryAsync(pcq.Id, false, GetTranslation("DonateError", GetLanguage(pcq.From.Id)));
                else
                    Bot.Api.AnswerPreCheckoutQueryAsync(pcq.Id, true);
            }
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {

        }

        private static void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {

        }

        private static void BotOnMessageReceived(object sender, MessageEventArgs e)
        {
            new Task(() => { Handler.HandleMessage(e.Message); }).Start();
        }

        private static void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            new Task(() => { Handler.HandleQuery(e.CallbackQuery); }).Start();
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            receiveErrorEventArgs.ApiRequestException.LogError();
        }

        private static string GetTranslation(string key, string language, params object[] args)
        {
            try
            {
                var strings = Program.Langs[language].XMLFile.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Program.English.XMLFile.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                if (strings != null)
                {
                    var values = strings.Descendants("value");
                    var choice = Helpers.RandomNum(values.Count());
                    var selected = values.ElementAt(choice).Value;

                    return String.Format(selected, args).Replace("\\n", Environment.NewLine);
                }
                else
                {
                    throw new Exception($"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}");
                }
            }
            catch (Exception e)
            {
                try
                {
                    //try the english string to be sure
                    var strings =
                        Program.English.XMLFile.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                    var values = strings?.Descendants("value");
                    if (values != null)
                    {
                        var choice = Helpers.RandomNum(values.Count());
                        var selected = values.ElementAt(choice).Value;
                        // ReSharper disable once AssignNullToNotNullAttribute
                        return String.Format(selected, args).Replace("\\n", Environment.NewLine);
                    }
                    else
                        throw new Exception("Cannot load english string for fallback");
                }
                catch
                {
                    throw new Exception(
                        $"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}",
                        e);
                }
            }
        }

        public static string GetLanguage(long id)
        {
            using (var db = new SixNimmtDb())
            {
                Player p = null;
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                    p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                if (p != null && String.IsNullOrEmpty(p.Language))
                {
                    p.Language = "English";
                    db.SaveChanges();
                }
                return grp?.Language ?? p?.Language ?? "English";
            }
        }

        public static void SetLanguage(long chatId, string lang)
        {
            if (int.TryParse(chatId.ToString(), out int o))
            {
                SetLanguage(o, lang);
                return;
            }
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == chatId);
                if (grp == null)
                    return;
                grp.Language = lang;
                db.SaveChanges();
            }
        }

        public static void SetLanguage(int userId, string lang)
        {
            using (var db = new SixNimmtDb())
            {
                var user = db.Players.FirstOrDefault(x => x.TelegramId == userId);
                if (user == null)
                    return;
                user.Language = lang;
                db.SaveChanges();
            }
        }

        public static void SetTableConfig(long chatId, bool config)
        {
            if (int.TryParse(chatId.ToString(), out int o))
            {
                SetTableConfig(o, config);
                return;
            }
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == chatId);
                if (grp == null)
                    return;
                grp.UseSticker = config;
                db.SaveChanges();
            }
        }

        public static void SetTableConfig(int userId, bool config)
        {
            using (var db = new SixNimmtDb())
            {
                var user = db.Players.FirstOrDefault(x => x.TelegramId == userId);
                if (user == null)
                    return;
                user.UseSticker = config;
                db.SaveChanges();
            }
        }

        public static void SetCardDeckConfig(long chatId, bool useDynamic)
        {
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == chatId);
                if (grp == null)
                    return;
                grp.DynamicDeck = useDynamic;
                db.SaveChanges();
            }
        }

        public static void SetGroupListConfig(long chatId, bool show)
        {
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == chatId);
                if (grp == null)
                    return;
                grp.ShowOnGroupList = show;
                db.SaveChanges();
            }
        }

        public static void SetChooseCardTimeConfig(long chatId, int chooseCardTime)
        {
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == chatId);
                if (grp == null)
                    return;
                grp.ChooseCardTime = chooseCardTime;
                db.SaveChanges();
            }
        }
    }
}
