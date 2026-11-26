using WebSocketSharp;

namespace Yep
{
    internal class MultiplayerClient(string url, params string[] protocols) : WebSocket(url, protocols)
    {
        private int[,] Level = new int[,] { };
        private string ID = null!;

        internal string Name = "Anonymous";

        internal void Initialize()
        {
            OnMessage += onMessage;
            OnOpen += onOpen;
            OnClose += onClose;
            Connect();
            while (true) ;
        }

        private void onClose(object? sender, CloseEventArgs e)
        {
            if (e.Code > 0) Connect(); // Try to reconnect if the connection closed with an error

        }

        private void onOpen(object? sender, EventArgs e)
        {
            Send($"{NetworkCodes.Heartbeat}");
            Send($"{NetworkCodes.Name}|{Name}");
        }

        private void onMessage(object? sender, MessageEventArgs e)
        {
            string[] splitMessage = e.Data.Split('|');
            int type = int.Parse(splitMessage[0]);

            switch (type)
            {
                case NetworkCodes.ConnectionReceived:
                    ID = splitMessage[1];
                    Console.WriteLine("Received a response from the server");
                    break;
                case NetworkCodes.Level:
                    Level = MultiplayerSerialization.ToArray(splitMessage[1]);
                    break;
                case NetworkCodes.Start:
                    Draw();
                    Task.Run(InputLoop);
                    break;
                case NetworkCodes.PlayerPosition:
                    int[] position = [.. splitMessage[1].Split(',').Select(int.Parse)];
                    int[] oldPosition = [.. splitMessage[2].Split(',').Select(int.Parse)];

                    var char_ = Maze.getChar(Level[oldPosition[0], oldPosition[1]]);

                    Console.SetCursorPosition(oldPosition[0] * 2, oldPosition[1]);
                    Console.Write(new string(char_.Item1, char_.Item2));

                    Console.SetCursorPosition(position[0] * 2, position[1]);

                    if (splitMessage[3] == ID)
                    {
                        bool moved = false;

                        if (position[0] == oldPosition[0] && position[1] == oldPosition[1])
                            Sound.Beep((200, 0.05));
                        else moved = true;

                        Console.Write(Maze.getChar(1).Item1);
                        CheckTile(position[0], position[1], moved);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else Console.Write(splitMessage[4]);
                    break;
            }
        }

        private void InputLoop()
        {
            while (true)
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
                }
            }
        }

        internal void Draw()
        {
            Console.Clear();

            for (int y = 0; y < Level.GetLongLength(1); y++)
            {

                for (int x = 0; x < Level.GetLongLength(0); x++)
                {
                    var char_ = Maze.getChar(Level[x, y]);

                    Console.SetCursorPosition(x * 2, y);
                    Console.Write(new string(char_.Item1, char_.Item2));

                }
            }
        }

        private void CheckTile(int x, int y, bool moved)
        {
            switch (Level[x, y])
            {
                case 0:
                    if (moved)
                        Sound.Beep((500, 0.05));
                    break;
                case 3:
                    Sound.Beep((700, 0.1), (800, 0.1), (1000, 0.07));
                    Send($"{NetworkCodes.NextLevel}");

                    break;
                case 5:
                    if (moved)
                        Sound.Beep((600, 0.05));
                    break;
            }
        }

    }
}
