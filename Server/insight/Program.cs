using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NetworkCommsDotNet;

namespace norsu.ass.Server
{
    static class Program
    {
        private static bool _shuttingDown;
        private static int _logStart = 1;
        private static int _cmdLocation = 0;
        static void Main(string[] args)
        {
            SetConsoleCtrlHandler(ConsoleCloseHandler, true);
            
            awooo.IsRunning = true;
            awooo.Context = SynchronizationContext.Current;
            
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            Console.WindowHeight = 41;
            Console.BufferHeight = 41;
            Console.BufferWidth = 107;
            Console.WindowWidth = 107;
            
            awooo.Context = SynchronizationContext.Current;
            awooo.IsRunning = true;
            
            AddLog("Starting server...");

            if (Models.Office.Cache.Count == 0)
            {
                new Models.Office()
                {
                    LongName = "Sample Office Name",
                    ShortName = "SON",
                }.Save();
            }
            
            Network.Server.Instance.Start();
            
            AddLog("Server started");
            PrintScreen();
            var cmd = args.FirstOrDefault()??"db";
            
            while (cmd!=null)
            {
                if(_shuttingDown) return;
                
                switch(cmd.ToLower())
                {
                    case "database":
                    case "db":
                    case "summary":
                        PrintDatabaseCounts();
                        break;
                    case "exit":
                    case "quit":
                    case "shutdown":
                        Shutdown();
                        return;
                    case "help":
                    case "h":
                    case "-h":
                    case "commands":
                    case "tabang":
                        PrintHelp();
                        break;
                    case "about":
                    case "team":
                        PrintAbout();
                        break;
                    case "cls":
                    case "clear":
                        lock (logsync)
                            Logs.Clear();
                        break;
                    case "debug":
                        ShowLog = true;
                        NetworkComms.EnableLogging();
                        break;
                    case "version":
                        PrintTitle();
                        break;
                    default:
                        AddLog("Invalid Command! Type HELP for the list of commands.");
                        break;
                }
                PrintScreen();
                PrintCommandPrompt();
                cmd = Console.ReadLine();
            }

            Shutdown();

        }

        private static void PrintCommandPrompt()
        {
            Console.SetCursorPosition(46, 38);
        }

