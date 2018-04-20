using SixNimmtBot.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Database;
using SixNimmtBot.Handlers;
using static SixNimmtBot.Helpers;

namespace SixNimmtBot
{
    public partial class Callbacks
    {
        [Callback(Trigger = "game")]
        public static void GameQuery(CallbackQuery query, string[] args)
        {
            var temp = args[1].Split('|');
            var gameId = temp[0];
            var playerId = temp[1];
            var playerChoice = temp[2];

            var game = Bot.GetGameByGuid(gameId);
            if (game != null)
            {
                game.HandleQuery(query, temp);
            }
            else
            {
                // should not happen
            }
        }

        [Callback(Trigger = "update", DevOnly = true)]
        public static void UpdateQuery(CallbackQuery query, string[] args)
        {
            var temp = args[1].Split('|');
            var update = temp[0];
            switch (update)
            {
                case "yes":
                    Bot.Api.EditMessageReplyMarkupAsync(query.Message.Chat.Id, query.Message.MessageId, null).Wait();
                    Commands.Update(query.Message, args);
                    break;
                case "no":
                    Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, "OK, I will do nothing.");
                    break;
            }
        }

        [Callback(Trigger = "config")]
        public static void ConfigQuery(CallbackQuery query, string[] args)
        {
            var temp = args[1].Split('|');
            var chatId = long.Parse(temp[1]);
            if (temp[0] == "lang")
            {
                if (temp.Length == 2)
                {
                    var menu = Handler.GetConfigLangMenu(chatId);
                    Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, GetTranslation(chatId > 0 ? "ChoosePMLanguage" : "ChooseLanguage", GetLanguage(chatId)), menu);
                }
                if (temp.Length > 2)
                {
                    var chosenLang = temp[2];
                    Handler.SetLanguage(chatId, chosenLang);
                    var menu = Handler.GetConfigMenu(chatId);
                    var toSend = GetTranslation("ReceivedButton", GetLanguage(chatId)) + Environment.NewLine + GetTranslation("WhatToDo", GetLanguage(chatId));
                    Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, toSend, menu);
                }
            }
            else if (temp[0] == "table")
            {
                if (temp.Length == 2)
                {
                    var menu = Handler.GetConfigTableMenu(chatId);
                    var groupOrPlayer = Helpers.GetGroupOrPlayer(chatId);
                    var current = groupOrPlayer.UseSticker;
                    Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, 
                        GetTranslation("ChooseTable", GetLanguage(chatId), 
                        current == true ? GetTranslation("ChooseUseSticker", GetLanguage(chatId)) : GetTranslation("ChooseUseText", GetLanguage(chatId))), menu);
                }
                if (temp.Length > 2)
                {
                    var chosen = temp[2] == "sticker";
                    Handler.SetTableConfig(chatId, chosen);
                    var menu = Handler.GetConfigMenu(chatId);
                    var toSend = GetTranslation("ReceivedButton", GetLanguage(chatId)) + Environment.NewLine + GetTranslation("WhatToDo", GetLanguage(chatId));
                    Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, toSend, menu);
                }
            }
            else if (temp[0] == "done")
            {
                Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, GetTranslation("ConfigDone", GetLanguage(chatId)));
            }
            else if (temp[0] == "back")
            {
                Bot.Edit(query.Message.Chat.Id, query.Message.MessageId, GetTranslation("WhatToDo", Handler.GetLanguage(chatId)), Handler.GetConfigMenu(chatId));
            }
            return;
        }
    }
}
