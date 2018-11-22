using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using VideoLibrary;
using YoutubeExtractor;

//-s -t -b

//-s HMly6D0nrgA -t "C:\Users\ozan.bayram\Google Drive" -b "C:\Sources\MP3Head\MP3Head.App\bin\Release\netcoreapp2.1\win-x86\keys.txt"

/// <summary>
/// https://github.com/flagbug/YoutubeExtractor
/// 
/// https://github.com/hig-dev/libvideo
/// 
/// https://github.com/flagbug/YoutubeExtractor
/// https://github.com/i3arnon/libvideo
/// https://github.com/hig-dev/libvideo
/// https://www.nuget.org/packages/VideoLibraryHIG/
/// </summary>
namespace MP3Head.App
{
    class Program
    {
        const string HelpSwitch1 = "-h";
        const string SingleSwitch1 = "-s";
        const string BulkSwitch1 = "-b";
        const string TargetSwitch1 = "-t";

        const string HelpSwitch2 = "/?";
        const string SingleSwitch2 = "/s";
        const string BulkSwitch2 = "/b";
        const string TargetSwitch2 = "/t";

        const string DefaultTargetFolder = @"c:\MP3Downloads\";

        static bool IsHelpSwitchSet = false;
        static bool IsSingleSwitchSet = false;
        static bool IsBulkSwitchSet = false;
        static bool IsTargetSwitchSet = false;

        //static string newFullYoutubeUrl = string.Empty;
        static string VideoKey = string.Empty;
        static string VideoKeySourceFileName = string.Empty;
        static string NewTargetPath = string.Empty;

        /// <summary>
        /// /? yardım bilgilerini gösterir
        /// -s {youtube video key} ile tek dosya indirme 
        /// -b "{text source file}" içindeki youtube video key listesinin toplu olarak indirilmesi.
        /// -t "{hedef klasor}" indirilenleri varsayılan hedef "c:\MP3Downloads\" klasörüden farklı bir yere kaydetme
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args) //CommandLineToArgvW for more information
        {
            string targetFolder = string.Empty;
            targetFolder = DefaultTargetFolder;

            Result invalidArgumentsResult = CheckInvalidArguments(args);
            bool hasInvalidArguments = invalidArgumentsResult.HasError;

            if (args == null || args.Length == 0 || hasInvalidArguments)
            {
                //throw new ArgumentNullException(nameof(args));
                DisplayResult(invalidArgumentsResult);
                Environment.Exit(-1);
            }

            Result setSwitchResult = SetSwitchValues(args);

            if (setSwitchResult.HasError)
            {
                DisplayResult(setSwitchResult);
                Environment.Exit(-2);
            }

            if (IsHelpSwitchSet)
            {
                DisplayHelp();
                Environment.Exit(0);
            }

            if (IsTargetSwitchSet && !(IsSingleSwitchSet || IsBulkSwitchSet))
            {
                Result myResult = new Result();
                myResult.HasError = true;
                myResult.Descriprion = $"Yeni Hedef klasör anahtarı diğer anahtarlar olmadan kullanılamaz. \r\n";
                DisplayResult(myResult);
                Environment.Exit(-3);
            }

            if (IsTargetSwitchSet)
            {
                targetFolder = SetTargetFolder();
            }
            else
            {
                CheckDefaultTargetFolder();
            }

            if (IsSingleSwitchSet)
            {
                Console.WriteLine($"İndirme başladı: {VideoKey}\r\n");
                string fileName = string.Empty;
                DownloadMusic(VideoKey, targetFolder, out fileName);
                Console.WriteLine($"İndirme tamamlandı! {fileName} dosyası {targetFolder} altına kaydedildi\r\n");
            }

            if (IsBulkSwitchSet)
            {
                DownloadBulkMusic(VideoKeySourceFileName, targetFolder);
            }

            Console.WriteLine("Çıkmak için enter tuşuna basınız!\r\n");
            Console.ReadLine();
        }

        private static void CheckDefaultTargetFolder()
        {
            Console.Write($"Varsayılan Hedef klasör '{DefaultTargetFolder}' kontrol ediliyor");

            if (!Directory.Exists(DefaultTargetFolder))
            {
                Directory.CreateDirectory(DefaultTargetFolder);
                Result myResult = new Result();
                myResult.HasError = true;
                myResult.Descriprion = $"Varsayılan Hedef klasör '{DefaultTargetFolder}' bulunamadı, oluşturuluyor. \r\n ";
                //DisplayResult(myResult);

                Console.WriteLine(myResult.Descriprion);
            }
            else
            {
                Console.Write("[OK]\r\n");
            }
        }

