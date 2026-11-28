using Yep;

class Entry
{

    public static string header = @"
 /$$     /$$                 
|  $$   /$$/                 
 \  $$ /$$//$$$$$$   /$$$$$$ 
  \  $$$$//$$__  $$ /$$__  $$
   \  $$/| $$$$$$$$| $$  \ $$
    | $$ | $$_____/| $$  | $$
    | $$ |  $$$$$$$| $$$$$$$/
    |__/  \_______/| $$____/ 
                   | $$      
                   | $$      
                   |__/ 

";
    private static MultiplayerServer server = new();

    private static MultiplayerClient client = new("ws://localhost:9450");
    private static Menu menu = new();
    private static Maze maze = new();
    public static bool playing = false;
    static async Task Main(string[] args)
    {
        Sound.Beep((0, 0.1)); // Ensure the sound lib loads at program start so there isn't a pause when the first sound is played
        Console.Title = "Your Enigmatic Program - A game with mazes";
        Console.CursorVisible = false;
        Menu.Screen main = menu.CreateScreen(0, header + " Your Enigmatic Program\n   A game with mazes");
        main.AddOptions(
            (() => "Singleplayer", 1, null),
            (() => "Multiplayer", 2, null),
            (() => "Exit", -1, async () => { Environment.Exit(0); return false; }));

        Menu.Screen singleplayer = menu.CreateScreen(1, header + " Singleplayer");
        singleplayer.AddOptions(
            (() => "Start", 0, async () => { Singleplayer(); return true; }),
            (() => "Back", 0, null));
        Menu.Screen multiplayer = menu.CreateScreen(2, header + " Multiplayer");
        multiplayer.AddOptions(
            (() => "Host", 3, null),
            (() => "Join", 4, null),
            (() => "Back", 0, null));

        Menu.Screen multiplayerHost = menu.CreateScreen(3, header + " Host Multiplayer");
        multiplayerHost.AddOptions(
            (() => "Start hosting", 0, async () => { server.Host(); return true; }),
            (() => "Game settings", 3, async () => { server.Configure(); return true; }),
            (() => $"Set password (Currently set to \"{server.password}\")", 3, async () => { server.SetPassword(); return true; }),
            (() => "Back", 2, null));

        Menu.Screen multiplayerJoin = menu.CreateScreen(4, header + " Join Multiplayer");
        multiplayerJoin.AddOptions(
            (() => $"Change name (Currently set to \"{client.Name}\")", 4, async () => { ChangeName(); return true; }),
            (() => "Connect to a server", 0, async () => { await IpAndConnect(); return true; }),
            (() => "Back", 2, null));

        Menu.Screen paused = menu.CreateScreen(-1, header + " Paused");
        paused.AddOptions(
            (() => "Resume", 0, async () => { maze.Draw(); return false; }),
            (() => "Exit to main menu", 0, async () => { playing = false; return false; }));


        if (args.Length > 0)
        {
            if (args.Length != 3)
            {
                c.colorPrintln("**red**Invalid or malformed arguments.\nProper format is \"Yep <server ip> \"server password\" \"name\"\"");
                Console.ReadKey();
                Environment.Exit(1);
            }
            else await IpAndConnect(args[0], args[1], args[2]);
        }
        await menu.Navigate(0);
    }

    private static async Task IpAndConnect(string? ip = null, string? password = null, string? Name = null)
    {
        string name = client.Name;

        if (ip != null)
        {
            client = new($"ws://{ip}:9450") { Name = Name ?? name };
            await client.Initialize(password);
        }
        else
        {

            Console.Clear();
            c.colorPrint($"**cyan**{header} Enter the IP address of the server\n\n**yellow**> **red**", false);

            while (string.IsNullOrWhiteSpace(ip)) ip = Console.ReadLine()!;

            Console.ResetColor();

            client = new($"ws://{ip}:9450") { Name = name };
            await client.Initialize(password);
        }
    }

    private static void ChangeName()
    {
        Console.Clear();
        c.colorPrint($"**cyan**{header} Choose a name\n\n**yellow**> **red**", false);
        string name = null!;

        while (!MultiplayerServer.IsValidName(name)) name = Console.ReadLine()!;

        Console.ResetColor();

        client.Name = name;

    }
    private static void Singleplayer()
    {
        playing = true;
        maze = new();
        maze.Generate(5);
        maze.Draw();

        while (playing)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.LeftArrow:
                    maze.Player_.Move(Player.Direction.Left);
                    break;
                case ConsoleKey.RightArrow:
                    maze.Player_.Move(Player.Direction.Right);
                    break;
                case ConsoleKey.UpArrow:
                    maze.Player_.Move(Player.Direction.Up);
                    break;
                case ConsoleKey.DownArrow:
                    maze.Player_.Move(Player.Direction.Down);
                    break;
                case ConsoleKey.Enter:
                    maze.Seed++;
                    maze.Generate(51);
                    maze.Draw();
                    break;
                case ConsoleKey.Escape:
                    Sound.Beep((600, 0.1), (800, 0.1));
                    menu.Navigate(-1).Wait();
                    break;

            }
        }
    }
}
