using Database;
using SixNimmtBot.Models;
using SixNimmtBot.Models.General;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using System.Data;
using SixNimmtBot.Models.Game;

namespace SixNimmtBot
{
    public static class Helpers
    {
        public static int RandomNum(int size)
        {
            Random rnd = new Random();
            return rnd.Next(0, size);
        }


        public static bool IsGroupAdmin(Message msg, bool IgnoreDev = false)
        {
            if (msg.Chat.Type == ChatType.Private) return false;
            if (msg.Chat.Type == ChatType.Channel) return false;
            return IsGroupAdmin(msg.From.Id, msg.Chat.Id, IgnoreDev);
        }

        public static bool IsGroupAdmin(CallbackQuery call, bool IgnoreDev = false)
        {
            if (call.Message.Chat.Type == ChatType.Private) return false;
            if (call.Message.Chat.Type == ChatType.Channel) return false;
            return IsGroupAdmin(call.Message.From.Id, call.Message.Chat.Id, IgnoreDev);
        }

        public static bool IsGroupAdmin(int userid, long chatid, bool IgnoreDev = false)
        {
            if (Constants.Dev.Contains(userid) && !IgnoreDev) return true;

            var admins = Bot.Api.GetChatAdministratorsAsync(chatid).Result;
            if (admins.Any(x => x.User.Id == userid)) return true;
            return false;
        }

        public static Player GetPlayer(long id)
        {
            using (var db = new SixNimmtDb())
            {
                return db.Players.FirstOrDefault(x => x.TelegramId == id);
            }
        }

        public static dynamic GetGroupOrPlayer(long id)
        {
            if (int.TryParse(id.ToString(), out int o))
            {
                return GetPlayer(o);
            }
            else
            {
                return GetGroup(id);
            }
        }

        public static Group GetGroup(long id)
        {
            using (var db = new SixNimmtDb())
            {
                return db.Groups.FirstOrDefault(x => x.GroupId == id);
            }
        }

        public static Group MakeDefaultGroup(Chat chat)
        {
            return new Group
            {
                GroupId = chat.Id,
                Name = chat.Title,
                Language = "English",
                CreatedBy = "Command",
                CreatedTime = DateTime.UtcNow,
                UserName = chat.Username,
                GroupLink = chat.Username == "" ? $"https://telegram.me/{chat.Username}" : null
            };
        }

        public static Player MakeDefaultPlayer(User user)
        {
            return new Player
            {
                Name = user.FirstName,
                TelegramId = user.Id,
                UserName = user.Username,
                Language = "English"
            };
        }

        public static List<SNCard> AllCards(this List<SNCard[]> deck)
        {
            return deck.SelectMany(c => c).Where(c => c != null).ToList();
        }

        public static Dictionary<string, Locale> ReadLanguageFiles()
        {
            var files = Directory.GetFiles(Constants.GetLangDirectory());
            var langs = new Dictionary<string, Locale>();
            try
            {
                foreach (var file in files)
                {
                    var lang = Path.GetFileNameWithoutExtension(file);
                    XDocument doc = XDocument.Load(file);
                    var loc = new Locale
                    {
                        Language = Path.GetFileNameWithoutExtension(file),
                        XMLFile = doc,
                        LanguageName = doc.Descendants("language").FirstOrDefault().Attribute("name").Value
                    };
                    langs.Add(lang, loc);
                }
            }
            catch { }
            return langs;
        }

        public static Locale ReadEnglish()
        {
            return new Locale
            {
                Language = "English",
                XMLFile = XDocument.Load(Path.Combine(Constants.GetLangDirectory(), "English.xml")),
                LanguageName = "English"
            };
        }

        public static string GetLanguage(long id)
        {
            using (var db = new SixNimmtDb())
            {
                Player p = null;
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                    p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                if (p != null && String.IsNullOrEmpty(p.Language))
                {
                    p.Language = "English";
                    db.SaveChanges();
                }
                return grp?.Language ?? p?.Language ?? "English";
            }
        }

        public static string GetLanguage(int id)
        {
            using (var db = new SixNimmtDb())
            {
                Player p = null;
                var grp = db.Groups.FirstOrDefault(x => x.GroupId == id);
                if (grp == null)
                    p = db.Players.FirstOrDefault(x => x.TelegramId == id);
                if (p != null && String.IsNullOrEmpty(p.Language))
                {
                    p.Language = "English";
                    db.SaveChanges();
                }
                return grp?.Language ?? p?.Language ?? "English";
            }
        }

