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
using System.Windows.Forms.DataVisualization.Charting;

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
            if (args[1] == null)
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
                if (msg.ReplyToMessage?.Type != MessageType.Document)
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

        [Attributes.Command(Trigger = "full", DevOnly = true)]
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


        [Attributes.Command(Trigger = "growth", DevOnly = true)]
        public static void CheckGrowth(Message msg, string[] args)
        {
            var i = 7;
            if (args[1] != null)
            {
                int.TryParse(args[1].Split()[0], out i);
            }
            using (var db = new SixNimmtDb())
            {
                try
                {
                    var sql = $@"
    select top {i} 
	CONVERT(date, timestarted) as [GameDate], 
	count(*) as [Num]
	from game
	where timeended is not null 
	and convert(date, timestarted) <> convert(date, getdate())
	group by CONVERT(date, timestarted) 
	order by CONVERT(date, timestarted) desc";
                    var res = db.Database.SqlQuery<GameCountStat>(sql).ToList();
                    res.Reverse();
                    var ds = new DataSet();
                    var dt = new DataTable();
                    dt.Columns.Add("GameDate", typeof(string));
                    dt.Columns.Add("Num", typeof(int));

                    foreach (var r in res)
                    {
                        var row = dt.NewRow();
                        row[0] = r.GameDate.ToShortDateString();
                        row[1] = r.Num;
                        dt.Rows.Add(row);
                    }
                    ds.Tables.Add(dt);
                    var chart = CreateChart(ds, $"Game Count for past {i} days", "GameDate", "Num", SeriesChartType.Spline, 400, 250);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        chart.SaveImage(ms, ChartImageFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);
                        // var image = new FileToSend("growth", ms);
                        Bot.Api.SendPhotoAsync(msg.Chat.Id, ms, replyToMessageId: msg.MessageId).Wait();
                    }
                }
                catch (Exception e)
                {
                    //
                }
                //
            }
        }

        [Attributes.Command(Trigger = "groupact", DevOnly = true)]
        public static void CheckGroupActivity(Message msg, string[] args)
        {
            var i = 7;
            if (args[1] != null)
            {
                int.TryParse(args[1].Split()[0], out i);
            }
            using (var db = new SixNimmtDb())
            {
                try
                {
                    var sql = $@"
    select top 10
	count(*) as [Num], [group].name as [Name]
	from game
	join [group] on game.grpid = [group].id
	where timeended is not null 
	and convert(date, timestarted) <> convert(date, getdate())
	and timeended > DATEADD(DAY, {-i}, getdate())
	group by [group].name
	order by count(*) desc";
                    var res = db.Database.SqlQuery<GroupGameCountStat>(sql).ToList();
                    res.Reverse();
                    var ds = new DataSet();
                    var dt = new DataTable();
                    dt.Columns.Add("Num", typeof(int));
                    dt.Columns.Add("Name", typeof(string));


                    foreach (var r in res)
                    {
                        var row = dt.NewRow();
                        row[0] = r.Num;
                        row[1] = r.Name;
                        dt.Rows.Add(row);
                    }
                    ds.Tables.Add(dt);
                    var chart = CreateChart(ds, $"Group Activity for past {i} days", "Name", "Num", SeriesChartType.Bar, 800, 500);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        chart.SaveImage(ms, ChartImageFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);
                        // var image = new FileToSend("growth", ms);
                        Bot.Api.SendPhotoAsync(msg.Chat.Id, ms, replyToMessageId: msg.MessageId).Wait();
                    }
                }
                catch (Exception e)
                {
                    //
                }
                //
            }
        }
    }
}
