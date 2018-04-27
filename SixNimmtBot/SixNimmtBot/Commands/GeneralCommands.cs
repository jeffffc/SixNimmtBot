using Database;
using SixNimmtBot.Attributes;
using SixNimmtBot.Handlers;
using SixNimmtBot.Models.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.Payments;
using static SixNimmtBot.Helpers;

namespace SixNimmtBot
{
    public partial class Commands
    {
        [Attributes.Command(Trigger = "start")]
        public static void Start(Message msg, string[] args)
        {
            if (msg.Chat.Type != ChatType.Private) return;
            if (args[1] == null)
            {
                msg.Reply("Thank you for starting me. This bot is still in BETA phase. Use /config to change the language and display of table cards.");
            }
        }

        [Attributes.Command(Trigger = "ping")]
        public static void Ping(Message msg, string[] args)
        {
            var now = DateTime.UtcNow;
            var span1 = now - msg.Date.ToUniversalTime();
            var ping = msg.Reply($"Time to receive: {span1.ToString("mm\\:ss\\.ff")}");
            // var span2 = ping.Date.ToUniversalTime() - now;
            // Bot.Edit(ping.Text + $"{Environment.NewLine}Time to send: {span2.ToString("mm\\:ss\\.ff")}", ping);

        }

        [Attributes.Command(Trigger = "lang", DevOnly = true)]
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

        [Attributes.Command(Trigger = "grouplang", DevOnly = true)]
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

        [Attributes.Command(Trigger = "config", AdminOnly = true)]
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

        [Attributes.Command(Trigger = "setlang")]
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

        [Attributes.Command(Trigger = "maintenance", DevOnly = true)]
        public static void Maintenance(Message msg, string[] args)
        {
            Program.MaintMode = !Program.MaintMode;
            Bot.Send(msg.Chat.Id, $"Maintenance Mode: {Program.MaintMode}");
        }

        [Attributes.Command(Trigger = "getlang")]
        public static void GetLang(Message msg, string[] args)
        {
            if (!Constants.Dev.Contains(msg.From.Id) && msg.Chat.Type != ChatType.Private) return;

            Bot.Send(msg.Chat.Id, GetTranslation("GetWhichLang", GetLanguage(msg.Chat.Id)), Handler.GetGetLangMenu());
        }

        [Attributes.Command(Trigger = "reloadlangs", DevOnly = true)]
        public static void ReloadLang(Message msg, string[] args)
        {
            Program.English = Helpers.ReadEnglish();
            Program.Langs = Helpers.ReadLanguageFiles();
            msg.Reply("Done.");
        }

        [Attributes.Command(Trigger = "uploadlang", DevOnly = true)]
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

        [Attributes.Command(Trigger = "rules")]
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

        [Attributes.Command(Trigger = "donatetest")]
        public static void DonateTest(Message msg, string[] args)
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
                        var providerToken = Constants.DonationTestToken;
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

        [Attributes.Command(Trigger = "donate")]
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

        [Attributes.Command(Trigger = "runinfo")]
        public static void RunInfo(Message msg, string[] args)
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string buildDate = new DateTime(2000, 1, 1).Add(new TimeSpan(
                TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
                TimeSpan.TicksPerSecond * 2 * version.Revision)).ToString();
            string uptime = $"{(DateTime.Now - Program.Startup):dd\\.hh\\:mm\\:ss\\.ff}";
            int gamecount = Bot.Games.Count;
            int playercount = Bot.Games.Select(x => x.Players.Count).Sum();
            Bot.Send(msg.Chat.Id, $"Version: {version.ToString().ToCode()}\nBuild Date: {buildDate.ToCode()}\nUptime: {uptime.ToCode()}\nGame count: {gamecount.ToString().ToCode()}\nPlayer count: {playercount.ToString().ToCode()}");
        }

