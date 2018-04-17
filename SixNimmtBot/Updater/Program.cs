using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace SixNimmtBot
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var mainPath = @"C:\SixNimmtBot";
                var updatePath = @"C:\UpdateFiles\SixNimmt";

                Console.WriteLine("Waiting on bot to exit....");
                //first, wait for the bot to close out
                var botName = "SixNimmtBot";
                while (Process.GetProcessesByName(botName).Any())
                {
                    Thread.Sleep(100);
                }
                Console.WriteLine("Patching...");
                Thread.Sleep(500);
                //ok, it's off, patch it
                foreach (var file in Directory.GetFiles(updatePath))
                {
                    if (file.Contains("Updater"))
                        continue;
                    Console.WriteLine(file);
                    File.Copy(file, file.Replace(updatePath, mainPath), true);
                    File.Delete(file);
                }
                Console.WriteLine("Starting bot....");
                //now start it back up
                //if (!Process.GetProcessesByName("Werewolf Control").Any())
                var path = Path.Combine(mainPath, "SixNimmtBot.exe");
                Process.Start(path);
                Console.WriteLine("Update complete");
                Thread.Sleep(5000);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Thread.Sleep(-1);
            }
        }
    }
}