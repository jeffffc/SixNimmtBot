﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models
{
    public class SNPlayer
    {
        public int Id { get; set; }
        public int TelegramId { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public int Score { get; set; } = 0;
        public List<SNCard> CardsInHand = new List<SNCard>();
        public List<SNCard> KeptCards = new List<SNCard>();
        public int? Choice { get; set; } = 0;
        public bool UseSticker = false;

        public QuestionAsked CurrentQuestion { get; set; } = null;
    }

    public class QuestionAsked
    {
        public int MessageId { get; set; } = 0;
    }
}