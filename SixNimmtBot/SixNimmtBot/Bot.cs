﻿using SixNimmtBot.Models.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace SixNimmtBot
{
    public class Bot
    {
        public static ITelegramBotClient Api;
        public static User Me;

        internal static HashSet<Models.General.Command> Commands = new HashSet<Models.General.Command>();
        internal static HashSet<Models.General.Callback> Callbacks = new HashSet<Models.General.Callback>();
        public delegate void CommandMethod(Message msg, string[] args);
        public delegate void CallbackMethod(CallbackQuery query, string[] args);


        internal static Message Send(long chatId, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            return BotMethods.Send(chatId, text, replyMarkup, parseMode, disableWebPagePreview, disableNotification);
        }

        internal static Message SendSticker(long chatId, string fileId, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            return BotMethods.SendSticker(chatId, fileId, replyMarkup, disableNotification);
        }

        internal static Message SendSticker(long chatId, Stream sticker, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            return BotMethods.SendSticker(chatId, sticker, replyMarkup, disableNotification);
        }

        internal static Message Edit(long chatId, int oldMessageId, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return BotMethods.Edit(chatId, oldMessageId, text, replyMarkup, parseMode, disableWebPagePreview, disableNotification);
            }
            catch (Exception ex)
            {
                ex.LogError();
                return null;
            }
        }

        internal static List<GroupAdmin> GetChatAdmins(long chatid, bool forceCacheUpdate = false)
        {
            try
            {   // Admins are cached for 1 hour
                string itemIndex = $"{chatid}";
                List<GroupAdmin> admins = Program.AdminCache[itemIndex] as List<GroupAdmin>; // Read admin list from cache
                if (admins == null || forceCacheUpdate)
                {
                    admins = Api.GetChatAdministratorsAsync(chatid).Result.Select(x =>
                        new GroupAdmin(x.User.Id, chatid, x.User.FirstName)).ToList();

                    CacheItemPolicy policy = new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddHours(1) };
                    Program.AdminCache.Set(itemIndex, admins, policy); // Write admin list into cache
                }

                return admins;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        #region Game related
        public static List<SixNimmt> Games { get { return Program.Games; } }

        public static SixNimmt GetGameByGuid(string Id)
        {
            return GetGameByGuid(Guid.Parse(Id));
        }

        public static SixNimmt GetGameByGuid(Guid guid)
        {
            return Program.Games.OrderBy(x => x.TimeCreated).FirstOrDefault(x => x.Id == guid);
        }

        public static SixNimmt GetGameByChatId(long chatId)
        {
            return Program.Games.OrderBy(x => x.TimeCreated).FirstOrDefault(x => x.ChatId == chatId);
        }

        public static void AddGame(SixNimmt game)
        {
            Program.Games.Add(game);
        }

        public static void RemoveGame(SixNimmt game)
        {
            Program.Games.Remove(game);
        }

        public static void RemoveGame(string id)
        {
            RemoveGame(GetGameByGuid(id));
        }

        public static void RemoveGame(long chatId)
        {
            RemoveGame(GetGameByChatId(chatId));
        }

        #endregion

    }

    public static class BotMethods
    {
        #region Messages
        public static Message Send(long chatId, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            return Bot.Api.SendTextMessageAsync(chatId, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;

        }

        public static Message Send(this Chat chat, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(chat.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message SendSticker(long chatId, string fileId, IReplyMarkup replyMarkup = null, bool disableNotification = false)
        {
            return Bot.Api.SendStickerAsync(chatId, fileId, disableNotification, 0, replyMarkup).Result;
        }

        public static Message SendSticker(long chatId, Stream sticker, IReplyMarkup replyMarkup = null, bool disableNotification = false)
        {
            return Bot.Api.SendStickerAsync(chatId, sticker, disableNotification, 0, replyMarkup).Result;
        }

        public static Message Reply(this Message m, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(m.Chat.Id, text, parseMode, disableWebPagePreview, disableNotification, m.MessageId, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message Reply(long chatId, int oldMessageId, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(chatId, text, parseMode, disableWebPagePreview, disableNotification, oldMessageId, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message ReplyNoQuote(this Message m, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendTextMessageAsync(m.Chat.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static void ReplyPM(this Message m, string[] texts, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            foreach (var text in texts)
            {
                try
                {
                    var r = Bot.Api.SendTextMessageAsync(m.From.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
                    if (r == null)
                    {
                        m.Reply(Helpers.GetTranslation("NotStartedBot", Helpers.GetLanguage(m.From.Id)), new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                            InlineKeyboardButton.WithUrl("Start me!", $"https://t.me/{Bot.Me.Username}") }));
                        return;
                    }
                }
                catch (Exception e)
                {
                    e.LogError();
                }
            }
            if (m.Chat.Type != ChatType.Private)
                m.Reply(Helpers.GetTranslation("SentPM", Helpers.GetLanguage(m.From.Id)));
        }

        public static Message ReplyPM(this Message m, string text, IReplyMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                var r = Bot.Api.SendTextMessageAsync(m.From.Id, text, parseMode, disableWebPagePreview, disableNotification, 0, replyMarkup).Result;
                if (r == null)
                {
                    return m.Reply(Helpers.GetTranslation("NotStartedBot", Helpers.GetLanguage(m.From.Id)), new InlineKeyboardMarkup(new InlineKeyboardButton[] {
                        InlineKeyboardButton.WithUrl("Start me!", $"https://t.me/{Bot.Me.Username}") }));
                }
                if (m.Chat.Type != ChatType.Private)
                    m.Reply(Helpers.GetTranslation("SentPM", Helpers.GetLanguage(m.From.Id)));
                return r;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message Edit(string text, Message msg, InlineKeyboardMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true)
        {
            return Edit(msg.Chat.Id, msg.MessageId, text, replyMarkup, parseMode, disableWebPagePreview);
        }


        public static Message Edit(long chatId, int oldMessageId, string text, InlineKeyboardMarkup replyMarkup = null, ParseMode parseMode = ParseMode.Html, bool disableWebPagePreview = true, bool disableNotification = false)
        {
            try
            {
                var t = Bot.Api.EditMessageTextAsync(chatId, oldMessageId, text, parseMode, disableWebPagePreview, replyMarkup);
                t.Wait();
                return t.Result;
            }
            catch (Exception e)
            {
                if (e is AggregateException Agg && Agg.InnerExceptions.Any(x => x.Message.ToLower().Contains("message is not modified")))
                {
                    /*
                    var m = "Messae not modified." + Environment.NewLine;
                    m += $"Chat: {chatId}" + Environment.NewLine;
                    m += $"Text: {text}" + Environment.NewLine;
                    m += $"Time: {DateTime.UtcNow.ToLongTimeString()} UTC";
                    Send(Constants.LogGroupId, m);
                    */
                    return null;
                }
                e.LogError();
                return null;
            }
        }

        public static Message SendDocument(long chatId, string fileToSend, string caption = null, IReplyMarkup replyMarkup = null, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendDocumentAsync(chatId, fileToSend, caption, ParseMode.Default, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message SendDocument(long chatId, Stream fileToSend, string caption = null, IReplyMarkup replyMarkup = null, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendDocumentAsync(chatId, fileToSend, caption, ParseMode.Default, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }

        public static Message SendDocument(long chatId, InputOnlineFile fileToSend, string caption = null, IReplyMarkup replyMarkup = null, bool disableNotification = false)
        {
            try
            {
                return Bot.Api.SendDocumentAsync(chatId, fileToSend, caption, ParseMode.Default, disableNotification, 0, replyMarkup).Result;
            }
            catch (Exception e)
            {
                e.LogError();
                return null;
            }
        }
        #endregion

        #region Callbacks
        public static bool AnswerCallback(CallbackQuery query, string text = null, bool popup = false)
        {
            try
            {
                var t = (Task<bool>)Bot.Api.AnswerCallbackQueryAsync(query.Id, text, popup);
                return t.Result;            // Await this call in order to be sure it is sent in time
            }
            catch (Exception e)
            {
                e.LogError();
                return false;
            }
        }
        #endregion

    }
}
