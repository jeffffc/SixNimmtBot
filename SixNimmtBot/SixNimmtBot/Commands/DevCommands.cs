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
using Telegram.Bot.Types.Enums;
using SixNimmtBot.Models.General;
using static SixNimmtBot.Helpers;

namespace SixNimmtBot
{
    public partial class Commands
    {
        [Attributes.Command(Trigger = "update", DevOnly = true)]
        public static void Update(Message msg, string[] args)
        {
            if (msg.Date > DateTime.UtcNow.AddSeconds(-3) || msg.From.Id == Bot.Me.Id)
            {
                var txt = "Waiting for games to finish and then I will update the bot.";
                Message sent;
                if (msg.From.Id == Bot.Me.Id)
                    sent = Bot.Edit(msg.Chat.Id, msg.MessageId, $"{msg.Text}\n\n{txt}");
                else
                    sent = msg.Reply(txt);
                Process.Start(Path.Combine(@"C:\SixNimmtBot\", "Updater.exe"), msg.Chat.Id.ToString());
                Program.MaintMode = true;
                new Thread(x => CheckCurrentGames(sent)).Start();
            }
        }

        public static void CheckCurrentGames(Message msg)
        {
            while (Bot.Games.Count > 0)
                Thread.Sleep(1000);
            msg.Reply("Updating the bot now...");
            Environment.Exit(1);
            return;
        }

        [Attributes.Command(Trigger = "sql", DevOnly = true)]
        public static void Sql(Message msg, string[] args)
        {
            if (args.Length == 1)
            {
                msg.Reply("You must enter a sql query.");
                return;
            }
            using (var db = new SixNimmtDb())
            {
                var conn = db.Database.Connection;
                if (conn.State != ConnectionState.Open)
                    conn.Open();
                string raw = "";

                var queries = args[1].Split(';');
                foreach (var sql in queries)
                {
                    try
                    {
                        using (var comm = conn.CreateCommand())
                        {
                            comm.CommandText = sql;
                            if (string.IsNullOrEmpty(sql)) continue;
                            var reader = comm.ExecuteReader();
                            var result = "";
                            if (reader.HasRows)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                    raw += $"<code>{reader.GetName(i).FormatHTML()}</code>" + (i == reader.FieldCount - 1 ? "" : " - ");
                                result += raw + Environment.NewLine;
                                raw = "";
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                        raw += (reader.IsDBNull(i) ? "<i>NULL</i>" : $"<code>{reader[i].ToString().FormatHTML()}</code>") + (i == reader.FieldCount - 1 ? "" : " - ");
                                    result += raw + Environment.NewLine;
                                    raw = "";
                                }
                            }

                            result += reader.RecordsAffected == -1 ? "" : (reader.RecordsAffected + " records affected");
                            result = !String.IsNullOrEmpty(result) ? result : (sql.ToLower().StartsWith("select") ? "Nothing found" : "Done.");
                            msg.Reply(result);
                        }
                    }
                    catch (Exception e)
                    {
                        msg.Reply($"<b>SQL Exception</b>:\n{e.Message}");
                    }
                }
            }
        }

        [Attributes.Command(Trigger = "media", DevOnly = true)]
        public static void GetMediaFileId(Message msg, string[] args)
        {
            if (msg.ReplyToMessage != null)
            { 
                if (msg.ReplyToMessage.Document != null)
                    msg.Reply(msg.ReplyToMessage.Document.FileId);
                if (msg.ReplyToMessage.Photo != null)
                    msg.Reply(msg.ReplyToMessage.Photo.Last().FileId);
                if (msg.ReplyToMessage.Video != null)
                    msg.Reply(msg.ReplyToMessage.Video.FileId);
            }
        }