        [Attributes.Command(Trigger = "stats")]
        public static void Stats(Message msg, string[] args)
        {
            using (var db = new SixNimmtDb())
            {
                var isGroup = !(msg.Chat.Type == ChatType.Private);
                var player = msg.ReplyToMessage?.From ?? msg.From;
                var playerId = player.Id;
                var temp = db.Players.FirstOrDefault(x => x.TelegramId == playerId).Achievements ?? 0;
                var achv = (Achievements)temp;
                var achvCount = achv.GetUniqueFlags().Count();
                if (!db.GamePlayers.Any(x => x.Player.TelegramId == playerId))
                {
                    msg.Reply(GetTranslation("StatsHaveNotPlayed", GetLanguage(playerId)));
                    return;
                }
                var playerName = $"{player.GetName()} (<code>{playerId}</code>)";
                int numOfWins = db.GetNumOfWins(playerId).First().Value;
                int numOfLoss = db.GetNumOfLoss(playerId).First().Value;
                var numOfGames = db.GetPlayerNumOfGames(playerId).First().Value;
                var numOfBulls = db.GetPlayerNumOfBulls(playerId).First().Value;

                double avg = (double)numOfBulls / numOfGames;
                var send = GetTranslation("StatsDetails", GetLanguage(isGroup == true ? msg.Chat.Id : playerId),
                    playerName,
                    achvCount.ToBold(),
                    numOfGames.ToBold(),
                    /* $"{numOfWins} ({Math.Round((double)numOfWins * 100 / numOfGames, 0)}%)".ToBold(),
                    $"{numOfGames - numOfWins} ({Math.Round((double)(numOfGames - numOfWins) * 100 / numOfGames, 0)}%)".ToBold(),
                    numOfBulls.ToBold(),
                    avg.ToString("F").ToBold()
                    */
                    // number of wins
                    numOfWins.ToBold(),
                    numOfLoss.ToBold(),
                    numOfBulls.ToBold(),
                    avg.ToString("F").ToBold()
                    );
                msg.Reply(send);
            }
        }

        [Attributes.Command(Trigger = "globalstats")]
        public static void GlobalStats(Message msg, string[] args)
        {
            using (var db = new SixNimmtDb())
            {
                int numOfGroups = db.GetTotalNumOfGroups().First().Value;
                int numOfPlayers = db.GetTotalNumOfPlayers().First().Value;

                int numOfGames = db.GetTotalNumOfGames().First().Value;
                var numOfBulls = db.GetTotalNumOfBulls().First().Value;
                var average = db.GetAverageNumOfBulls().First().Value;

                var send = GetTranslation("GlobalStatsDetails", GetLanguage(msg.Chat.Id),
                    numOfGroups.ToBold(),
                    numOfPlayers.ToBold(),
                    numOfGames.ToBold(),
                    numOfBulls.ToBold(),
                    average.ToString("F").ToBold()
                    );
                msg.Reply(send);
            }
        }

        [Attributes.Command(Trigger = "groupstats", GroupOnly = true)]
        public static void GroupStats(Message msg, string[] args)
        {
            var chatId = msg.Chat.Id;
            using (var db = new SixNimmtDb())
            {
                var numOfGames = db.GetGroupNumOfGames(chatId).First().Value;
                var numOfBulls = db.GetGroupNumOfBulls(chatId).First().Value;

                var playerAverageBulls = db.GetGroupAverageNumOfBulls(chatId).ToList();
                var playerBullsText = "";
                var i = 1;
                var temp = playerAverageBulls.ToList();
                temp.Reverse();
                temp = temp.Take(3).ToList();
                foreach (var res in temp)
                {
                    var name = res.Username == null ? res.Name.ToBold() : $"<a href='https://t.me/{res.Username}'>{res.Name}</a>";
                    var bulls = (decimal)res.average;
                    playerBullsText += $"{i}. {GetTranslation("GroupStatsPlayerBulls", GetLanguage(chatId), bulls.ToString("F").ToBold(), name)}\n";
                    i++;
                }
                var playerBullsText2 = "";
                i = 1;
                foreach (var res in playerAverageBulls.Take(3).ToList())
                {
                    var name = res.Username == null ? res.Name.ToBold() : $"<a href='https://t.me/{res.Username}'>{res.Name}</a>";
                    var bulls = (decimal)res.average;
                    playerBullsText2 += $"{i}. {GetTranslation("GroupStatsPlayerBulls", GetLanguage(chatId), bulls.ToString("F").ToBold(), name)}\n";
                    i++;
                }

                var send = GetTranslation("GroupStatsDetails", GetLanguage(msg.Chat.Id),
                    msg.Chat.Title.FormatHTML().ToBold(),
                    numOfGames.ToBold(),
                    numOfBulls.ToBold(),
                    playerBullsText,
                    playerBullsText2
                    );
                msg.Reply(send);
            }
        }


