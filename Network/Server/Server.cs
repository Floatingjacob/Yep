using WebSocketSharp.Server;
using Yep.Core;

namespace Yep.Network.Server
{
    internal class Server
    {
        private GameState game = new();
        private Menu menu = new();
        internal string password = "";
        internal WebSocketServer wssv = null!;
        internal Monitor mon;
        internal Commands commands = null!;

        internal Server()
        {
            Menu.Screen main = menu.CreateScreen(0, Entry.header + " Configure Game");
            main.AddOptions(
                (() => $"Change starting seed (Current is {game.startingSeed})", 0, async () => { Change("Change starting seed\n", ref game.startingSeed); return true; }),
                (() => $"Change max level size (Current is {game.maxLevelSize})", 0, async () => { Change("Change max level size\n", ref game.maxLevelSize); game.maxLevelSize |= 1; return true; }),
                (() => $"Change starting level size (Current is {game.startingLevelSize})", 0, async () => { Change("Change starting level size", ref game.startingLevelSize); game.startingLevelSize |= 1; return true; }),
                (() => $"Change level size increment (Current is {game.levelSizeIncrement})", 0, async () => { Change("Change level size increment", ref game.levelSizeIncrement); game.levelSizeIncrement &= ~1u; return true; }),
                (() => $"Enemies (Currently {(game.enemiesEnabled ? "enabled" : "disabled")})", 0, async () => { game.enemiesEnabled = !game.enemiesEnabled; return true; }),
                (() => "Back", 0, async () => { return false; }));

            mon = new(this);
            commands = new(this, game);
            commands.RegisterAll();
            
        }
        
        internal async Task Host(int port = 9450)
        {
            wssv = new($"ws://0.0.0.0:{port}");
            wssv.AddWebSocketService("/", () => new Session(game, this));

            game.mazes.Clear();
            game.NewLevel((int)game.startingSeed, game.startingLevelSize);

            wssv.Log.Level = WebSocketSharp.LogLevel.Error;
            wssv.Log.Output = (logData, filePath) =>
            {
                mon.Push($"**dgray**[Websocket {logData.Level} error] {logData.Message}**white**");
            };
            wssv.Start();

            Console.Clear();
            mon.Push($"**green**Server started! Password is **dgreen**\"{password}\"\n**yellow**Type \"join\" to start playing.**white**");
            await mon.Open();
        }
        
        internal void SetPassword()
        {
            Console.Clear();

            c.colorPrint($"**cyan**{Entry.header} Choose a password (empty for none)\n**yellow**> **red**", false);

            password = Console.ReadLine() ?? "";

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
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (name.Length <= 0) return false;
            if (name.Contains('|')) return false;
            if (name.Length >= 20) return false;
            return true;
        }

        internal void Configure()
        {
            //Console.Clear();
            menu.Navigate(0).Wait();
        }

    }
}
