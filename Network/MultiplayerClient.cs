using System.Data;
using WebSocketSharp;

namespace Yep
{
    internal class MultiplayerClient(string url, params string[] protocols) : WebSocket(url, protocols)
    {
        private int[,] Level = new int[,] { };
        private int[] Position = [0, 0];
        private Dictionary<(int x, int y), List<(string ID, char Mover)>> RemotePlayersOnTile = new();
        private string ID = null!;
        private int Score = 0;
        private Menu menu = new();
        private bool connected;
        internal string Name = "Anonymous";
        private string? Password;
        private MultiplayerClientMonitor Mon = null!;
        Task inputLoop = null!;

        internal async Task Initialize(string? autoPassword = null)
        {
            Mon = new(this)
                ;
            if (autoPassword != null)
                Password = autoPassword;
            if (Password == "!@EMPTY@!")
                Password = "";

            Score = 0;
            Level = new int[,] { };
            Entry.playing = true;
            Menu.Screen paused = menu.CreateScreen(-1, Entry.header + " Paused");
            paused.AddOptions(
                (() => $"Score: {Score}", -1, null),
                (() => "Resume", -1, async () => { Draw(); return false; }),
                (() => "Open monitor", -1, async () => { await Mon.Open(); return true; }),
                (() => "Exit to main menu", -1, async () => { Close(CloseStatusCode.Normal, "Exiting to main menu"); Entry.playing = false; return false; }));

            Menu.Screen password = menu.CreateScreen(0, Entry.header + "This server is password protected.\n What do you do?");
            password.AddOptions(
                (() => "Attempt password", 0, async () => {Send($"{NetworkCodes.Password}|{PasswordPrompt()}"); return false; }),
                (() => "Run away", 0, async () => { Close(CloseStatusCode.Normal, "Couldn't guess password"); Entry.playing = false; return false; }));

            OnMessage += onMessage;
            OnOpen += onOpen;
            OnClose += onClose;
            
            
            Task.Run(Connect); // I want my beeping dots

            Console.Clear();

            c.colorPrint($"**cyan**{Entry.header} Connecting to server", false);

            while (!connected)
            {
                Sound.Beep((700, 0.05));
                Console.Write('.');
                await Task.Delay(500);
            }

            while (Entry.playing) ;
        }


        private void onClose(object? sender, CloseEventArgs e)
        {
            //if (e.Code != 1) Connect(); // Try to reconnect if the connection closed with an error
            Entry.playing = false;
            connected = false;
                
        }

        private void onOpen(object? sender, EventArgs e)
        {
            connected = true;
            Send($"{NetworkCodes.Heartbeat}");
        }

