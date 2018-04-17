using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot.Models
{
    public class GroupAdmin
    {
        public int Id { get; set; }
        public long GroupId { get; set; }
        public string Name { get; set; }

        public GroupAdmin(int Id, long GroupId, string Name)
        {
            this.Id = Id;
            this.GroupId = GroupId;
            this.Name = Name;
        }
    }
}