        [Attributes.Command(Trigger = "addachv", DevOnly = true)]
        public static void AddAchv(Message msg, string[] args)
        {
            //get the user to add the achievement to
            //first, try by reply
            var id = 0;
            var achIndex = 0;
            var param = args[1].Split(' ');
            if (msg.ReplyToMessage != null)
            {
                var m = msg.ReplyToMessage;
                while (m.ReplyToMessage != null)
                    m = m.ReplyToMessage;
                //check for forwarded message

                id = m.From.Id;
                if (m.ForwardFrom != null)
                    id = m.ForwardFrom.Id;
            }
            else
            {
                //ok, check for a user mention
                var e = msg.Entities?.FirstOrDefault();
                if (e != null)
                {
                    switch (e.Type)
                    {
                        case MessageEntityType.Mention:
                            //get user
                            var username = msg.Text.Substring(e.Offset + 1, e.Length - 1);
                            using (var db = new SixNimmtDb())
                            {
                                id = db.Players.FirstOrDefault(x => x.UserName == username)?.TelegramId ?? 0;
                            }
                            break;
                        case MessageEntityType.TextMention:
                            id = e.User.Id;
                            break;
                    }
                    achIndex = 1;
                }
            }

            if (id == 0)
            {
                //check for arguments then
                if (int.TryParse(param[0], out id))
                    achIndex = 1;
                else if (int.TryParse(param[1], out id))
                    achIndex = 0;

            }


            if (id != 0)
            {
                //try to get the achievement
                if (Enum.TryParse(param[achIndex], out Achievements a))
                {
                    //get the player from database
                    using (var db = new SixNimmtDb())
                    {
                        var p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                        if (p != null)
                        {
                            if (p.Achievements == null)
                                p.Achievements = 0;
                            var ach = (Achievements)p.Achievements;
                            if (ach.HasFlag(a))
                            {
                                msg.Reply("This player already have this achivement!");
                                return; //no point making another db call if they already have it
                            }
                            ach = ach | a;
                            p.Achievements = (long)ach;
                            db.SaveChanges();
                            var achvMsg = GetTranslation("NewUnlocks", GetLanguage(p.TelegramId)).ToBold() + Environment.NewLine + Environment.NewLine;
                            achvMsg += $"{a.GetAchvName(GetLanguage(p.TelegramId)).ToBold()}\n{a.GetAchvDescription(GetLanguage(p.TelegramId))}";
                            Bot.Send(p.TelegramId, achvMsg);
                            msg.Reply($"Achievement {a} unlocked for {p.Name}");
                        }
                    }
                }
            }
            else
            {
                msg.Reply("Please reply to a (forwarded) message of the player, or provide the ID/Username/Mention.");
            }
        }

        [Attributes.Command(Trigger = "full")]
        public static void FullInfo(Message msg, string[] args)
        {
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            string buildDate = new DateTime(2000, 1, 1).Add(new TimeSpan(
                TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
                TimeSpan.TicksPerSecond * 2 * version.Revision)).ToString();
            string uptime = $"{(DateTime.Now - Program.Startup):dd\\.hh\\:mm\\:ss\\.ff}";
            int gamecount = Bot.Games.Count;
            int playercount = Bot.Games.Select(x => x.Players.Count).Sum();
            var toSend = "";
            foreach (var g in Bot.Games.ToList().OrderBy(x => x.Phase))
            {
                if (g.Phase == SixNimmt.GamePhase.InGame)
                    toSend += $"{g.GroupName.ToBold()}:\n--- {g.Players.Count.ToCode()} Players; Round {g.Round.ToCode()}\n";
                else
                    toSend += $"{g.GroupName.ToBold()}:\n--- {g.Players.Count.ToCode()} Players; {g.Phase.ToCode()}\n";
            }
            Bot.Send(msg.Chat.Id, $"Version: {version.ToString().ToCode()}\nBuild Date: {buildDate.ToCode()}\nUptime: {uptime.ToCode()}\nGame count: {gamecount.ToString().ToCode()}\nPlayer count: {playercount.ToString().ToCode()}\n\n{toSend}");
        }
    }
}