        private static string SetTargetFolder()
        {
            string targetFolder = DefaultTargetFolder;

            if (!string.IsNullOrEmpty(NewTargetPath))
            {
                //if no trailing slashes add one to end
                string trailingSlash = NewTargetPath.Substring(NewTargetPath.Length - 1, 1);

                targetFolder = @NewTargetPath;

                if (string.IsNullOrEmpty(trailingSlash) || trailingSlash != @"\")
                {
                    targetFolder += @"\";
                }

                if (!Directory.Exists(targetFolder))
                {
                    Result myResult = new Result();
                    myResult.HasError = true;
                    myResult.Descriprion = $"Hedef klasör '{targetFolder}' bulunamadı, varsayılan klasör kullanılacak '{DefaultTargetFolder}'  \r\n ";
                    DisplayResult(myResult, false);
                    targetFolder = DefaultTargetFolder;
                }
            }

            return targetFolder;
        }

        static void DownloadMusic(string VideoKey, string TargetFolder, out string FileName)
        {
            FileName = string.Empty;

            // youtube link
            string link = string.Empty;
            link = $"https://www.youtube.com/watch?v={VideoKey}";

            try
            {
                var youtube = YouTube.Default;
                var video = youtube.GetAllVideos(link);
                var audio = video.Where(e => e.AudioFormat == AudioFormat.Aac && e.AdaptiveKind == AdaptiveKind.Audio).ToList();

                if (audio.Count > 0)
                {
                    //FileExtension: ".mp4"
                    //Title: "Irma - I know (Clip officiel) - YouTube"
                    //FullName: "Irma - I know (Clip officiel) - YouTube.mp4"

                    string fileName = audio[0].FullName;
                    string file = $"{TargetFolder}{fileName}";

                    bool fileExists = System.IO.File.Exists(file);

                    if (fileExists)
                    {
                        //adinin sonuna alt tire ekle
                        file += "_";
                    }

                    byte[] bytes = audio[0].GetBytes();
                    File.WriteAllBytes(file, bytes);

                    FileName = fileName;
                }
            }
            catch (HttpRequestException)
            {
                Result myResult = new Result();
                myResult.HasError = true;
                myResult.Descriprion = "A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond ---> System.Net.Sockets.SocketException: A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond";
                DisplayResult(myResult);
            }
        }

        static void DownloadBulkMusic(string VideoKeySourceFileName, string TargetFolder)
        {
            Console.WriteLine($"Toplu İndirme başladı: {VideoKeySourceFileName}\r\n");

            List<string> keysToDownload = new List<string>();
            string[] lines;
            string path;

            //determine if file name contains folder informatin
            string directoryName = Path.GetDirectoryName(VideoKeySourceFileName);
            if (string.IsNullOrEmpty(directoryName)) //no path
            {
                string currentDirectory = System.IO.Directory.GetCurrentDirectory();
                path = Path.Combine(currentDirectory, VideoKeySourceFileName);
                Console.WriteLine($"Toplu indirme dosyası uygulama klasöründen kullanılıyor.");
            }
            else
            {
                path = VideoKeySourceFileName;
            }

            Console.WriteLine($"Toplu indirme dosyası tam yolu: {path}");

            bool fileExists = System.IO.File.Exists(@path);

            if (fileExists)
            {
                lines = System.IO.File.ReadAllLines(@VideoKeySourceFileName);
            }
            else
            {
                Console.WriteLine($"Toplu indirme dosyası bulunamadı {path}");
                return;
            }

            //read keys
            foreach (string line in lines)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    keysToDownload.Add(line);
                }
            }

            int fileCount = 0;
            fileCount = keysToDownload.Count;

            if (fileCount == 0)
            {
                Console.Write($"İndirilecek dosya yok. İndirme işlemi durduruldu. \r\n");
                return;
            }

            Console.WriteLine($"Toplu indirme dosyası içindeki dosya sayısı: {fileCount} adet.");
            Console.WriteLine($"İndirme hedef klasörü: {TargetFolder}");
            Console.Write($"İndirme işlemine devam edilsin mi? [E/H]");
            
            ConsoleKey key;// = keyInfo.Key;

            do
            {
                key = Console.ReadKey(true).Key;   // true is intercept key (dont show), false is show
                Console.WriteLine("");
                if (key != ConsoleKey.E && key != ConsoleKey.H)
                {
                    Console.Write("Geçersiz cevap, Lütfen Evet için E, Hayır için H tuşlayınız");
                }
            } while (key != ConsoleKey.E && key != ConsoleKey.H);


            if (key == ConsoleKey.H || key == ConsoleKey.N)
            {
                Console.Write($"İndirme işlemi durduruldu. \r\n");
                return;
            }

