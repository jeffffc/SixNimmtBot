using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using SixNimmtBot;
using Database;

namespace SixNimmtBot.Handlers
{
    partial class Handler
    {
        public static void HandleMessage(Message msg)
        {
            switch (msg.Type)
            {
                case MessageType.TextMessage:
                    string text = msg.Text;
                    string[] args = text.Contains(' ')
                                    ? new[] { text.Split(' ')[0].ToLower(), text.Remove(0, text.IndexOf(' ') + 1) }
                                    : new[] { text.ToLower(), null };
                    if (args[0].EndsWith('@' + Bot.Me.Username.ToLower()))
                        args[0] = args[0].Remove(args[0].Length - Bot.Me.Username.Length - 1);
                    if (msg.Text.StartsWith("/"))
                    {
                        args[0] = args[0].Substring(1);
                        var cmd = Bot.Commands.FirstOrDefault(x => x.Trigger == args[0]);
                        if (cmd != null)
                        {
                            if (new[] { ChatType.Supergroup, ChatType.Group }.Contains(msg.Chat.Type))
                            {
                                using (var db = new SixNimmtDb())
                                {
                                    var DbGroup = db.Groups.FirstOrDefault(x => x.GroupId == msg.Chat.Id);
                                    if (DbGroup == null)
                                    {
                                        DbGroup = Helpers.MakeDefaultGroup(msg.Chat);
                                        db.Groups.Add(DbGroup);
                                        db.SaveChanges();
                                    }
                                }
                            }
                            if (cmd.GroupOnly && !new[] { ChatType.Supergroup, ChatType.Group }.Contains(msg.Chat.Type))
                            {
                                msg.Reply("This command can only be used in groups!");
                                return;
                            }

                            if (cmd.AdminOnly && new[] { ChatType.Supergroup, ChatType.Group }.Contains(msg.Chat.Type) && !Helpers.IsGroupAdmin(msg))
                            {
                                msg.Reply("You aren't a group admin!");
                                return;
                            }

                            if (cmd.DevOnly && !Constants.Dev.Contains(msg.From.Id))
                            {
                                msg.Reply("You aren't a bot dev!");
                                return;
                            }

                            cmd.Method.Invoke(msg, args);
                            return;
                        }
                    }
                    break;
                case MessageType.ServiceMessage:
                    bool me = false;
                    if (msg.NewChatMembers.Any())
                    {
                        foreach (var c in msg.NewChatMembers)
                        {
                            if (c.Id == Bot.Me.Id)
                                me = true;
                        }
                    }
                    if (msg.NewChatMember != null && msg.NewChatMember.Id == Bot.Me.Id)
                        me = true;

                    if (me)
                    {
                        msg.ReplyNoQuote("Thank you for adding me into this chat! [6 Nimmt!] is a board game orininated in Germany, it's meaning is [Take 6!]." +
                            "It is a easy game to be played by 3 - 10 players. Admins can use /config to do simple configurations. Press /startgame to start a new game now!");
                    }
                    break;
                    /*
                case MessageType.SuccessfulPayment:
                    var randomRef = Guid.NewGuid().ToString();
                    randomRef = $"#crim_{randomRef.Substring(randomRef.Length - 12)}";
                    using (var db = new SixNimmtDb())
                    {
                        var donate = new Donation
                        {
                            TelegramId = msg.From.Id,
                            Amount = msg.SuccessfulPayment.TotalAmount / 100,
                            Reference = randomRef,
                            DonationTime = DateTime.UtcNow
                        };
                        db.Donations.Add(donate);
                        db.SaveChanges();
                    }
                    // notify user successful donation, provide reference code for checking in case
                    msg.Reply(GetTranslation("DonateSuccessful", GetLanguage(msg.From.Id), randomRef));
                    // log who, how much, when and ref code to log group
                    Bot.Send(Constants.LogGroupId, $"Donation from user <a href='tg://user?id={msg.From.Id}'>{msg.From.FirstName.FormatHTML()}</a>.\nAmount: {msg.SuccessfulPayment.TotalAmount / 100} HKD\nReference: {randomRef}");
                    break;
                    */
            }
        }

    }
}