        private void onMessage(object? sender, MessageEventArgs e)
        {
            string[] splitMessage = e.Data.Split('|');
            int type = int.Parse(splitMessage[0]);

            switch (type)
            {
                case NetworkCodes.ConnectionReceived:
                    ID = splitMessage[1];
                    Console.WriteLine("\nReceived a response from the server!");
                    break;
                case NetworkCodes.Password:
                    if (Password != null)
                        Send($"{NetworkCodes.Password}|{Password}");
                    else
                    {
                        Sound.Beep((300, 0.1), (0, 0.05), (300, 0.1));
                        menu.Navigate(0);
                    }
                    break;
                case NetworkCodes.Name:
                    Send($"{NetworkCodes.Name}|{Name}");
                    break;
                case NetworkCodes.Level:
                    Level = MultiplayerSerialization.ToArray(splitMessage[1]);
                    break;
                case NetworkCodes.Forbidden:
                    Mon.Push($"Received message from server: **red**{splitMessage[1]}**gray**");
                    break;
                case NetworkCodes.Start:
                    RemotePlayersOnTile.Clear();
                    Sound.Beep((700, 0.1), (800, 0.1), (1000, 0.07));
                    Draw();
                    if (inputLoop == null)
                        inputLoop = Task.Run(InputLoop);
                    break;
                case NetworkCodes.PlayerPosition:
                    int[] position = [.. splitMessage[1].Split(',').Select(int.Parse)];
                    int[] oldPosition = [.. splitMessage[2].Split(',').Select(int.Parse)];

                    if (!RemotePlayersOnTile.ContainsKey((position[0], position[1])))
                        RemotePlayersOnTile[(position[0], position[1])] = [];
                    if (!RemotePlayersOnTile.ContainsKey((oldPosition[0], oldPosition[1])))
                        RemotePlayersOnTile[(oldPosition[0], oldPosition[1])] = [];

                    var char_ = Maze.getChar(Level[oldPosition[0], oldPosition[1]]);

                    if (splitMessage[3] == ID)
                    {
                        if (Level[position[0], position[1]] == 3)
                        {
                            Send($"{NetworkCodes.NextLevel}");
                            break;
                        }

                        Position = position;

                        if (position.SequenceEqual(oldPosition))
                            Sound.Beep((200, 0.05)); // Can't move
                        else if (Level[position[0], position[1]] == 0) 
                            Sound.Beep((500, 0.05)); // On empty tile

                        Console.SetCursorPosition(oldPosition[0] * 2, oldPosition[1] + 1);

                        if (RemotePlayersOnTile[(oldPosition[0], oldPosition[1])].Count > 0) // If one or more remote players are on the tile, redraw one
                            Console.Write(RemotePlayersOnTile[(oldPosition[0], oldPosition[1])][0].Mover);
                        else Console.Write(new string(char_.Item1, char_.Item2));

                        Console.SetCursorPosition(position[0] * 2, position[1] + 1);
                        Console.Write(Maze.getChar(1).Item1);

                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    }
                    if (!oldPosition.SequenceEqual(position))
                    {
                        RemotePlayersOnTile[(position[0], position[1])].Add((splitMessage[3], splitMessage[4][0]));
                        RemotePlayersOnTile[(oldPosition[0], oldPosition[1])].Remove((splitMessage[3], splitMessage[4][0]));
                    }

                    if (!oldPosition.SequenceEqual(Position)) // Don't draw over the local player
                    {
                        Console.SetCursorPosition(oldPosition[0] * 2, oldPosition[1] + 1);
                        Console.Write(new string(char_.Item1, char_.Item2));
                    }
                    if (!position.SequenceEqual(Position)) // Don't draw over the local player
                    {
                        Console.SetCursorPosition(position[0] * 2, position[1] + 1);
                        Console.Write(splitMessage[4]);
                    }

                    break;
                case NetworkCodes.UpdateTile:
                    int[] tile = [.. splitMessage[1].Split(',').Select(int.Parse)];
                    int newTile = int.Parse(splitMessage[2]);
                    Level[tile[0], tile[1]] = newTile;

                    if (!RemotePlayersOnTile.ContainsKey((tile[0], tile[1])))
                        RemotePlayersOnTile[(tile[0], tile[1])] = [];

                    if (newTile == 4 || !tile.SequenceEqual(Position) && RemotePlayersOnTile[(tile[0], tile[1])].Count <= 0)
                    { // Enemies can draw over local and remote players
                        Console.SetCursorPosition(tile[0] * 2, tile[1] + 1);
                        Console.Write(Maze.getChar(Level[tile[0], tile[1]]).Item1);
                    }
                    break;
                case NetworkCodes.Score:
                    Sound.Beep((600, 0.05));
                    Score++;
                    break;
                case NetworkCodes.PeerDisconnect:
                    int[] tile_ = [.. splitMessage[1].Split(',').Select(int.Parse)];

                    foreach (var item in RemotePlayersOnTile[(tile_[0], tile_[1])])
                    {
                        if (item.ID == splitMessage[2])
                        {
                            RemotePlayersOnTile[(tile_[0], tile_[1])].Remove(item);
                            break;
                        }
                    }

                    Console.SetCursorPosition(tile_[0] * 2, tile_[1] + 1); // need to check if other players are on this tile
                    Console.Write(Maze.getChar(Level[tile_[0], tile_[1]]).Item1);

                    break;
                case NetworkCodes.Message:
                    // should send different IDs for different message types
                    Mon.Push(splitMessage[1]);
                    break;
            }
        }

        private void InputLoop()
        {
            while (Entry.playing)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.LeftArrow:
                        Send($"{NetworkCodes.Move}|{(int)Player.Direction.Left}");
                        break;
                    case ConsoleKey.RightArrow:
                        Send($"{NetworkCodes.Move}|{(int)Player.Direction.Right}");
                        break;
                    case ConsoleKey.UpArrow:
                        Send($"{NetworkCodes.Move}|{(int)Player.Direction.Up}");
                        break;
                    case ConsoleKey.DownArrow:
                        Send($"{NetworkCodes.Move}|{(int)Player.Direction.Down}");
                        break;
                    case ConsoleKey.Escape:
                        Sound.Beep((600, 0.1), (800, 0.1));
                        menu.Navigate(-1);
                        break;
                }
            }
        }

        private string PasswordPrompt()
        {
            string? password = null;

            while (string.IsNullOrWhiteSpace(password))
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(Entry.header + " Enter a password");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("\n> ");
                Console.ForegroundColor = ConsoleColor.Red;
                password = Console.ReadLine();
            }

            return password;
        }

        internal void Draw()
        {
            Console.Clear();

            for (int y = 0; y < Level.GetLongLength(1); y++)
            {
                for (int x = 0; x < Level.GetLongLength(0); x++)
                {
                    var char_ = Maze.getChar(Level[x, y]);

                    Console.SetCursorPosition(x * 2, y + 1);
                    Console.Write(new string(char_.Item1, char_.Item2));
                }
            }
            Send($"{NetworkCodes.GetPlayers}");
        }
    }
}