        public static string GetTranslation(string key, string language, params object[] args)
        {
            try
            {
                var strings = Program.Langs[language].XMLFile.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key) ??
                              Program.English.XMLFile.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                if (strings != null)
                {
                    var values = strings.Descendants("value");
                    var choice = Helpers.RandomNum(values.Count());
                    var selected = values.ElementAt(choice).Value;

                    return String.Format(selected, args).Replace("\\n", Environment.NewLine);
                }
                else
                {
                    throw new Exception($"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}");
                }
            }
            catch (Exception e)
            {
                try
                {
                    //try the english string to be sure
                    var strings =
                        Program.English.XMLFile.Descendants("string").FirstOrDefault(x => x.Attribute("key")?.Value == key);
                    var values = strings?.Descendants("value");
                    if (values != null)
                    {
                        var choice = Helpers.RandomNum(values.Count());
                        var selected = values.ElementAt(choice).Value;
                        // ReSharper disable once AssignNullToNotNullAttribute
                        return String.Format(selected, args).Replace("\\n", Environment.NewLine);
                    }
                    else
                        throw new Exception("Cannot load english string for fallback");
                }
                catch
                {
                    throw new Exception(
                        $"Error getting string {key} with parameters {(args != null && args.Length > 0 ? args.Aggregate((a, b) => a + "," + b.ToString()) : "none")}",
                        e);
                }
            }
        }

        public static string GetTranslation(string key, XDocument file)
        {
            var strings = file.Descendants("string").FirstOrDefault(x => x.Attribute("key").Value == key);
            var values = strings.Descendants("value");
            return values.First().Value;
        }

        #region Language Files Helperss

