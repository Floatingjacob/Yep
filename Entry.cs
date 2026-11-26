using Yep;

class Entry
{

    private static string header = @"
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

  Your Enigmatic Program
    A game with mazes";
    private static MultiplayerServer server = new();

    private static MultiplayerClient client = new("ws://localhost:9450");
    private static Menu menu = new();

    static void Main(string[] args)
    {
        Console.CursorVisible = false;

        Menu.Screen main = menu.CreateScreen(0, header);
        main.AddOptions(
            (() => "Singleplayer", 1, null),
            (() => "Multiplayer", 2, null),
            (() => "Exit", -1, () => { Environment.Exit(0); return false; }));

        Menu.Screen singleplayer = menu.CreateScreen(1, "Singleplayer");
        singleplayer.AddOptions(
            (() => "Start", 0, () => { Singleplayer(); return false; }),
            (() => "Back", 0, null));

        Menu.Screen multiplayer = menu.CreateScreen(2, "Multiplayer");
        multiplayer.AddOptions(
            (() => "Host", 3, null),
            (() => "Join", 4, null),
            (() => "Back", 0, null));

        Menu.Screen multiplayerHost = menu.CreateScreen(3, "Host Multiplayer");
        multiplayerHost.AddOptions(
            (() => "Start hosting", 3, () => { server.Host(); return false; }),
            (() => "Game settings", 3, () => { server.Configure(); return true; }),
            (() => $"Set password (Currently set to \"{server.password}\")", 3, () => { server.SetPassword(); return true; }),
            (() => "Back", 2, null));

        Menu.Screen multiplayerJoin = menu.CreateScreen(4, "Join Multiplayer");
        multiplayerJoin.AddOptions(
            (() => $"Change name (Currently set to \"{client.Name}\")", 4, () => { ChangeName(); return true; }),
            (() => "Connect to a server", 4, () => { IpAndConnect(); return false; }),
            (() => "Back", 2, null));

        menu.Navigate(0);
    }

    private static void IpAndConnect()
    {
        string name = client.Name;
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Enter the IP address of the server\n");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("> ");
        Console.ForegroundColor = ConsoleColor.Red;
        string ip = null!;
        while (ip == null) ip = Console.ReadLine()!;
        client = new($"ws://{ip}:9450");
        client.Name = name;
        client.Initialize();
    }

    private static void ChangeName()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Choose a name\n");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("> ");
        Console.ForegroundColor = ConsoleColor.Red;
        string name = null!;
        while (name == null) name = Console.ReadLine()!;
        client.Name = name;

    }
    private static void Singleplayer()
    {
        Maze maze = new();
        maze.Generate(5);
        maze.Draw();

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey();
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

            }
        }
    }
}
