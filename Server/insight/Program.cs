using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetworkCommsDotNet;

namespace norsu.ass.Server
{
    static class Program
    {
        private static bool _shuttingDown;

        static void Main(string[] args)
        {
            awooo.IsRunning = true;
            Console.BufferWidth = 666;
            Console.BufferHeight = 333;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            Console.Title = "NORSU-G Suggestion Server";
            PrintTitle();
            awooo.Context = SynchronizationContext.Current;
            awooo.IsRunning = true;
            
            Console.WriteLine("Starting server...");
            
            Network.Server.Instance.Start();
            
            Console.WriteLine("Server started");

            var cmd = args.FirstOrDefault()??"";
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
                        PrintAbout();
                        break;
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;
                    case "debug":
                        ShowLog = true;
                        NetworkComms.EnableLogging();
                        break;
                }
                
                Console.Write("ass> ");
                cmd = Console.ReadLine();
            }

            Shutdown();

        }
        public static bool ShowLog { get; set; }

        public static void Log(string message)
        {
            if(ShowLog)
                Console.WriteLine($"{DateTime.Now.ToString("g")}: {message}");
        }
        public static async void Shutdown()
        {
            if (_shuttingDown) return;
            _shuttingDown = true;
            Console.WriteLine("Server is shutting down...");
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

        public static void PrintTitle()
        {
            Console.WriteLine("Automated Suggestion System [Version 0.9-alpha]");
            Console.WriteLine("Negros Oriental State University - Guihulngan");
            Console.WriteLine("Computer Science Thesis - Class 2018");
            Console.WriteLine();
        }
        public static void PrintDatabaseCounts()
        {
            Console.WriteLine("Database Summary");
            Console.WriteLine($"Users:       {Models.User.Cache.Count}");
            Console.WriteLine($"Offices:     {Models.Office.Cache.Count}");
            Console.WriteLine($"Suggestions: {Models.Suggestion.Cache.Count}");
            Console.WriteLine($"Reviews:     {Models.Rating.Cache.Count}");
            Console.WriteLine($"Comments:    {Models.Comment.Cache.Count}");
        }

        private static void PrintAbout()
        {
            Console.WriteLine(@"

THE TEAM
NORSU-G ComSci Class 2018

* FIRSTNAME LASTNAME
  Team Leader / Graphics
  email@asdf.com
  093453453453

* FIRSTNAME LASTNAME
  Programmer (Android)
  email@asdf.com
  093453453453

* FIRSTNAME LASTNAME
  Programmer (Server)
  email@asdf.com
  093453453453

* FIRSTNAME LASTNAME
  UI / UX Designer
  email@asdf.com
  093453453453

* FIRSTNAME LASTNAME
  Programmer (Client)
  email@asdf.com
  093453453453

ENGR. FIRSTNAME LASTNAME
Adviser
email@asdf.com
093453453453

");
            
        }

        public static void PrintHelp()
        {
            Console.WriteLine(@"
ABOUT       Prints information about the authors.
CLS         Clears the screen.
CLEAR       Clears the screen.
COMMANDS    Shows the list of commands.
DATABASE    Shows database summary.
DB          Shows database summary.
EXIT        Terminates the server process.
HELP        Prints the list of commands.
QUIT        Terminates the server process.
SUMMARY     Prints database summary.
SHUTDOWN    Terminates the server process.
TEAM        Prints informations about the team.

");
        }

    }

}