        public static void UploadFile(string fileid, long id, string newFileCorrectName, int msgID)
        {
            var file = Bot.Api.GetFileAsync(fileid).Result;
            var path = Directory.CreateDirectory(Constants.GetLangDirectory(true));
            //var fileName = file.FilePath.Substring(file.FilePath.LastIndexOf("/") + 1);
            var uri = $"https://api.telegram.org/file/bot{Constants.GetBotToken("BotToken")}/{file.FilePath}";
            var newFilePath = Path.Combine(path.FullName, newFileCorrectName);
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri(uri), newFilePath);
            }


            //ok, we have the file.  Now we need to determine the language, scan it and the original file.
            var newFileErrors = new List<LanguageError>();
            //first, reload existing file to program
            Program.English = Helpers.ReadEnglish();
            Program.Langs = Helpers.ReadLanguageFiles();
            var langs = Program.Langs.Select(x => new LangFile(x.Key, x.Value.XMLFile));
            var master = Program.English;
            var newFile = new LangFile(newFilePath);

            //make sure it has a complete langnode
            CheckLanguageNode(newFile, newFileErrors);

            //test the length
            TestLength(newFile, newFileErrors);

            //check uniqueness
            var error = langs.FirstOrDefault(x =>
                    (x.FileName.ToLower() == newFile.FileName.ToLower() && x.Name != newFile.Name) //check for matching filename and mismatching name
                    || (x.Name == newFile.Name && (x.Base != newFile.Base || x.Variant != newFile.Variant)) //check for same name and mismatching base-variant
                    || (x.Base == newFile.Base && x.Variant == newFile.Variant && x.FileName != newFile.FileName) //check for same base-variant and mismatching filename
                                                                                                                  //if we want to have the possibility to rename the file, change previous line with FileName -> Name
            );
            if (error != null)
            {
                //problem....
                newFileErrors.Add(new LanguageError(newFile.FileName, "*Language Node*",
                    $"ERROR: The following file partially matches the same language node. Please check the file name, and the language name, base and variant. Aborting.\n\n*{error.FileName}.xml*\n_Name:_{error.Name}\n_Base:_{error.Base}\n_Variant:_{error.Variant}", ErrorLevel.FatalError));
            }

            //get the errors in it
            GetFileErrors(newFile, newFileErrors, master.XMLFile);

            //need to get the current file
            var curFile = langs.FirstOrDefault(x => x.Name == newFile.Name);
            var curFileErrors = new List<LanguageError>();

            if (curFile != null)
            {
                //test the length
                TestLength(curFile, curFileErrors);

                ////validate current file name / base / variants match
                //if (newFile.Base != lang.Base)
                //{
                //    newFileErrors.Add(new LanguageError(curFileName, "Language Node", $"Mismatched Base! {newFile.Base} - {lang.Base}", ErrorLevel.Error));
                //}
                //if (newFile.Variant != lang.Variant)
                //{
                //    newFileErrors.Add(new LanguageError(curFileName, "Language Node", $"Mismatched Variant! {newFile.Variant} - {lang.Variant}", ErrorLevel.Error));
                //}

                //get the errors in it
                GetFileErrors(curFile, curFileErrors, master.XMLFile);
            }

            //send the validation result
            Bot.Api.SendTextMessageAsync(id, OutputResult(newFile, newFileErrors, curFile, curFileErrors), parseMode: ParseMode.Markdown);
            Thread.Sleep(500);


            if (newFileErrors.All(x => x.Level != ErrorLevel.FatalError))
            {
                //load up each file and get the names
                var buttons = new[]
                {
                    InlineKeyboardButton.WithCallbackData($"New", $"upload|{id}|{newFile.FileName}"),
                    InlineKeyboardButton.WithCallbackData($"Old", $"upload|{id}|current")
                };
                var menu = new InlineKeyboardMarkup(buttons.ToArray());
                Bot.Api.SendTextMessageAsync(id, "Which file do you want to keep?", replyToMessageId: msgID,
                    replyMarkup: menu);
            }
            else
            {
                Bot.Api.SendTextMessageAsync(id, "Fatal errors present, cannot upload.", replyToMessageId: msgID);
            }
        }

        private static void CheckLanguageNode(LangFile langfile, List<LanguageError> errors)
        {

            if (String.IsNullOrWhiteSpace(langfile.Name))
                errors.Add(new LanguageError(langfile.FileName, "*Language Node*", "Language name is missing", ErrorLevel.FatalError));
            if (String.IsNullOrWhiteSpace(langfile.Base))
                errors.Add(new LanguageError(langfile.FileName, "*Language Node*", "Base is missing", ErrorLevel.FatalError));
            if (String.IsNullOrWhiteSpace(langfile.Variant))
                errors.Add(new LanguageError(langfile.FileName, "*Language Node*", "Variant is missing", ErrorLevel.FatalError));
        }

        private static void TestLength(LangFile file, List<LanguageError> fileErrors)
        {
            var test = $"setlang|-1001234567890|{file.Base ?? ""}|{file.Variant ?? ""}|v";
            var count = Encoding.UTF8.GetByteCount(test);
            if (count > 64)
                fileErrors.Add(new LanguageError(file.FileName, "*Language Node*", "Base and variant are too long.", ErrorLevel.FatalError));
        }

        private static void GetFileErrors(LangFile file, List<LanguageError> fileErrors, XDocument master)
        {
            var masterStrings = master.Descendants("string");

            foreach (var str in masterStrings)
            {
                var key = str.Attribute("key").Value;
                var isgif = str.Attributes().Any(x => x.Name == "isgif");
                //get the english string
                //get the locale values
                var masterString = GetTranslation(key, master);
                var values = file.Doc.Descendants("string")
                        .FirstOrDefault(x => x.Attribute("key").Value == key)?
                        .Descendants("value");
                if (values == null)
                {
                    fileErrors.Add(new LanguageError(file.FileName, key, $"Values missing"));
                    continue;
                }
                //check master string for {#} values
                int vars = 0;
                for (int i = 0; i < 5; i++)
                    if (masterString.Contains("{" + i + "}"))
                        vars = i + 1;

                foreach (var value in values)
                {
                    for (int i = 0; i <= 5 - 1; i++)
                    {
                        if (!value.Value.Contains("{" + i + "}") && vars - 1 >= i)
                        {
                            //missing a value....
                            fileErrors.Add(new LanguageError(file.FileName, key, "Missing {" + i + "}", ErrorLevel.Error));
                        }
                        else if (value.Value.Contains("{" + i + "}") && vars - 1 < i)
                        {
                            fileErrors.Add(new LanguageError(file.FileName, key, "Extra {" + i + "}", ErrorLevel.Error));
                        }
                    }

                    if (isgif && value.Value.Length > 200)
                    {
                        fileErrors.Add(new LanguageError(file.FileName, key, "GIF string length cannot exceed 200 characters", ErrorLevel.FatalError));
                    }
                }
            }
        }

        private static string OutputResult(LangFile newFile, List<LanguageError> newFileErrors, LangFile curFile, List<LanguageError> curFileErrors)
        {
            var result = $"NEW FILE\n*{newFile.FileName}.xml - ({newFile.Name ?? ""})*" + Environment.NewLine;

            if (newFileErrors.Any(x => x.Level == ErrorLevel.Error))
            {
                result += "_Errors:_\n";
                result = newFileErrors.Where(x => x.Level == ErrorLevel.Error).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n{fileError.Message}\n\n");
            }
            if (newFileErrors.Any(x => x.Level == ErrorLevel.MissingString))
            {
                result += "_Missing Values:_\n";
                result = newFileErrors.Where(x => x.Level == ErrorLevel.MissingString).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n");
            }
            if (newFileErrors.Any(x => x.Level == ErrorLevel.FatalError))
            {
                result += "\n*Fatal errors:*\n";
                result = newFileErrors.Where(x => x.Level == ErrorLevel.FatalError).Aggregate(result, (current, fileError) => current + $"{fileError.Key}\n{fileError.Message}\n\n");
            }
            if (newFileErrors.Count == 0)
            {
                result += "_No errors_\n";
            }

            if (curFile != null)
            {
                result += "\n\n";
                result += $"OLD FILE (Last updated: {curFile.LatestUpdate.ToString("MMM dd")})\n*{curFile.FileName}.xml - ({curFile.Name})*\n";
                result +=
                    $"Errors: {curFileErrors.Count(x => x.Level == ErrorLevel.Error)}\nMissing strings: {curFileErrors.Count(x => x.Level == ErrorLevel.MissingString)}";
            }
            else
            {
                result += "\n\n*No old file, this is a new language*";
                result += "\nPlease double check the filename, and the language name, base and variant, as you won't be able to change them.";
                result += $"\n_Name:_ {newFile.Name ?? ""}";
                result += $"\n_Base:_ {newFile.Base ?? ""}";
                if (!Directory.GetFiles(Constants.GetLangDirectory(), "*.xml").Select(x => new LangFile(x)).Any(x => x.Base == newFile.Base))
                    result += " *(NEW)*";
                result += $"\n_Variant:_ {newFile.Variant ?? ""}";
            }

            return result;
        }

        public static void UseNewLanguageFile(string fileName, long id, int msgId)
        {
            var msg = "Moving file to production..\n";
            msg += "Checking paths for duplicate language file...\n";
            Bot.Api.EditMessageTextAsync(id, msgId, msg);
            fileName += ".xml";
            var tempPath = Constants.GetLangDirectory(true);
            var langPath = Constants.GetLangDirectory();
            var newFilePath = Path.Combine(tempPath, fileName);
            var copyToPath = Path.Combine(langPath, fileName);

            //get the new files language
            var doc = XDocument.Load(newFilePath);

            var newFileLang = new
            {
                Name = doc.Descendants("language").First().Attribute("name").Value,
                Base = doc.Descendants("language").First().Attribute("base").Value,
                Variant = doc.Descendants("language").First().Attribute("variant").Value
            };


            //check for existing file
            var langs = Directory.GetFiles(langPath).Select(x => new LangFile(x)).ToList();
            var lang = langs.FirstOrDefault(x => x.Name == newFileLang.Name);
            if (lang != null)
            {
                //            
            }
            else
            {
                lang = langs.FirstOrDefault(x => x.Base == newFileLang.Base && x.Variant == newFileLang.Variant && x.Name != newFileLang.Name);
                if (lang != null)
                {
                    msg += $"Found duplicate language (matching base and variant) with filename {Path.GetFileNameWithoutExtension(lang.FileName)}\n";
                    msg += "Aborting!";
                    Bot.Api.EditMessageTextAsync(id, msgId, msg);
                    return;
                }
            }


            System.IO.File.Copy(newFilePath, copyToPath, true);
            msg += "File copied to bot\n";

            msg += "\n<b>Operation complete.</b>";

            Bot.Api.EditMessageTextAsync(id, msgId, msg, parseMode: ParseMode.Html);
        }

        public class LangFile
        {
            public string Name { get; set; }
            public string Base { get; set; }
            public string Variant { get; set; }
            public string FileName { get; set; }
            // public string FilePath { get; set; }
            public XDocument Doc { get; set; }
            public DateTime LatestUpdate { get; }

            public LangFile(string path)
            {
                Doc = XDocument.Load(path);
                Name = Doc.Descendants("language").First().Attribute("name")?.Value;
                Base = Doc.Descendants("language").First().Attribute("base")?.Value;
                Variant = Doc.Descendants("language").First().Attribute("variant")?.Value;
                // FilePath = path;
                FileName = Path.GetFileNameWithoutExtension(path);
                LatestUpdate = System.IO.File.GetLastWriteTimeUtc(path);
            }

            public LangFile(string xmlName, XDocument xmlDoc)
            {
                Doc = xmlDoc;
                Name = Doc.Descendants("language").First().Attribute("name")?.Value;
                Base = Doc.Descendants("language").First().Attribute("base")?.Value;
                Variant = Doc.Descendants("language").First().Attribute("variant")?.Value;
                // FilePath = path;
                FileName = xmlName;
                LatestUpdate = System.IO.File.GetLastWriteTimeUtc(Path.Combine(Constants.GetLangDirectory(true), xmlName, ".xml"));
            }
        }

        public static InlineKeyboardMarkup GenerateStartMe(int id)
        {
            var row = new List<InlineKeyboardButton>();
            var rows = new List<InlineKeyboardButton[]>();
            row.Add(InlineKeyboardButton.WithUrl(GetTranslation("StartMe", GetLanguage(id)), $"https://telegram.me/{Bot.Me.Username}"));
            rows.Add(row.ToArray());
            return new InlineKeyboardMarkup(rows.ToArray());
        }

        public static Chart CreateChart(DataSet source, string title, string xName, string yName, SeriesChartType chartType, int width, int height)
        {
            Chart chart = new Chart();
            chart.DataSource = source.Tables[0];
            chart.Width = width;
            chart.Height = height;
            chart.Titles.Add(title);
            chart.Titles[0].Font = new Font("Tahoma", 16.0f);
            //create serie...
            Series serie1 = new Series();
            switch (chartType)
            {
                case SeriesChartType.Bar:
                    serie1.ChartType = chartType;
                    serie1.XValueMember = xName;
                    serie1.YValueMembers = yName;
                    serie1.IsValueShownAsLabel = true;
                    break;
                case SeriesChartType.Spline:
                    serie1.Name = "Serie1";
                    serie1.Color = Color.DarkSlateBlue;
                    serie1.BorderColor = Color.Black;
                    serie1.ChartType = chartType;
                    serie1.BorderDashStyle = ChartDashStyle.Solid;
                    serie1.BorderWidth = 5;
                    serie1.ShadowColor = Color.FromArgb(128, 128, 128);
                    serie1.ShadowOffset = 1;
                    serie1.IsValueShownAsLabel = true;
                    serie1.XValueMember = xName;
                    serie1.YValueMembers = yName;
                    serie1.Font = new Font("Tahoma", 10.0f);
                    serie1.BackSecondaryColor = Color.FromArgb(0, 102, 153);
                    serie1.LabelForeColor = Color.FromArgb(100, 100, 100);
                    break;
            }
            
            chart.Series.Add(serie1);
            //create chartareas...
            ChartArea ca = new ChartArea();
            ca.Name = "Game Growth";

            ca.BackColor = Color.White;
            ca.BorderColor = Color.FromArgb(26, 59, 105);
            ca.BorderWidth = 0;
            ca.BorderDashStyle = ChartDashStyle.Dash;
            ca.AxisX = new Axis();
            ca.AxisX.Title = xName;
            ca.AxisX.TitleAlignment = StringAlignment.Center;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisX.MajorGrid.LineWidth = 1;
            ca.AxisY = new Axis();
            chart.ChartAreas.Add(ca);
            ca.AxisY.Title = yName;
            ca.AxisY.TitleAlignment = StringAlignment.Center;
            ca.AxisY.TextOrientation = TextOrientation.Rotated270;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineWidth = 1;
            if (chartType == SeriesChartType.Bar)
            {
                ca.AxisX.Interval = 1;
                ca.AxisY.TextOrientation = TextOrientation.Horizontal;
            }

            //databind...
            chart.DataBind();
            //save result...
            //chart.SaveImage(@"c:\myChart.png", ChartImageFormat.Png);
            return chart;
        }

        public class LanguageError
        {
            public string File { get; set; }
            public string Key { get; set; }
            public string Message { get; set; }
            public ErrorLevel Level { get; set; }

            public LanguageError(string file, string key, string message, ErrorLevel level = ErrorLevel.MissingString)
            {
                File = file;
                Key = key;
                Message = message;
                Level = level;
            }
        }

        public enum ErrorLevel
        {
            DuplicatedString, MissingString, Error, FatalError
        }
        #endregion
    }


}
