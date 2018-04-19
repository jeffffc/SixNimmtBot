using Database;
using SixNimmtBot.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using static SixNimmtBot.Helpers;

namespace SixNimmtBot
{
    public partial class Commands
    {
        [Command(Trigger = "startgame", GroupOnly = true)]
        public static void StartGame(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                if (Program.MaintMode)
                {
                    Bot.Send(msg.Chat.Id, GetTranslation("CantStartGameMaintenance", GetLanguage(msg.Chat.Id)));
                    return;
                }


                Bot.AddGame(new SixNimmt(msg.Chat.Id, msg.From, msg.Chat.Title, msg.Chat.Username));
            }
            else
            {
                game.HandleMessage(msg);
                // msg.Reply(GetTranslation("ExistingGame", GetLanguage(msg.Chat.Id)));
            }
        }

        [Command(Trigger = "test")]
        public static void Testing(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
               game.HandleMessage(msg);
            }
        }

        [Command(Trigger = "join", GroupOnly = true)]
        public static void JoinGame(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
               game.HandleMessage(msg);
            }
        }

        [Command(Trigger = "flee", GroupOnly = true)]
        public static void FleeGame(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
               game.HandleMessage(msg);
            }
        }

        [Command(Trigger = "forcestart", GroupOnly = true, AdminOnly = true)]
        public static void ForceStart(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
               game.HandleMessage(msg);
            }
        }

        [Command(Trigger = "killgame", DevOnly = true)]
        public static void KillGame(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                game.HandleMessage(msg);
                Bot.RemoveGame(game);
                game.Dispose();
                game = null;
            }
        }

        [Command(Trigger = "nextgame", GroupOnly = true)]
        public static void NextGame(Message msg, string[] args)
        {
            if (msg.Chat.Type == ChatType.Private)
                return;
            var grpId = msg.Chat.Id;
            using (var db = new SixNimmtDb())
            {
                var dbGrp = db.Groups.FirstOrDefault(x => x.GroupId == grpId);
                if (dbGrp != null)
                {
                    var notified = db.NotifyGames.FirstOrDefault(x => x.GroupId == grpId && x.UserId == msg.From.Id);
                    if (notified != null)
                    {
                        Bot.Send(msg.From.Id, GetTranslation("AlreadyInWaitingList", GetLanguage(msg.From.Id)));
                        return;
                    }
                    else
                    {
                    }
                    db.Database.ExecuteSqlCommand($"INSERT INTO NotifyGame VALUES ({msg.From.Id}, {msg.Chat.Id})");
                    db.SaveChanges();
                    Bot.Send(msg.From.Id, GetTranslation("NextGame", GetLanguage(msg.From.Id)));
                }
            }
        }

        [Command(Trigger = "extend", GroupOnly = true)]
        public static void ExtendTimer(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
               game.HandleMessage(msg);
            }
        }

        [Command(Trigger = "mycards", GroupOnly = true)]
        public static void MyCards(Message msg, string[] args)
        {
            SixNimmt game = Bot.GetGameByChatId(msg.Chat.Id);
            if (game == null)
            {
                return;
            }
            else
            {
                game.HandleMessage(msg);
            }
        }
    }
}
