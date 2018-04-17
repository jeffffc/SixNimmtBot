using Database;
using SixNimmtBot.Attributes;
using SixNimmtBot.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using static SixNimmtBot.Helpers;

namespace SixNimmtBot
{
    public partial class Commands
    {
        [Command(Trigger = "ping")]
        public static void Ping(Message msg, string[] args)
        {
            var now = DateTime.UtcNow;
            var span1 = now - msg.Date.ToUniversalTime();
            var ping = msg.Reply($"Time to receive: {span1.ToString("mm\\:ss\\.ff")}");
            // var span2 = ping.Date.ToUniversalTime() - now;
            // Bot.Edit(ping.Text + $"{Environment.NewLine}Time to send: {span2.ToString("mm\\:ss\\.ff")}", ping);

        }

        [Command(Trigger = "lang", DevOnly = true)]
        public static void ChangeLang(Message msg, string[] args)
        {
            if (args == null)
                return;
            var lang = args[1];
            try
            {
                using (var db = new SixNimmtDb())
                {
                    var p = db.Players.FirstOrDefault(x => x.TelegramId == msg.From.Id);
                    if (p != null)
                    {
                        p.Language = lang;
                        db.SaveChanges();
                        Bot.Send(msg.Chat.Id, "OK");
                    }
                }
            }
            catch { }
        }

        [Command(Trigger = "grouplang", DevOnly = true)]
        public static void ChangeGroupLang(Message msg, string[] args)
        {
            if (args == null)
                return;
            var lang = args[1];
            try
            {
                using (var db = new SixNimmtDb())
                {
                    var p = db.Groups.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
                    if (p != null)
                    {
                        p.Language = lang;
                        db.SaveChanges();
                        Bot.Send(msg.Chat.Id, "OK");
                    }
                }
            }
            catch { }
        }

        [Command(Trigger = "config", AdminOnly = true)]
        public static void Config(Message msg, string[] args)
        {
            var id = msg.Chat.Id;

            //make sure the group is in the database
            using (var db = new SixNimmtDb())
            {
                switch (msg.Chat.Type)
                {
                    case ChatType.Group:
                    case ChatType.Supergroup:
                        var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                        if (grp == null)
                        {
                            grp = Helpers.MakeDefaultGroup(msg.Chat);
                            db.Groups.Add(grp);
                        }
                        grp.UserName = msg.Chat.Username;
                        grp.Name = msg.Chat.Title;
                        break;
                    case ChatType.Private:
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p == null)
                        {
                            p = Helpers.MakeDefaultPlayer(msg.From);
                            db.Players.Add(p);
                        }
                        p.UserName = msg.From.Username;
                        p.Name = msg.From.FirstName;
                        break;
                }
                db.SaveChanges();
            }

            var menu = Handler.GetConfigMenu(msg.Chat.Id);
            Bot.Send(msg.From.Id, GetTranslation("WhatToDo", GetLanguage(msg.Chat.Id)), replyMarkup: menu);
        }

        [Command(Trigger = "setlang")]
        public static void SetLang(Message msg, string[] args)
        {
            var id = msg.From.Id;

            //make sure the user is in the database
            using (var db = new SixNimmtDb())
            {
                var user = db.Players.FirstOrDefault(x => x.TelegramId == id);
                if (user == null)
                {
                    user = Helpers.MakeDefaultPlayer(msg.From);
                    db.Players.Add(user);
                }

                user.UserName = msg.From.Username;
                user.Name = msg.From.FirstName;
                db.SaveChanges();
            }

            var menu = Handler.GetConfigLangMenu(msg.From.Id, true);
            Bot.Send(msg.From.Id, GetTranslation("ChoosePMLanguage", GetLanguage(msg.From.Id)), replyMarkup: menu);
        }

        [Command(Trigger = "maintenance", DevOnly = true)]
        public static void Maintenance(Message msg, string[] args)
        {
            Program.MaintMode = !Program.MaintMode;
            Bot.Send(msg.Chat.Id, $"Maintenance Mode: {Program.MaintMode}");
        }

