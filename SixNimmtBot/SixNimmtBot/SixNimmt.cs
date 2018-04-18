﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using SixNimmtBot.Models;
using SixNimmtBot;
using System.Threading;
using Database;
using System.Diagnostics;
using System.IO;
using Telegram.Bot.Types.InlineKeyboardButtons;
using System.Xml.Linq;
using static SixNimmtBot.Helpers;
using ConsoleTables;
using System.Drawing;
using System.Drawing.Imaging;

namespace SixNimmtBot
{
    public class SixNimmt : IDisposable
    {
        #region Initiate Variables

        public long ChatId;
        public string GroupName;
        public int GameId;
        public Group DbGroup;
        public List<SNPlayer> Players = new List<SNPlayer>();
        public List<SNCard[]> TableCards = new List<SNCard[]>();
        public SNPlayer Initiator;
        public Guid Id = Guid.NewGuid();
        public SNDeck CardDeck;
        public int JoinTime = Constants.JoinTime;
        public GamePhase Phase = GamePhase.Joining;
        private int _secondsToAdd = 0;
        private int _playerList = 0;
        public string CurrentTableStickerId;
        public bool UseSticker = false;
        public Database.Game DbGame;
        

        public Locale Locale;
        public string Language = "English";

        #endregion


        public SixNimmt(long chatId, User u, string groupName)
        {
            #region Creating New Game - Preparation
            using (var db = new SixNimmtDb())
            {
                ChatId = chatId;
                GroupName = groupName;
                DbGroup = db.Groups.FirstOrDefault(x => x.GroupId == ChatId);
                UseSticker = DbGroup.UseSticker ?? false;
                LoadLanguage(DbGroup.Language);
                if (DbGroup == null)
                    Bot.RemoveGame(this);
            }
            // something
            #endregion

            var msg = GetTranslation("NewGame", u.GetName());
            // beta message
            msg += Environment.NewLine + Environment.NewLine + GetTranslation("Beta");
            Bot.Send(chatId, msg);
            AddPlayer(u, true);
            Initiator = Players[0];
            new Task(() => { NotifyNextGamePlayers(); }).Start();
            new Thread(GameTimer).Start();
        }

        #region Main method

