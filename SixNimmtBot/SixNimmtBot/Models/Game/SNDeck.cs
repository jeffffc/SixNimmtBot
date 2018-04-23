using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models.Game
{
    public class SNDeck
    {
        public List<SNCard> Cards = new List<SNCard>();

        public SNDeck()
        {
            for (int i = 1; i <= 104; i++)
            {
                SNCard card = new SNCard(i);
                Cards.Add(card);
            }
            Cards.Shuffle(10);
        }

        public SNDeck(int numOfPlayers, bool random = false)
        {
            if (!random)
            {
                for (int i = 1; i <= (numOfPlayers * 10 + 4); i++)
                {
                    SNCard card = new SNCard(i);
                    Cards.Add(card);
                }
            }
            else
            {
                var temp = new List<SNCard>();
                for (int i = 1; i <= 104; i++)
                {
                    SNCard card = new SNCard(i);
                    temp.Add(card);
                }
                temp.Shuffle(5);
                Cards.AddRange(temp.Take(numOfPlayers * 10 + 4));
            }
            Cards.Shuffle(10);
        }
    }

}