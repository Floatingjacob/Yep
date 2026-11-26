using SoundFlow.Components;
using System.Diagnostics;
using WebSocketSharp.Server;

namespace Yep
{
    internal class MultiplayerServer
    {
        private GameState game = new();
        private Menu menu = new();
        internal string? password = "";
        internal WebSocketServer wssv = null!;

        internal MultiplayerServer()
        {
            Menu.Screen main = menu.CreateScreen(0, Entry.header + " Configure Game");
            main.AddOptions(
                (() => $"Change starting seed (Current is {game.startingSeed})", 0, async () => { Change("Change starting seed\n", ref game.startingSeed); return true; }),
                (() => $"Change max level size (Current is {game.maxLevelSize})", 0, async () => { Change("Change max level size\n", ref game.maxLevelSize); game.maxLevelSize |= 1; return true; }),
                (() => $"Change starting level size (Current is {game.startingLevelSize})", 0, async () => { Change("Change starting level size", ref game.startingLevelSize); game.startingLevelSize |= 1; return true; }),
                (() => $"Change level size increment (Current is {game.levelSizeIncrement})", 0, async () => { Change("Change level size increment", ref game.levelSizeIncrement); game.levelSizeIncrement &= ~1u; return true; }),
                (() => $"Enemies (Currently {(game.enemiesEnabled ? "enabled" : "disabled")})", 0, async () => { game.enemiesEnabled = !game.enemiesEnabled; return true; }),
                (() => "Back", 0, async () => { return false; }));
        }
        
        internal void Host(int port = 9450)
        {
            wssv = new($"ws://0.0.0.0:{port}");
            wssv.AddWebSocketService("/", () => new MultiplayerSession(game, this));

            game.mazes.Clear();
            game.NewLevel((int)game.startingSeed, game.startingLevelSize);

            wssv.Start();

            Console.Clear();
            c.colorPrintln($"**green**Server started! Password is **dgreen**\"{password}\"\n**yellow**Type \"join\" to start playing.");
            InputLoop();
        }

        private void InputLoop()
        {
            Console.CursorVisible = true;
            string? input;
            while (true)
            {
                c.colorPrint("**yellow**\n> **red**", false);

                input = Console.ReadLine();

                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Type \"help\" for a list of commands.");
                    continue;
                }

                string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                switch (args[0].ToUpper())
                {
                    case "JOIN":
                        if (!OperatingSystem.IsWindows())
                            throw new NotImplementedException();

                        string path = Path.GetFullPath(Environment.GetCommandLineArgs()[0]);

#if DEBUG
                        if (path.Contains(".dll")) // Only for development
                            path = path.Replace(".dll", ".exe");
#endif
                        string? passwd = password;
                        if (string.IsNullOrWhiteSpace(passwd))
                            passwd = "!@EMPTY@!";

                        Process p = new();
                        ProcessStartInfo psi = new() { FileName = path, Arguments = $"localhost {passwd}", UseShellExecute = true };

                        p.StartInfo = psi;
                        p.Start();
                        break;

                    default:
                        c.colorPrint("**gray**Type \"help\" for a list of commands.");
                        break;
                }

                // kick, change password, tp players, etc.

            }
        }

        internal void SetPassword()
        {
            Console.Clear();

            c.colorPrint($"**cyan**{Entry.header} Choose a password (empty for none)\n**yellow**> **red**", false);

            password = Console.ReadLine();

            Console.Clear();
        }

        private void Change(string prompt, ref uint variable)
        {
            bool valid = false;

            while (!valid)
            {
                Console.Clear();

                c.colorPrint($"**cyan**{Entry.header} {prompt}\n**yellow**> **red**", false);

                string s = null!;

                while (s == null) s = Console.ReadLine()!;

                valid = uint.TryParse(s, out variable);
            }

            Console.ResetColor();

        }

        static internal bool IsValidName(string name)
        {
            if (name.Length <= 0) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Contains('|')) return false;
            if (name.Length >= 20) return false;
            return true;
        }

        internal void Configure()
        {
            //Console.Clear();
            menu.Navigate(0);
        }

    }
}
