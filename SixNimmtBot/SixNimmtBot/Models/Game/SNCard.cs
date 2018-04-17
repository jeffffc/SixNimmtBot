using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models
{
    public class SNCard
    {
        private int _number;
        public int Number
        {
            get
            {
                return _number;
            }
            set
            {
                _number = value;
                if (_number == 55)
                    Bulls = 7;
                else if (_number % 11 == 0)
                    Bulls = 5;
                else if (_number % 10 == 0)
                    Bulls = 3;
                else if (_number % 5 == 0)
                    Bulls = 2;
                else
                    Bulls = 1;
            }
        }
        public int Bulls { get; private set; }
        public SNPlayer PlayedBy { get; set; }

        public SNCard(int num)
        {
            Number = num;
        }
    }
}