        private void GameTimer()
        {
            while (Phase != GamePhase.Ending)
            {
                for (var i = 0; i < JoinTime; i++)
                {
                    if (this.Phase == GamePhase.InGame)
                        break;
                    if (this.Phase == GamePhase.Ending)
                        return;
                    //try to remove duplicated game
                    if (i == 10)
                    {
                        var count = Bot.Games.Count(x => x.ChatId == ChatId);
                        if (count > 1)
                        {
                            var toDel = Bot.Games.FirstOrDefault(x => x.Id != this.Id && x.Phase != GamePhase.InGame);
                            if (toDel != null)
                            {
                                Bot.Send(toDel.ChatId, GetTranslation("DuplicatedGameRemoving"));
                                toDel.Phase = GamePhase.Ending;
                                Bot.RemoveGame(toDel);
                            }
                        }
                    }
                    if (_secondsToAdd != 0)
                    {
                        i = Math.Max(i - _secondsToAdd, Constants.JoinTime - Constants.JoinTimeMax);
                        // Bot.Send(ChatId, GetTranslation("JoinTimeLeft", TimeSpan.FromSeconds(Constants.JoinTime - i).ToString(@"mm\:ss")));
                        _secondsToAdd = 0;
                    }
                    var specialTime = JoinTime - i;
                    if (new int[] { 10, 30, 60, 90 }.Contains(specialTime))
                    {
                        Bot.Send(ChatId, GetTranslation("JoinTimeSpecialSeconds", specialTime));
                    }
                    if (Players.Count == 10)
                        break;
                    Thread.Sleep(1000);
                }

                if (this.Phase == GamePhase.Ending)
                    return;
                do
                {
                    SNPlayer p = Players.FirstOrDefault(x => Players.Count(y => y.TelegramId == x.TelegramId) > 1);
                    if (p == null) break;
                    Players.Remove(p);
                }
                while (true);

                if (this.Phase == GamePhase.Ending)
                    return;

                if (this.Players.Count() >= 3)
                    this.Phase = GamePhase.InGame;
                if (this.Phase != GamePhase.InGame)
                {
                    /*
                    this.Phase = GamePhase.Ending;
                    Bot.RemoveGame(this);
                    Bot.Send(ChatId, "Game ended!");
                    */
                }
                else
                {
                    #region Ready to start game
                    if (Players.Count < 3)
                    {
                        Send(GetTranslation("GameEnded"));
                        return;
                    }

                    Bot.Send(ChatId, GetTranslation("GameStart"));

                    // create game + gameplayers in db
                    using (var db = new SixNimmtDb())
                    {
                        DbGame = new Database.Game
                        {
                            GrpId = DbGroup.Id,
                            GroupId = ChatId,
                            GroupName = GroupName,
                            TimeStarted = DateTime.UtcNow
                        };
                        db.Games.Add(DbGame);
                        db.SaveChanges();
                        GameId = DbGame.Id;
                        foreach (var p in Players)
                        {
                            GamePlayer DbGamePlayer = new GamePlayer
                            {
                                PlayerId = db.Players.FirstOrDefault(x => x.TelegramId == p.TelegramId).Id,
                                GameId = GameId
                            };
                            db.GamePlayers.Add(DbGamePlayer);
                        }
                        db.SaveChanges();
                    }

                    PrepareGame();
                    SortTableCards();

                    // remove joined players from nextgame list
                    // RemoveFromNextGame(Players.Select(x => x.TelegramId).ToList());

                    #endregion

                    #region Start!
                    SendTableCards();
                    foreach (var player in Players)
                    {
                        SendPM(player, GetPlayerInitialCards(player.CardsInHand));
                    }
                    while (Phase != GamePhase.Ending)
                    {
                        // _playerList = Send(GeneratePlayerList()).MessageId;
                        PlayersChooseCard();
                        if (Phase == GamePhase.Ending)
                            break;
                        // NextPlayer();
                    }
                    EndGame();
                    #endregion
                }
                this.Phase = GamePhase.Ending;
                Bot.Send(ChatId, GetTranslation("GameEnded"));

            }

            Bot.RemoveGame(this);
        }

        #endregion


        #region Player Control

        private void AddPlayer(User u, bool newGame = false)
        {
            var player = this.Players.FirstOrDefault(x => x.TelegramId == u.Id);
            if (player != null)
                return;

            using (var db = new SixNimmtDb())
            {
                var DbPlayer = db.Players.FirstOrDefault(x => x.TelegramId == u.Id);
                if (DbPlayer == null)
                {
                    DbPlayer = new Player
                    {
                        TelegramId = u.Id,
                        Name = u.FirstName,
                        Language = "English"
                    };
                    db.Players.Add(DbPlayer);
                    db.SaveChanges();
                }
                SNPlayer p = new SNPlayer
                {
                    Name = u.FirstName,
                    Id = DbPlayer.Id,
                    TelegramId = u.Id,
                    Username = u.Username,
                    UseSticker = DbPlayer.UseSticker ?? false
                };
                try
                {
                    Message ret;
                    try
                    {
                        ret = SendPM(p, GetTranslation("YouJoined", GroupName));
                    }
                    catch
                    {
                        Bot.Send(ChatId, GetTranslation("NotStartedBot", u.GetName()), GenerateStartMe());
                        return;
                    }
                }
                catch { }
                this.Players.Add(p);
            }
            if (!newGame)
                _secondsToAdd += 15;

            do
            {
                SNPlayer p = Players.FirstOrDefault(x => Players.Count(y => y.TelegramId == x.TelegramId) > 1);
                if (p == null) break;
                Players.Remove(p);
            }
            while (true);

            Send(GetTranslation("JoinedGame", u.GetName()) + Environment.NewLine + GetTranslation("JoinInfo", Players.Count, 3, 10));
        }

        private void RemovePlayer(User user)
        {
            if (this.Phase != GamePhase.Joining) return;

            var player = this.Players.FirstOrDefault(x => x.TelegramId == user.Id);
            if (player == null)
                return;

            this.Players.Remove(player);

            do
            {
                SNPlayer p = Players.FirstOrDefault(x => Players.Count(y => y.TelegramId == x.TelegramId) > 1);
                if (p == null) break;
                Players.Remove(p);
            }
            while (true);

            Send(GetTranslation("FledGame", user.GetName()) + Environment.NewLine + GetTranslation("JoinInfo", Players.Count, 3, 8));
        }