        [Command(Trigger = "getlang")]
        public static void GetLang(Message msg, string[] args)
        {
            if (!Constants.Dev.Contains(msg.From.Id) && msg.Chat.Type != ChatType.Private) return;

            Bot.Send(msg.Chat.Id, GetTranslation("GetWhichLang", GetLanguage(msg.Chat.Id)), Handler.GetGetLangMenu());
        }

        [Command(Trigger = "reloadlangs", DevOnly = true)]
        public static void ReloadLang(Message msg, string[] args)
        {
            Program.English = Helpers.ReadEnglish();
            Program.Langs = Helpers.ReadLanguageFiles();
            msg.Reply("Done.");
        }

        [Command(Trigger = "uploadlang", DevOnly = true)]
        public static void UploadLang(Message msg, string[] args)
        {
            try
            {
                var id = msg.Chat.Id;
                if (msg.ReplyToMessage?.Type != MessageType.DocumentMessage)
                {
                    Bot.Send(id, "Please reply to the file with /uploadlang");
                    return;
                }
                var fileid = msg.ReplyToMessage.Document?.FileId;
                if (fileid != null)
                    UploadFile(fileid, id,
                        msg.ReplyToMessage.Document.FileName,
                        msg.MessageId);
            }
            catch (Exception e)
            {
                Bot.Send(msg.Chat.Id, e.Message, parseMode: ParseMode.Default);
            }
        }

        [Command(Trigger = "rules")]
        public static void Rules(Message msg, string[] args)
        {
            try
            {
                Bot.Send(msg.From.Id, GetTranslation("Rules", GetLanguage(msg.From.Id)));
            }
            catch
            {
                msg.Reply(GetTranslation("NotStartedBot", GetLanguage(msg.From.Id)), GenerateStartMe(msg.From.Id));
                return;
            }
            if (msg.Chat.Type != ChatType.Private)
            {
                msg.Reply(GetTranslation("SentPM", GetLanguage(msg.From.Id)));
                return;
            }
        }

        [Command(Trigger = "donate")]
        public static void Donate(Message msg, string[] args)
        {
            if (msg.Chat.Type != ChatType.Private)
            {
                msg.Reply(GetTranslation("DonatePrivateOnly", GetLanguage(msg.From.Id)));
                return;
            }
            var argList = msg.Text.Split(' ');
            int money = 0;
            if (argList.Count() <= 1)
            {
                msg.Reply(GetTranslation("DonateInputValue", GetLanguage(msg.From.Id)));
                return;
            }
            else
            {
                if (!int.TryParse(argList[1], out money))
                {
                    msg.Reply(GetTranslation("DonateInputValue", GetLanguage(msg.From.Id)));
                    return;
                }
                else
                {
                    if (money < 10 || money > 100000)
                    {
                        msg.Reply(GetTranslation("DonateWrongValue", GetLanguage(msg.From.Id)));
                        return;
                    }
                    else
                    {
                        var providerToken = Constants.DonationLiveToken;
                        var title = GetTranslation("DonateTitle", GetLanguage(msg.From.Id));
                        var description = GetTranslation("DonateDescription", GetLanguage(msg.From.Id), money);
                        var payload = Constants.DonationPayload + msg.From.Id.ToString();
                        var startParameter = "donate";
                        var currency = "HKD";
                        var prices = new LabeledPrice[1] {
                            new LabeledPrice {
                                Label = GetTranslation("Donation", GetLanguage(msg.From.Id)),
                                Amount = money * 100
                            } };
                        Bot.Api.SendInvoiceAsync(msg.From.Id, title, description, payload, providerToken,
                            startParameter, currency, prices);
                    }
                }
            }

        }

        [Command(Trigger = "runinfo")]
        public static void RunInfo(Message msg, string[] args)
        {
            string uptime = $"{(DateTime.Now - Program.Startup):dd\\.hh\\:mm\\:ss\\.ff}";
            int gamecount = Bot.Games.Count;
            int playercount = Bot.Games.Select(x => x.Players.Count).Sum();
            Bot.Send(msg.Chat.Id, GetTranslation("Runinfo", GetLanguage(msg.Chat.Id), uptime, gamecount, playercount));
        }

    }
}
