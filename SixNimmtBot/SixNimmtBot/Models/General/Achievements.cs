using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SixNimmtBot.Helpers;

namespace SixNimmtBot.Models.General
{
    [Flags]
    public enum Achievements : long
    {
        None = 0,
        Newbie = 1, // Play a game
        SixNimmt = 2, // Take a full row
        Addicted = 4, // Play 100 games
        Professional = 8, //Win 100 games
        ThirtySixNimmt = 16, // Get 36 Bulls
        ZeroNimmt = 32, // No Bulls at all
    } // MAX VALUE: 9223372036854775807
      //            

    public static partial class Extensions
    {
        public static string GetAchvDescription(this Achievements value, string language)
        {
            return GetTranslation($"Achv{value.ToString()}Desc", language);
        }
        public static string GetAchvName(this Achievements value, string language)
        {
            return GetTranslation($"Achv{value.ToString()}Name", language);
        }

        public static IEnumerable<Achievements> GetUniqueFlags(this Enum flags, bool no = false)
        {
            ulong flag = 1;
            foreach (var value in Enum.GetValues(flags.GetType()).Cast<Achievements>())
            {
                ulong bits = Convert.ToUInt64(value);
                while (flag < bits)
                {
                    flag <<= 1;
                }

                if (flag == bits && no == false ? flags.HasFlag(value) : !flags.HasFlag(value))
                {
                    yield return value;
                }
            }
        }
    }
}