            //download keys
            foreach (var videoKey in keysToDownload)
            {
                string targetFolder = TargetFolder;
                string fileName = string.Empty;

                Console.WriteLine($"İndirme başladı: {videoKey}");
                DownloadMusic(videoKey, targetFolder, out fileName);
                Console.WriteLine($"İndirme tamamlandı! {fileName} dosyası {targetFolder} altına kaydedildi");
            }

            Console.WriteLine($"Toplu İndirme tamamlandı: {VideoKeySourceFileName}\r\n");
        }

        static Result CheckInvalidArguments(string[] args)
        {
            Result myResult;
            string message = string.Empty;

            if (args.Length > 0)
            {
                const string validSwitches = "-h -s -b -t /? /s /b /t";
                bool hasInvalidSwitches = false;
                string invalidSwitches = string.Empty;

                for (int i = 0; i < args.Length; i++)
                {
                    string argument = args[i];

                    bool isEven = i % 2 == 0;//used to determine the argument is a switch argument or not

                    if (isEven && argument.Length <= 2 && argument.Length > 0) //if argument's length is 1 or 2 it's a switch
                    {
                        bool isItemValid = false;
                        isItemValid = validSwitches.Contains(argument);

                        //if item is not contained in validArguments it's INVALID
                        if (!isItemValid)
                        {
                            hasInvalidSwitches = true;
                            invalidSwitches += $"{argument} ";
                        }
                    }
                    else //if argument's length is 2 it's not a switch, it's a value
                    {
                        //do nothing
                    }
                }

                if (hasInvalidSwitches)
                {
                    message = $"Geçerli olmayan anahtar(lar) algılandı. Hatalı anahtarlar: {invalidSwitches}\r\n";
                }

                myResult = new Result();
                myResult.HasError = hasInvalidSwitches;
                myResult.Descriprion = message;
            }
            else
            {
                message = $"Eksik parametre. İşlem için gerekli parametre(ler) eklenmeli.\r\n";

                myResult = new Result();
                myResult.HasError = true;
                myResult.Descriprion = message;
            }

            return myResult;
        }

        static Result SetSwitchValues(string[] args)
        {
            Result myResult;

            bool hasMissingValue = false;
            string errors = string.Empty;
            string message = string.Empty;

            if (args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    string argument = args[i];

                    if (argument.Length <= 2 && argument.Length > 0) //if argument's length is 1 or 2 it's a switch
                    {
                        if (argument == HelpSwitch1 /*"-h"*/ || argument == HelpSwitch2 /*"/?"*/)
                        {
                            IsHelpSwitchSet = true;
                        }

                        if (argument == SingleSwitch1 /*"-s"*/ || argument == SingleSwitch2 /*"/s"*/)
                        {
                            IsSingleSwitchSet = true;

                            string parameterValue = string.Empty;

                            //claim switch related parameter value
                            int nextIndex = i + 1;
                            if (nextIndex < args.Length && args[nextIndex] != null && args[nextIndex].Length > 2)
                            {
                                VideoKey = args[nextIndex];
                            }
                            else
                            {
                                hasMissingValue = true;
                                IsSingleSwitchSet = false;
                                errors += "-s için parametre değeri yok ya da eksik\r\n";
                            }
                        }

                        if (argument == BulkSwitch1 /*"-b"*/ || argument == BulkSwitch2 /*"/b"*/)
                        {
                            IsBulkSwitchSet = true;

                            //claim switch related parameter value
                            int nextIndex = i + 1;
                            if (nextIndex < args.Length && args[nextIndex] != null && args[nextIndex].Length > 2)
                            {
                                VideoKeySourceFileName = args[nextIndex];
                            }
                            else
                            {
                                hasMissingValue = true;
                                IsBulkSwitchSet = false;
                                errors += "-b için parametre değeri yok ya da eksik\r\n";
                            }
                        }

                        if (argument == TargetSwitch1 /*"-t"*/ || argument == TargetSwitch2 /*"/t"*/)
                        {
                            IsTargetSwitchSet = true;

                            //claim switch related parameter value
                            int nextIndex = i + 1;
                            if (nextIndex < args.Length && args[nextIndex] != null && args[nextIndex].Length > 2)
                            {
                                NewTargetPath = args[nextIndex];
                            }
                            else
                            {
                                hasMissingValue = true;
                                IsTargetSwitchSet = false;
                                errors += "-t için parametre değeri yok ya da eksik. Geçerli bir klasör yazınız. \r\n";
                            }
                        }
                    }
                    else //if argument's length is 2 it's not a switch, it's a value
                    {
                        //do nothing
                    }
                }

                myResult = new Result();
                myResult.HasError = hasMissingValue;

                if (hasMissingValue)
                {
                    message = $"Parametre anahtar(lar)ı için eksik ya da hatalı değerler algılandı.\r\nHatalar:\r\n {errors}\r\n";
                }

                myResult.Descriprion = message;
            }
            else
            {
                message = $"Eksik parametre. İşlem için gerekli parametre(ler) eklenmeli.\r\n";
                myResult = new Result();
                myResult.HasError = true;
            }

            return myResult;
        }

        static void DisplayResult(Result ResultToDisplay, bool ShowHelp = true)
        {
            Console.WriteLine(ResultToDisplay.Descriprion);

            if (ShowHelp)
            {
                DisplayHelp();
            }
        }

        static void DisplayHelp()
        {
            Console.WriteLine("Uygulama yardımı");
            Console.WriteLine("/? bu yardımı gösterir");
            Console.WriteLine("-s {youtube video key} ile tek dosya indirme");
            Console.WriteLine("-b {text source file} içindeki youtube video key listesinin toplu olarak indirilmesi. (Klasör belirtilmezse dosya uygulama klasöründe aranır.)");
            Console.WriteLine("-t {hedef klasor} indirilenleri varsayılan hedef \"c:\\MP3Downloads\\\" klasörüden farklı bir yere kaydetme. (Tek başına kullanılmaz. -s ya da -b anahtarları ile birlikte kullanılmalıdır.)");
            Console.WriteLine("");
            Console.WriteLine("Çıkmak için enter tuşuna basınız!");
            //charArt();
            Console.ReadLine();
        }

        static void charArt()
        {
            Console.WriteLine("                                                                                                ");
            Console.WriteLine("         .                                                                                      ");
            Console.WriteLine("         #..:                                                                                   ");
            Console.WriteLine("          ,. @                                                                                  ");
            Console.WriteLine("             ,,                                                                                 ");
            Console.WriteLine("            `.#                                        `                                        ");
            Console.WriteLine("      ,  .  ` '                                     `  +  `                                     ");
            Console.WriteLine("      @# #@ ` ,                                      ' '...                                     ");
            Console.WriteLine("      @ @# +;  .                                      :..,                                      ");
            Console.WriteLine("      @  @  ,  ;                                   `:'.'.+:,                                    ");
            Console.WriteLine("      @     `  +         `                           ..'.'                      ``              ");
            Console.WriteLine("     ',.........:;     '+++`         '++              ',+  ++.                `++++             ");
            Console.WriteLine("     '`          +     ++;,          '++              :''  ++.                '++,,             ");
            Console.WriteLine("     '`          +`    ++            '++              `+,  ++.                ++'               ");
            Console.WriteLine("     '`          .+  ,'++''  :+++'   '++`+++    ''`++`:''  ++.  '''  .++++.  '+++'` .++++.      ");
            Console.WriteLine("     ',           +  :+++++ +++++++  '+++++++   +++++ ;++  ++. +++  `++++++. +++++``++++++.     ");
            Console.WriteLine("      +          `':   ++   '+   ++  '++,  +++  +++ : ;++  ++.'++   '+:  ++;  ++;  '+;  ++'     ");
            Console.WriteLine("      ;'        ;+;    ++       `++  '++   ,++  ++`   ;++  ++'++        `++'  ++'      `'++     ");
            Console.WriteLine("       ;+`      ;,     ++    '+++++  '++   `++  ++    ;++  +++++,    :+++++'  ++'   :++++++     ");
            Console.WriteLine("        ;,   `..',     ++   ++'. ++  '++   .++  ++    ;++  +++:++   +++,`'+'  ++'  '++,`'+'     ");
            Console.WriteLine("        ;,   ''';      ++  .++   ++  '++   ;++  ++    ;++  ++, ++;  ++`  '+'  ++'  ++`  ++'     ");
            Console.WriteLine("        ;,   '`        ++  .++  '++  '+++ ,++,  ++    ;++  ++. `++  ++; `++'  ++'  ++' `+++     ");
            Console.WriteLine("        ;';;;+`        ++   +++++++, '+;+++++   ++    ;++  ++.  ++' :++++:++  ++'  :++++,++     ");
            Console.WriteLine("        `,,,,,         ..    :;, ... ... ,;,    ..    ...  ..`   ..  .;:` ..  ...   .;:` ..     ");
            Console.WriteLine("                                                                                                ");
            Console.WriteLine("                                                                                                ");
        }
    }
}

class Result
{
    public bool HasError { get; set; }
    public string Descriprion { get; set; }
}