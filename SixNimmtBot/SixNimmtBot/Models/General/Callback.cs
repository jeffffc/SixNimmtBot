using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models
{
    class Callback
    {
        public string Trigger { get; set; }
        public bool AdminOnly { get; set; }
        public bool DevOnly { get; set; }
        public Bot.CallbackMethod Method { get; set; }

        public Callback(string Trigger, bool AdminOnly, bool DevnOnly, Bot.CallbackMethod Method)
        {
            this.Trigger = Trigger;
            this.AdminOnly = AdminOnly;
            this.DevOnly = DevOnly;
            this.Method = Method;
        }
    }
}