        [Attributes.Command(Trigger = "achievements")]
        public static void Achievements(Message msg, string[] args)
        {
            using (var db = new SixNimmtDb())
            {
                var p = db.Players.FirstOrDefault(x => x.TelegramId == msg.From.Id);
                var temp = p.Achievements ?? 0;
                var achv = (Achievements)temp;
                var lang = GetLanguage(msg.From.Id);
                var achvList = achv.GetUniqueFlags().ToList();
                var msg1 = $"{GetTranslation("AchievementsGot", lang, achvList.Count)}\n\n";
                msg1 = achvList.Aggregate(msg1, (current, a) => current + $"{a.GetAchvName(lang).ToBold()}\n{a.GetAchvDescription(lang)}\n\n");
                var noAchvList = achv.GetUniqueFlags(true).ToList();
                var msg2 = $"{GetTranslation("AchievementsLack", lang, noAchvList.Count)}\n\n";
                msg2 = noAchvList.Aggregate(msg2, (current, a) => current + $"{a.GetAchvName(lang).ToBold()}\n{a.GetAchvDescription(lang)}\n\n");
                msg.ReplyPM(new[] { msg1, msg2 });
            }
        }

        [Attributes.Command(Trigger = "setlink", GroupOnly = true, AdminOnly = true)]
        public static void SetLink(Message msg, string[] args)
        {
            if (!String.IsNullOrEmpty(msg.Chat.Username))
            {
                msg.Reply(GetTranslation("SetLinkDone", GetLanguage(msg.Chat.Id), $"<a href='https://t.me/{msg.Chat.Username}'>{msg.Chat.Title.FormatHTML()}</a>"));
                return;
            }

            if (args.Length < 2 || String.IsNullOrEmpty(args[1]))
            {
                msg.Reply(GetTranslation("SetLinkNoLink", GetLanguage(msg.Chat.Id)));
                return;
            }

            var link = args[1].Trim();

            if (!Regex.IsMatch(link, @"^(https?:\/\/)?t(elegram)?\.me\/joinchat\/([a-zA-Z0-9_\-]+)$"))
            {
                msg.Reply(GetTranslation("SetLinkInvalidLink", GetLanguage(msg.Chat.Id)));
                return;
            }
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == msg.Chat.Id) ?? MakeDefaultGroup(msg.Chat);
                grp.GroupLink = link;
                db.SaveChanges();
            }

            msg.Reply(GetTranslation("SetLinkDone", GetLanguage(msg.Chat.Id), $"<a href=\"{link}\">{msg.Chat.Title.FormatHTML()}</a>"));
        }

        [Attributes.Command(Trigger = "remlink", AdminOnly = true, GroupOnly = true)]
        public static void RemLink(Message msg, string[] args)
        {
            using (var db = new SixNimmtDb())
            {
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == msg.Chat.Id) ?? MakeDefaultGroup(msg.Chat);
                grp.GroupLink = null;
                db.SaveChanges();
            }
            msg.Reply(GetTranslation("LinkRemoved", GetLanguage(msg.Chat.Id)));
        }
    }
}
