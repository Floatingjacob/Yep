using WebSocketSharp.Server;

namespace Yep
{
    internal class MultiplayerServer
    {
        private GameState game = new();
        private Menu menu = new();
        internal string password = "";
        internal WebSocketServer wssv = null!;

        internal MultiplayerServer()
        {
            Menu.Screen main = menu.CreateScreen(0, "Configure Game");
            main.AddOptions(
                (() => $"Change starting seed (Current is {game.startingSeed})", 0, () => { Change("Change starting seed\n", ref game.startingSeed); return true; }),
                (() => $"Change max level size (Current is {game.maxLevelSize})", 0, () => { Change("Change max level size\n", ref game.maxLevelSize); game.maxLevelSize |= 1; return true; }),
                (() => $"Change starting level size (Current is {game.startingLevelSize})", 0, () => { Change("Change starting level size", ref game.startingLevelSize); game.startingLevelSize |= 1; return true; }),
                (() => $"Change level size increment (Current is {game.levelSizeIncrement})", 0, () => { Change("Change level size increment", ref game.levelSizeIncrement); game.levelSizeIncrement &= ~1u; return true; }),
                (() => $"Enemies (Currently {(game.enemiesEnabled ? "enabled" : "disabled")})", 0, () => { game.enemiesEnabled = !game.enemiesEnabled; return true; }),
                (() => "Back", 0, () => { return false; }));
        }

        internal void Host(int port = 9450)
        {
            wssv = new($"ws://0.0.0.0:{port}");
            wssv.AddWebSocketService("/", () => new MultiplayerSession(game));
            wssv.Start();

            game.mazes.Clear();
            game.NewLevel((int)game.startingSeed, game.startingLevelSize);

            Console.Clear();
            Console.WriteLine("Server started...");

            while (true) ;
        }

        internal void SetPassword()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;

            Console.WriteLine("Choose a password (empty for none)\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("> ");

            Console.ForegroundColor = ConsoleColor.Red;
            password = Console.ReadLine();
        }

        private void Change(string prompt, ref uint variable)
        {
            bool valid = false;

            while (!valid)
            {

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;

                Console.WriteLine(prompt);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("> ");

                Console.ForegroundColor = ConsoleColor.Red;
                
                string s = null!;

                while (s == null) s = Console.ReadLine()!;

                valid = uint.TryParse(s, out variable);
            }
        }



        internal void Configure()
        {
            //Console.Clear();
            menu.Navigate(0);
        }

    }
}
