using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models.General
{
    public class GameCountStat
    {
        public DateTime GameDate { get; set; }
        public int Num { get; set; }
    }

    public class GroupGameCountStat
    {
        public int Num { get; set; }
        public string Name { get; set; }
    }
}