        private static void PrintScreen()
        {
            Console.Clear();
            Console.SetCursorPosition(0,0);
            Console.WriteLine(@"
  ╔═════════════════════════════════════════════════════════════════════════════════════════════════════╗
  ║  _  _  _____  ____  ___  __  __       ___      __    ___  ___    ___  ____  ____  _  _  ____  ____  ║
  ║ ( \( )/  _  \(  _ \/ __)(  )(  ) __  / __)    /__\  / __)/ __)  / __)( ___)(  _ \( \/ )( ___)(  _ \ ║
  ║ |    || (_) ||    /\__ \| (__) |(__)( (_-.   /(__)\ \__ \\__ \  \__ \| __) |    / \  / | __) |    / ║
  ║ (_)\_)\_____/(_|\_)(___/\______)     \___/  (__)(__)(___/(___/  (___/(____)(_)\_)  \/  (____)(_)\_) ║
  ╠═════════════════════════════════════════════════════════════════════════════════════════════════════╣
  ║ NEGROS ORIENTAL STATE UNIVERSITY - GUIHULNGAN CAMPUS AUTOMATED SUGGESTION SYSTEM SERVER VERSION 0.9 ║
  ╟────────────────────────────────┬────────────────────────────────────────────────────────────────────╢
  ║  ░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░  │                                                                    ║
  ║ ░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░ │                                                                    ║
  ║ ▒▒▒▓░░░▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ │                                                                    ║
  ║ ▒▒▓░░░░░▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ │                                                                    ║
  ║ ▒▒▓░░░░░▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ │                                                                    ║
  ║ ▒▒▒▓░░░▓▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒ │                                                                    ║
  ║ ░▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░ │                                                                    ║
  ║  ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒░  │                                                                    ║
  ║  ▓██████████████████████████▓  │                                                                    ║
  ║ ▓████████████████████████████▓ │                                                                    ║
  ║ ███▓░░░▓██████████████████████ │                                                                    ║
  ║ ██▓░░░░░▓███████████░█████████ │                                                                    ║
  ║ ██▓░░░░░▓██████████▓▓▓████████ │                                                                    ║
  ║ ███▓░░░▓██████████░▓▓▓░███████ │                                                                    ║
  ║ █████████████████░▓▓▓▓▓░██████ │                                                                    ║
  ║ ▓███████████████░▓▓▓▓▓▓▓░█████ │                                                                    ║
  ║  ▓█████████▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓ │                                                                    ║
  ║  ░▒▒▒▒▒▒▒▒▒░▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓░  │                                                                    ║
  ║ ░▒▒▒▒▒▒▒▒▒▒▒▒░▓▓▓▓▓▓▓▓▓▓▓▓▓░▒░ │                                                                    ║
  ║ ▒▒▒▒▒▒▒▒▒▒▒▒▒▒░▓▓▓▓▓▓▓▓▓▓▓░▒▒▒ │                                                                    ║
  ║ ▒▒▒▓░░░▓▒▒▒▒▒▒▒░▓▓▓▓▓▓▓▓▓░▒▒▒▒ │                                                                    ║
  ║ ▒▒▓░░░░░░▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒ │                                                                    ║
  ║ ▒▒░░░░░░▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▒▒▒▒▒ │                                                                    ║
  ║ ▒▒▒▓░░░▓▒▒▒▒▒▒▒▓▓▓▓▓▓▓▓▓▓▓▒▒▒▒ │                                                                    ║
  ║ ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▓▓░▓▓▓▓▓▓▒▒▒ │                                                                    ║
  ║ ░▒▒▒▒▒▒▒▒▒▒▒▒▒▓▓▓▓▒▒▒▒▒▓▓▓▓▒▒░ │                                                                    ║
  ║  ░▒▒▒▒▒▒▒▒▒▒▒▓▓▓▒▒▒▒▒▒▒▒░▓▓▓░  │                                                                    ║
  ║              ▓░           ░▓   │                                                                    ║
  ╟────────────────────────────────┼────────────────────────────────────────────────────────────────────╢
  ║ COMPUTER  SCIENCE  CLASS  2018 │ COMMAND>                                                           ║
  ╚════════════════════════════════╧════════════════════════════════════════════════════════════════════╝");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.SetCursorPosition(4,7);
            Console.Write("N");
            Console.SetCursorPosition(11, 7);
            Console.Write("OR");
            Console.SetCursorPosition(20, 7);
            Console.Write("S");
            Console.SetCursorPosition(26, 7);
            Console.Write("U");
            Console.ForegroundColor = ConsoleColor.White;
            string[] logs = null;
            lock (logsync)
                logs = Logs.ToArray();
            for (int i = 0; i < logs.Length; i++)
            {
                Console.SetCursorPosition(37, 9 + i);
                var log = logs[i]+"";
                if (log.Length > 66)
                    log = log.Substring(0, 66);
                Console.Write(log);
            }
        }

        private static Queue<string> Logs = new Queue<string>(28);
        
        public static bool ShowLog { get; set; }

        private static void AddLog(string message="")
        {
            message = message + "";
            message = message.Replace(Environment.NewLine, "");
            if (message.Length < 66)
                lock(logsync)
                Logs.Enqueue(message);
            else
            {
                for (int i = 0; i < message.Length; i++)
                {
                    if (i == 0)
                    {
                        lock (logsync)
                            Logs.Enqueue(message.Substring(i, 66));
                        i += 66;
                    }
                    else
                    {
                        if (i + 64 >= message.Length)
                        {
                            lock (logsync)
                                Logs.Enqueue("> " + message.Substring(i, 64));
                            i += 64;
                        }
                        else
                        {
                            lock (logsync)
                                Logs.Enqueue("> " + message.Substring(i));
                            break;
                        }
                        
                    }
                    
                }
                
            }

            while (Logs.Count > 28)
                lock (logsync)
                    Logs.Dequeue();
            
            
        }

        private static bool _printStarted;
        private static DateTime _lastLog = DateTime.Now;
        public static void Log(string message)
        {
            _lastLog = DateTime.Now;
            AddLog(message);

            if (_printStarted) return;
            _printStarted = true;

            Task.Factory.StartNew(async () =>
            {
                while ((DateTime.Now - _lastLog).TotalMilliseconds < 1111)
                {
                    await TaskEx.Delay(100);
                }
                
                PrintScreen();
                _lastLog = DateTime.Now;
                _printStarted = false;
                PrintCommandPrompt();
            });
                
        }
        private static object logsync = new object();
        public static async void Shutdown()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;

            try
            {
                if (Directory.Exists("Temp"))
                {
                    var files = Directory.GetFiles("Temp");
                    foreach (var file in files)
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception e)
                        {
                            //
                        }
                        
                    }
                }
            }
            catch (Exception e)
            {
                //
            }

            
            AddLog("Server is shutting down...");
            Network.Server.Instance.Stop();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (ShowLog)
            {
                NetworkComms.DisableLogging();
                ShowLog = false;
                e.Cancel = true;
                return;
            }
            Shutdown();
        }

        private static string Pad(object ss,int len)
        {
            var s = ss.ToString();
            var l = s.Length - len;
            if (l > 0)
            {
                for (int i = 0; i < l; i++)
                {
                    s = " " + s;
                }
            }
            return s;
        }

        public static void PrintTitle()
        {
            AddLog("");
            AddLog("Automated Suggestion System [Version 0.9.1]");
            AddLog("Negros Oriental State University - Guihulngan");
            AddLog("Computer Science Thesis - Class 2018");
            AddLog("");
            AddLog("Source Code: https://github.com/awooo-ph/ass");
            AddLog("");
            _logStart += 4;
        }
        
        public static void PrintDatabaseCounts()
        {
            AddLog("");
            AddLog("Database Summary");
            AddLog("");
            AddLog($"Offices:     { Pad(Models.Office.Cache.Count,4)}");
            AddLog($"Suggestions: { Pad(Models.Suggestion.Cache.Count,4) }");
            AddLog($"Reviews:     { Pad(Models.Rating.Cache.Count,4) }");
            AddLog($"Comments:    { Pad(Models.Comment.Cache.Count,4)}");
            AddLog($"Users:       {Pad(Models.User.Cache.Count.ToString(), 4)}");
            AddLog("");
            AddLog("");
        }

        public static void PrintAbout()
        {
            lock (logsync)
                Logs.Clear();
            AddLog("THE TEAM");
            AddLog("NORSU-G ComSci Class 2018");
            AddLog("");
            AddLog("* FIRSTNAME LASTNAME)");
            AddLog("  Team Leader / Graphics");
            AddLog("  email@asdf.com");
            AddLog("  093453453453");
            AddLog("");
            AddLog("* FIRSTNAME LASTNAME");
            AddLog("  Programmer (Android)");
            AddLog("  email@asdf.com");
            AddLog("  093453453453");
            AddLog("");
            AddLog("* FIRSTNAME LASTNAME");
            AddLog("  Programmer (Server)");
            AddLog("  email@asdf.com");
            AddLog("  093453453453");
            AddLog("");
            AddLog("* FIRSTNAME LASTNAME");
            AddLog("  Programmer (Client/Windows)");
            AddLog("  email@asdf.com");
            AddLog("  093453453453");
            AddLog("");
            AddLog("* FIRSTNAME LASTNAME");
            AddLog("  UX Designer");
            AddLog("  email@asdf.com");
            AddLog("  093453453453");
        }

        public static void PrintHelp()
        {
            AddLog("");
            AddLog("List of Available Commands");
            AddLog("");
            AddLog("ABOUT       Prints information about the authors.");
            AddLog("CLS         Clears the screen.");
            AddLog("CLEAR       Clears the screen.");
            AddLog("COMMANDS    Shows the list of commands.");
            AddLog("DATABASE    Shows database summary.");
            AddLog("DB          Shows database summary.");
            AddLog("EXIT        Terminates the server process.");
            AddLog("HELP        Prints the list of commands.");
            AddLog("RESET       Resets the database.");
            AddLog("QUIT        Terminates the server process.");
            AddLog("SUMMARY     Prints database summary.");
            AddLog("SHUTDOWN    Terminates the server process.");
            //AddLog("SHOW        Shows lists of items. Type SHOW HELP for details.");
            AddLog("TEAM        Prints informations about the team.");
            AddLog("VERSION     Prints the version information.");
            AddLog("");
        }

        private static void ShowHelp()
        {
            AddLog("");
            AddLog("Usage: SHOW [DATA] [PAGE] [OPTIONS]");
            AddLog("");
            AddLog("[DATA]     The type of data to show");
            AddLog("               OFFICES, REVIEWS, SUGGESTIONS, USERS");
            AddLog("[PAGE]     Page number");
            AddLog("[OPTIONS]  See options for each data type");
            AddLog("  OFFICES OPTIONS");
        }
        
        private static bool ConsoleCloseHandler(CtrlTypes ctrlType)

        {
            Shutdown();
            return true;

        }
        
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);
        
        public enum CtrlTypes

        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        
    }



}
