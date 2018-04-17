using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SixNimmtBot.Models
{
    public class Locale
    {
        public string Language { get; set; }
        public XDocument File { get; set; }
    }
}