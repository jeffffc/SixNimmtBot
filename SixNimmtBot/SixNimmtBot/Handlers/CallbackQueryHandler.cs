using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineKeyboardButtons;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SixNimmtBot.Handlers
{
    partial class Handler
    {
        public static void HandleQuery(CallbackQuery call)
        {
            if (call.Data != null)
            {
                var args = call.Data.Contains('|')
                ? new[] { call.Data.Split('|')[0], call.Data.Remove(0, call.Data.IndexOf('|') + 1) }
                : new[] { call.Data, null };

                var callback = Bot.Callbacks.FirstOrDefault(x => x.Trigger == args[0]);
                if (callback == null)
                {
                    // maybe it is from game
                    var g = Bot.GetGameByGuid(args[0]);
                    if (g != null)
                    {
                        var temp = $"game|{call.Data}";
                        args = new[] { temp.Split('|')[0], temp.Remove(0, temp.IndexOf('|') + 1) };
                        callback = Bot.Callbacks.FirstOrDefault(x => x.Trigger == args[0]);
                    }
                    if (callback == null)
                    {
                        BotMethods.AnswerCallback(call);
                        BotMethods.Edit("An error occured: No callback method found! The developer was informed!", call.Message);
                        BotMethods.Send(Constants.LogGroupId, $"Error occured! No callback method \"{args[0]}\" found! at CallbackHandler.cs, OnCallback()");
                        return;
                    }
                }

                if (callback.AdminOnly && !Helpers.IsGroupAdmin(call))
                {
                    BotMethods.AnswerCallback(call,  "You are not a group admin!", true);
                    return;
                }

                if (callback.DevOnly && !Constants.Dev.Contains(call.From.Id))
                {
                    call.Message.Reply("You aren't a bot dev!");
                    return;
                }

                callback.Method.Invoke(call, args);

                
            }
            else
            {
                //
            }
        }

        public static InlineKeyboardMarkup GetConfigMenu(long id)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton> { };
            //base menu
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigChangeLanguage", GetLanguage(id)), $"config|lang|{id}"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigTableCard", GetLanguage(id)), $"config|table|{id}"));
            // for group only
            if (id < 0)
            {
                buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigCardDeck", GetLanguage(id)), $"config|deck|{id}"));
            }
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigDone", GetLanguage(id)), $"config|done|{id}"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                twoMenu.Add(new[] { buttons[i] });
            }

            var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
            return menu;
        }

        public static InlineKeyboardMarkup GetConfigTableMenu(long id)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            using (var db = new SixNimmtDb())
            {
                bool? currentSetting = null;
                var gp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (gp != null)
                    currentSetting = gp?.UseSticker;
                else
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                    if (p != null)
                        currentSetting = p?.UseSticker;
                }

                foreach (var i in new string[] { GetTranslation("ChooseUseText", GetLanguage(id)), GetTranslation("ChooseUseSticker", GetLanguage(id)) })
                    buttons.Add(new InlineKeyboardCallbackButton(i, currentSetting != true ? $"config|table|{id}|sticker" : $"config|table|{id}|text"));
                var twoMenu = new List<InlineKeyboardButton[]>();
                for (var i = 0; i < buttons.Count; i++)
                {
                    if (buttons.Count - 1 == i)
                    {
                        twoMenu.Add(new[] { buttons[i] });
                    }
                    else
                        twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                    i++;
                }
                twoMenu.Add(new[] { new InlineKeyboardCallbackButton(GetTranslation("ConfigBack", GetLanguage(id)), $"config|back|{id}") });

                var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
                return menu;
            }
        }

        public static InlineKeyboardMarkup GetConfigLangMenu(long id, bool setlang = false)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            //base menu
            foreach (string lang in Program.Langs.Keys)
                buttons.Add(new InlineKeyboardCallbackButton(lang, !setlang ? $"config|lang|{id}|{lang}" : $"setlang|lang|{id}|{lang}"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }
            if (!setlang)
                twoMenu.Add(new[] { new InlineKeyboardCallbackButton(GetTranslation("ConfigBack", GetLanguage(id)), $"config|back|{id}") });

            var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
            return menu;
        }

        public static InlineKeyboardMarkup GetGetLangMenu()
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton>();
            //base menu
            foreach (string lang in Program.Langs.Keys)
                buttons.Add(new InlineKeyboardCallbackButton(lang, $"getlang|get|{lang}"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                if (buttons.Count - 1 == i)
                {
                    twoMenu.Add(new[] { buttons[i] });
                }
                else
                    twoMenu.Add(new[] { buttons[i], buttons[i + 1] });
                i++;
            }
            twoMenu.Add(new[] { new InlineKeyboardCallbackButton("Cancel", $"getlang|cancel") });

            var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
            return menu;
        }

        public static InlineKeyboardMarkup GetConfigCardDeckMenu(long id)
        {
            List<InlineKeyboardButton> buttons = new List<InlineKeyboardButton> { };
            //base menu
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigStaticDeck", GetLanguage(id)), $"config|deck|{id}|static"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigDynamicDeck", GetLanguage(id)), $"config|deck|{id}|dynamic"));
            buttons.Add(new InlineKeyboardCallbackButton(GetTranslation("ConfigBack", GetLanguage(id)), $"config|back|{id}"));
            var twoMenu = new List<InlineKeyboardButton[]>();
            for (var i = 0; i < buttons.Count; i++)
            {
                twoMenu.Add(new[] { buttons[i] });
            }

            var menu = new InlineKeyboardMarkup(twoMenu.ToArray());
            return menu;
        }
    }
}
