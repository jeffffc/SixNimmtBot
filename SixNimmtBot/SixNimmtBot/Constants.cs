using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SixNimmtBot
{
    public static class Constants
    {
        // Token from registry
        private static RegistryKey _key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\SixNimmt");
        public static string GetBotToken(string key)
        {
            return _key.GetValue(key, "").ToString();
        }
        private static string _logPath = @"C:\Logs\SixNimmt.log";
        public static string GetLogPath()
        {
            return Path.GetFullPath(_logPath);
        }
        private static string _languageDirectory = @"C:\SixNimmtLanguages";
        private static string _languageTempDirectory = Path.Combine(_languageDirectory, @"temp\");
        public static string GetLangDirectory(bool temp = false)
        {
            return (!temp) ? Path.GetFullPath(_languageDirectory) : Path.GetFullPath(_languageTempDirectory);
        }
        public static long LogGroupId = -1001117997439;
        public static int[] Dev = new int[] { 106665913, 295152997 };

        #region GameConstants
        public static int JoinTime = 120;
        public static int JoinTimeMax = 300;
#if DEBUG
        public static int ChooseCardTime = 30;
#else
        public static int ChooseCardTime = 45;
#endif
        public static int ExtendTime = 30;
        public static int WitnessTime = 15;

        #endregion

        public static string DonationLiveToken = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey("SOFTWARE\\SixNimmt").GetValue("DonationLiveToken").ToString();
        public static string DonationPayload = "CRIMINALDANCEBOTPAYLOAD:";

        public static string _imagePath = @"C:\SixNimmtImages\";
        public static string _boardPath = Path.Combine(_imagePath, "board.png");
        public static string _cardPath = Path.Combine(_imagePath, "card.png");
        public static string _outputPath = Path.Combine(_imagePath, "output.png");
        public static Image boardImage = Image.FromFile(_boardPath);
        public static Image cardImage = Image.FromFile(_cardPath);

        public static List<Image> cardImages = new List<Image> {};

        public static int maxWidth = 900;
        public static int maxHeight = 1000;
        public static int insideWidth = 850;
        public static int insideHeight = 960;
        public static int eachWidth = insideWidth / 5;
        public static int eachHeight = insideHeight / 4;

        public static int widthSides = (maxWidth - insideWidth) / 2;
        public static int heightSides = (maxHeight - insideHeight) / 2;

    }
}
