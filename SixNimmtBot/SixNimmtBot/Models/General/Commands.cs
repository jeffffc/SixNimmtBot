using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models.General
{
    class Command
    {
        public string Trigger { get; set; }
        public bool AdminOnly { get; set; }
        public bool DevOnly { get; set; }
        public bool GroupOnly { get; set; }
        public Bot.CommandMethod Method { get; set; }

        public Command(string Trigger, bool AdminOnly, bool DevAdminOnly, bool GroupOnly, Bot.CommandMethod Method)
        {
            this.Trigger = Trigger;
            this.AdminOnly = AdminOnly;
            this.DevOnly = DevAdminOnly;
            this.GroupOnly = GroupOnly;
            this.Method = Method;
        }
    }
}