        public void CleanPlayers()
        {
            foreach (var p in Players)
            {
                p.Choice = 0;
                p.CurrentQuestion = null;
            }
        }

        #endregion


        #region Visualize Cards Related

        public string GetTableCardsString()
        {
            return TableCards.Select(row => row.Where(x => x != null).Select(x => x.Number.ToString()).Aggregate((x, y) => x + " " + y))
                .Aggregate((x, y) => x + "\n" + y);
        }

        public void SortTableCards()
        {
            TableCards.Sort((x, y) => x[Array.FindLastIndex(x, z => z != null)].Number.CompareTo(y[Array.FindLastIndex(y, z => z != null)].Number));
        }

        public void SendTableCards()
        {
            // Send(GetTableCardsString());
            if (!UseSticker)
                Send(TextToTable(TableCards));
            // Bot.Api.SendPhotoAsync(ChatId, GetTableCardsImage(TableCards)).Wait();
            else
                CurrentTableStickerId = Bot.SendSticker(ChatId, GetTableCardsImage(TableCards)).Sticker.FileId;
            
        }

        public FileToSend GetTableCardsImage(List<SNCard[]> cards)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Image temp = (Image)Constants.boardImage.Clone();
                Graphics board = Graphics.FromImage(temp);

                int w = Constants.widthSides;
                int h = Constants.heightSides;
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        var c = cards[j][i];
                        if (c != null)
                            board.DrawImage(Constants.cardImages[c.Number - 1], w, h);
                        h += Constants.eachHeight;
                    }
                    w += Constants.eachWidth;
                    h = Constants.heightSides;
                }

                var s = ToStream(temp, ImageFormat.Jpeg);
                // boardImage.Save(outputPath);
                return new FileToSend("sticker", s);
            }
        }

        private Stream ToStream(Image image, ImageFormat format)
        {
            // 
            // image.Save(stream, format);
            Bitmap img = new Bitmap(image);
            byte[] bytes;
            using (WebP webp = new WebP())
                bytes = webp.EncodeLossless(img);
            var stream = new System.IO.MemoryStream(bytes);
            stream.Position = 0;
            return stream;
        }

        public string TextToTable(List<SNCard[]> cards)
        {
            var table = new ConsoleTable("C 1", "C 2", "C 3", "C 4", "C 5");
            foreach (var row in cards)
            {
                var tx = row.Select(x => x == null ? " " : x.Number.ToString().PadLeft(3, ' ')).ToList();
                // tx.Add(row.Where(x => x != null).Sum(x => x.Bulls).ToString().PadLeft(2, '0'));
                table.AddRow(tx.ToArray());
            }
            return table.ToStringAlternative().ToCode();
        }

        public string GetPlayerInitialCards(List<SNCard> cards)
        {
            var list = cards.OrderBy(x => x.Number);
            string msg = "";
            // Select(x => x.GetName()).Aggregate((x, y) => x + "\n" + y)
            for (int i = 0; i < list.Count(); i += 2)
            {
                var items = list.Skip(i).Take(2).ToList();
                msg += items.Select(x => x.GetName().PadRight(13)).Aggregate((x, y) => x + " " + y) + Environment.NewLine;
            }
            return msg.ToCode();
        }

        public string GetBullsTotalString(List<SNCard> row)
        {
            return GetBullsTotalString(row.ToArray());
        }

        public string GetBullsTotalString(SNCard[] row)
        {
            return $"{row.Where(x => x != null).Select(x => x.GetName().ToCode()).Aggregate((x, y) => x + " + " + y)}";
        }

        #endregion


        #region Game Methods

        public void PlayersChooseCard()
        {
            try
            {
                if (Players.All(x => x.CardsInHand.Count == 0))
                    Phase = GamePhase.Ending;

                if (Phase == GamePhase.Ending) return;

                if (Players.All(x => x.CardsInHand.Count == 1))
                {
                    Send(GetTranslation("OneCardLeft"));
                }
                else
                {
                    Send(GetTranslation("EveryoneChooseCard"));
                    var currentSticker = GetTableCardsImage(TableCards);
                    foreach (var p in Players)
                    {
                        if (p.UseSticker)
                        {
                            if (CurrentTableStickerId == null)
                                CurrentTableStickerId = Bot.SendSticker(p.TelegramId, currentSticker).Sticker.FileId;
                            else
                                SendSticker(p, CurrentTableStickerId);
                            SendMenu(p, GetTranslation("ChooseCard"), GenerateMenu(p, p.CardsInHand.OrderBy(x => x.Number).ToList()));
                        }
                        else
                        {
                            SendMenu(p, $"{TextToTable(TableCards)}\n\n{GetTranslation("ChooseCard")}", GenerateMenu(p, p.CardsInHand.OrderBy(x => x.Number).ToList()));
                        }
                        Thread.Sleep(400);
                    }
                    CurrentTableStickerId = null;
                    for (int i = 0; i < Constants.ChooseCardTime; i++)
                    {
                        Thread.Sleep(1000);
                        if (Players.All(x => x.CurrentQuestion == null))
                            break;
                    }
                }


                // check of afk
                foreach (var p in Players)
                {
                    if (p.CurrentQuestion != null)
                    {
                        p.Choice = 0;
                        Bot.Edit(p.TelegramId, p.CurrentQuestion.MessageId, GetTranslation("TimesUpButton"));
                        p.CurrentQuestion = null;
                    }
                }

                // move chosen card from hand to the pile
                List<SNCard> tempPile = new List<SNCard>();
                foreach (var p in Players)
                {
                    SNCard card;
                    if (p.Choice != 0)
                    {
                        card = p.CardsInHand.FirstOrDefault(x => x.Number == p.Choice);
                    }
                    else
                    {
                        card = p.CardsInHand[Helpers.RandomNum(p.CardsInHand.Count)];
                    }
                    p.CardsInHand.Remove(card);
                    card.PlayedBy = p;
                    tempPile.Add(card);
                    tempPile.Sort((x, y) => x.Number.CompareTo(y.Number));
                }

                Send($"{GetTranslation("CardsPlayed")}\n{tempPile.Select(x => x.Number.ToString()).Aggregate((x, y) => x + " " + y)}");
                Thread.Sleep(4000);

                int row1Diff = 0;
                int row2Diff = 0;
                int row3Diff = 0;
                int row4Diff = 0;

                foreach (var card in tempPile)
                {
                    row1Diff = card.Number - TableCards[0].Last(x => x != null).Number;
                    if (row1Diff < 0) row1Diff = 105;

                    row2Diff = card.Number - TableCards[1].Last(x => x != null).Number;
                    if (row2Diff < 0) row2Diff = 105;

                    row3Diff = card.Number - TableCards[2].Last(x => x != null).Number;
                    if (row3Diff < 0) row3Diff = 105;

                    row4Diff = card.Number - TableCards[3].Last(x => x != null).Number;
                    if (row4Diff < 0) row4Diff = 105;

                    var msg = $"{GetTranslation("PlayedCard", card.PlayedBy.GetName(), card.Number)}\n";

                    // if its lower than all4 row's rightmost card
                    if (new[] { row1Diff, row2Diff, row3Diff, row4Diff }.All(x => x == 105))
                    {
                        /* OLD: PLAYER AUTO KEEP THE ROW WITH LOWEST BULLS
                        TableCards.Sort((x, y) => x.Where(z => z != null).Sum(z => z.Bulls).CompareTo(y.Where(z => z != null).Sum(z => z.Bulls)));
                        // give the lowest valued row to this player
                        card.PlayedBy.KeptCards.AddRange(TableCards[0]);
                        msg += $"This card is lower than all 4 row's rightmost cards, {card.PlayedBy.GetName()} has to keep the row of the least bulls:\n" +
                            GetBullsTotalString(TableCards[0]);
                        Send(msg);
                        Array.Clear(TableCards[0], 0, TableCards[0].Length);
                        TableCards[0][0] = card;
                        */

                        /* NEW: PLAYER CHOOSE WHICH ROW TO KEEP */
                        Send($"{msg}\n{GetTranslation("CardLowerThanAll", card.PlayedBy.GetName())}");
                        SendMenu(card.PlayedBy,
                            GetTableCardsString() +
                            Environment.NewLine + Environment.NewLine +
                            GetTranslation("CardLowerThanAll", card.GetName()),
                            GenerateMenu(card.PlayedBy, TableCards));
                        for (int i = 0; i < Constants.ChooseCardTime; i++)
                        {
                            Thread.Sleep(1000);
                            if (card.PlayedBy.CurrentQuestion == null)
                                break;
                        }

                        // Check AFK
                        if (card.PlayedBy.CurrentQuestion != null)
                        {
                            card.PlayedBy.Choice = -1;
                            Bot.Edit(card.PlayedBy.TelegramId, card.PlayedBy.CurrentQuestion.MessageId, GetTranslation("TimesUpButton"));
                            card.PlayedBy.CurrentQuestion = null;
                        }

                        var rowChosen = card.PlayedBy.Choice == -1 ? TableCards[RandomNum(TableCards.Count)] : TableCards[(int)card.PlayedBy.Choice];
                        card.PlayedBy.KeptCards.AddRange(rowChosen);
                        msg = $"{GetTranslation("KeptRow", card.PlayedBy.GetName())}\n" + GetBullsTotalString(rowChosen);
                        Send(msg);
                        Array.Clear(rowChosen, 0, rowChosen.Length);
                        rowChosen[0] = card;
                        card.PlayedBy.Choice = null;
                    }
                    else
                    {
                        SNCard[] thisRow = TableCards[0];
                        // if it falls to the first row
                        if (row1Diff < row2Diff && row1Diff < row3Diff && row1Diff < row4Diff)
                            thisRow = TableCards[0];
                        // if it falls to the second row
                        else if (row2Diff < row1Diff && row2Diff < row3Diff && row1Diff < row4Diff)
                            thisRow = TableCards[1];
                        // if it falls to the third row
                        else if (row3Diff < row1Diff && row3Diff < row2Diff && row3Diff < row4Diff)
                            thisRow = TableCards[2];
                        // if it falls to the fourth row
                        else if (row4Diff < row1Diff && row4Diff < row2Diff && row4Diff < row3Diff)
                            thisRow = TableCards[3];

                        // if this row is full already
                        if (thisRow.Count(x => x != null) == 5)
                        {
                            // keep the cards and add the current card to this row
                            card.PlayedBy.KeptCards.AddRange(thisRow);
                            msg += $"{GetTranslation("CardExceedRow", card.PlayedBy.GetName())}\n" +
                                GetBullsTotalString(thisRow);
                            Send(msg);
                            Array.Clear(thisRow, 0, thisRow.Length);
                            thisRow[0] = card;
                        }
                        else
                        {
                            // if not yet full, add to the end
                            thisRow[Array.FindIndex(thisRow, x => x == null)] = card;
                            Send(msg += GetTranslation("SafeCard", TableCards.IndexOf(thisRow) + 1));
                        }
                    }

                    // at last sort the table again
                    SortTableCards();
                    SendTableCards();
                    Thread.Sleep(5000);
                }
                CleanPlayers();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        public void PrepareGame()
        {
            // create deck
            CardDeck = new SNDeck();

            for (int i = 1; i <= 104; i++)
            {
                SNCard card = new SNCard(i);
                CardDeck.Cards.Add(card);
            }
            CardDeck.Cards.Shuffle(10);

            // create table arrays
            for (int i = 0; i < 4; i++)
                TableCards.Add(new SNCard[5]);

            // select default cards to table
            var tempCards = new List<SNCard>();
            for (int i = 0; i < 4; i++)
                tempCards.Add(CardDeck.Cards.Pop());
            tempCards = tempCards.OrderBy(x => x.Number).ToList();

            // assign cards to table
            for (int i = 0; i < 4; i++)
                TableCards[i][0] = tempCards[i];

            // assign cards to players
            foreach (SNPlayer p in Players)
            {
#if DEBUG
                for (int i = 0; i < 3; i++)
#else
                for (int i = 0; i < 10; i++)
#endif
                    p.CardsInHand.Add(CardDeck.Cards.Pop());
            }
        }

        public void EndGame()
        {
            Send(GetTranslation("AllCardsUsed"));
            Thread.Sleep(3000);
            foreach (var p in Players)
            {
                var bullsCount = p.KeptCards.Count == 0 ? 0 : p.KeptCards.Where(x => x != null).Sum(x => x.Bulls);
                p.Score = bullsCount;
            }
            var finalPlayers = Players.OrderByDescending(x => x.Score);
            foreach (var p in finalPlayers)
            {
                Send(GetTranslation("PlayerBull", p.GetName(), p.Score));
                Thread.Sleep(3000);
            }
            var wonPlayers = Players.Where(x => x.Score == finalPlayers.Last().Score);
            Send($"{wonPlayers.Select(x => x.GetName()).Aggregate((x, y) => x + ", " + y)} {GetTranslation("Won")}");
        }

        public void NotifyNextGamePlayers()
        {
            var grpId = ChatId;
            using (var db = new SixNimmtDb())
            {
                var dbGrp = db.Groups.FirstOrDefault(x => x.GroupId == grpId);
                if (dbGrp != null)
                {
                    var toNotify = db.NotifyGames.Where(x => x.GroupId == grpId && x.UserId != Initiator.TelegramId).Select(x => x.UserId).ToList();
                    foreach (int user in toNotify)
                    {
                        Bot.Send(user, GetTranslation("GameIsStarting", GroupName));
                    }
                    db.Database.ExecuteSqlCommand($"DELETE FROM NotifyGame WHERE GROUPID = {grpId}");
                    db.SaveChanges();
                }
            }
        }

        #endregion


        #region Bot API Related Methods

        public Message Send(string msg)
        {
            return Bot.Send(ChatId, msg);
        }

        public Message SendPM(SNPlayer p, string msg)
        {
            return Bot.Send(p.TelegramId, msg);
        }

        public Message SendMenu(SNPlayer p, string msg, InlineKeyboardMarkup markup)
        {
            var sent = Bot.Send(p.TelegramId, msg, markup);
            p.CurrentQuestion = new QuestionAsked
            {
                MessageId = sent.MessageId
            };
            return sent;
        }

        public Message SendSticker(SNPlayer p, string fileId)
        {
            return Bot.SendSticker(p.TelegramId, fileId);
        }

        public Message SendStickerMenu(SNPlayer p, string fileId, InlineKeyboardMarkup markup)
        {
            var sent = Bot.SendSticker(p.TelegramId, fileId, markup);
            p.CurrentQuestion = new QuestionAsked
            {
                MessageId = sent.MessageId
            };
            return sent;
        }

        public Message Reply(int oldMessageId, string msg)
        {
            return BotMethods.Reply(ChatId, oldMessageId, msg);
        }

        public InlineKeyboardMarkup GenerateStartMe()
        {
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();
            row.Add(new InlineKeyboardUrlButton(GetTranslation("StartMe"), $"https://telegram.me/{Bot.Me.Username}"));
            rows.Add(row.ToArray());
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        /// <summary>
        /// Generate Menu for general card playing
        /// </summary>
        /// <param name="p">Player</param>
        /// <param name="cardList">Player's Cards in Hand</param>
        /// <returns>A 2-Column InlineKeyboardMarkup</returns>
        public InlineKeyboardMarkup GenerateMenu(SNPlayer p, List<SNCard> cardList)
        {
            var buttons = new List<Tuple<string, string>>();
            foreach (SNCard card in cardList)
            {
                buttons.Add(new Tuple<string, string>(card.GetName(), $"game|{this.Id}|{p.TelegramId}|card|{card.Number.ToString()}"));
            }
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i += 2)
            {
                row.Clear();
                var subButtons = buttons.Skip(i).Take(2).ToList();
                foreach (var button in subButtons)
                    row.Add(new InlineKeyboardCallbackButton(button.Item1, button.Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        /// <summary>
        /// Generate Menu for player who has to choose a row to keep the cards 
        /// </summary>
        /// <param name="p">Player</param>
        /// <param name="tableCards">Table Cards</param>
        /// <returns>A InlineKeyboardMarkup</returns>
        public InlineKeyboardMarkup GenerateMenu(SNPlayer p, List<SNCard[]> tableCards)
        {
            var buttons = new List<Tuple<string, string>>();
            for (int i = 0; i < tableCards.Count; i++)
            {
                var cardRow = tableCards[i];
                var rowBullsCount = cardRow.Where(x => x != null).Sum(x => x.Bulls);
                var label = $"Row {i + 1}: {rowBullsCount} 🐮";
                buttons.Add(new Tuple<string, string>(label, $"{this.Id}|{p.TelegramId}|row|{i}"));
            }
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();

            for (int i = 0; i < buttons.Count; i++)
            {
                row.Clear();
                row.Add(new InlineKeyboardCallbackButton(buttons[i].Item1, buttons[i].Item2));
                rows.Add(row.ToArray());
            }
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        #endregion


        #region Incoming Message/Query Handling

        public void HandleMessage(Message msg)
        {
            switch (msg.Text.ToLower().Substring(1).Split()[0].Split('@')[0])
            {
                case "join":
                    if (Phase == GamePhase.Joining)
                        AddPlayer(msg.From);
                    break;
                case "flee":
                    if (Phase == GamePhase.Joining)
                        RemovePlayer(msg.From);
                    else if (Phase == GamePhase.InGame)
                        Send(GetTranslation("CantFleeRunningGame"));
                    break;
                case "startgame":
                    if (Phase == GamePhase.Joining)
                        AddPlayer(msg.From);
                    break;
                case "forcestart":
                    if (this.Players.Count() >= 2) Phase = GamePhase.InGame;
                    else
                    {
                        Send(GetTranslation("GameEnded"));
                        Phase = GamePhase.Ending;
                        Bot.RemoveGame(this);
                    }
                    break;
                case "killgame":
                    Send(GetTranslation("KillGame"));
                    Phase = GamePhase.Ending;
                    Bot.RemoveGame(this);
                    break;
                case "seq":
                    if (_playerList == 0)
                        Reply(msg.MessageId, GetTranslation("PlayerSequenceNotStarted"));
                    else
                        Reply(_playerList, GetTranslation("GetPlayerSequence"));
                    break;
                case "extend":
                    if (Phase == GamePhase.Joining)
                    {
                        _secondsToAdd += Constants.ExtendTime;
                        Reply(msg.MessageId, GetTranslation("ExtendJoining", Constants.ExtendTime));
                    }
                    break;
            }

        }

        public void HandleQuery(CallbackQuery query, string[] args)
        {
            // args[0] = GameGuid
            // args[1] = playerId
            // args[2] = "card" or "row"
            // args[2] = cardChosen
            var p = Players.FirstOrDefault(x => x.TelegramId == int.Parse(args[1]));
            switch (args[2])
            {
                case "card":
                    if (p.CurrentQuestion != null)
                    {
                        p.Choice = int.Parse(args[3]);
                        BotMethods.Edit($"OK - {p.Choice}", query.Message);
                        p.CurrentQuestion = null;
                    }
                    break;
                case "row":
                    if (p.CurrentQuestion != null)
                    {
                        p.Choice = int.Parse(args[3]);
                        BotMethods.Edit($"OK - Row {p.Choice + 1}", query.Message);
                        p.CurrentQuestion = null;
                    }
                    break;
            }
        }

        #endregion


        #region Language Related

        public void LoadLanguage(string language)
        {
            try
            {
                var files = Directory.GetFiles(Constants.GetLangDirectory());
                var file = files.First(x => Path.GetFileNameWithoutExtension(x) == language);
                {
                    var doc = XDocument.Load(file);
                    Locale = new Locale
                    {
                        Language = Path.GetFileNameWithoutExtension(file),
                        File = doc
                    };
                }
                Language = Locale.Language;
            }
            catch
            {
                if (language != "English")
                    LoadLanguage("English");
            }
        }

        private string GetTranslation(string key, params object[] args)
        {
            try
            {
                var strings = Locale.File.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
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
                        Program.English.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
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

        #endregion

        #region General Methods

        public void Dispose()
        {
            Players?.Clear();
            Players = null;
            CardDeck = null;
            // MessageQueueing = false;
        }

        public void Log(Exception ex)
        {
            ex.LogError(ChatId);
            Send("Sorry there is some problem with me, I gonna go die now.");
            this.Phase = GamePhase.Ending;
            Bot.RemoveGame(this);
        }

        #endregion

        #region Constants

        public enum GamePhase
        {
            Joining, InGame, Ending
        }

        #endregion
    }
}