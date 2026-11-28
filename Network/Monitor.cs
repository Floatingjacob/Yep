namespace Yep.Network
{
    internal class Monitor(Server.Server? server = null, Client.Client? client = null)
    {
        private List<string> History = [];
        private List<string> WrappedHistory = [];
        private readonly Lock Lock_ = new();
        internal bool Visible = false;
        private string InputBuffer = "";
        private int scroll = 0;
        private int Width;

        internal async Task Open()
        {
            if (client != null)
                Visible = true;
            Width = Console.WindowWidth;
            Console.ResetColor();

            Push(null);

            Console.CursorVisible = true;

            while (Visible || server.wssv.IsListening )
            {
                lock (Lock_)
                    DrawArrow();

                string? input = null;

                while (string.IsNullOrWhiteSpace(input))
                {

                    ConsoleKeyInfo key = Console.ReadKey(true);

                    switch (key.Key)
                    {
                        case ConsoleKey.Backspace:
                            if (InputBuffer.Length > 0)
                            {
                                InputBuffer = InputBuffer.Substring(0, InputBuffer.Length - 1);
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                                Console.Write(' ');
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            }
                            break;
                        case ConsoleKey.Enter:
                            input = InputBuffer;
                            InputBuffer = "";
                            break;
                        case ConsoleKey.UpArrow:
                            if (scroll < WrappedHistory.Count - 1)
                            {
                                scroll++;
                                Push(null);
                            }
                            break;
                        case ConsoleKey.DownArrow:
                            if (scroll > 0)
                            {
                                scroll--;
                                Push(null);
                            }
                            break;

                        default:
                            InputBuffer += key.KeyChar;
                            Console.Write(key.KeyChar);
                            break;
                    }
                }

                if (client != null)
                {
                    if (input.StartsWith('/'))
                    {
                        if (input.Trim().ToUpper() == "/CLOSE")
                        {
                            Visible = false;
                            break;
                        }

                        if (await client.commands.TryRun(input.Substring(1)) == false)
                            Push("\n**gray**Invalid command. Type \"/help\" for a list of commands.**white**");
                    }
                    else client.Send($"{NetworkCodes.Message}|{input}");

                    input = null;
                    continue;
                }

                if (await server.commands.TryRun(input) == false)
                {
                    Push("\n**gray**Invalid command. Type \"help\" for a list of commands.**white**");
                }

                input = null;
            }

            Console.CursorVisible = false;
        }

        internal void Push(string? message)
        {
            lock (Lock_)
            {
                push(message);
            }
        }

        private void push(string? message = null)
        {
            if (message != null)
            {
                if (Console.WindowWidth != Width)
                {
                    WrappedHistory.Clear();
                    History.ForEach(x => Wrap(x).ForEach(WrappedHistory.Add));
                }
                History.Add(message);
                Wrap(message).ForEach(WrappedHistory.Add);
            }

            if (Visible || server != null)
            {
                //List<string> wrappedHistory = [];

                Console.Clear();

                int availableRows = Console.WindowHeight - 1;
                scroll = Math.Clamp(scroll, 0, Math.Max(0, WrappedHistory.Count - availableRows));
                int startIndex = Math.Max(0, WrappedHistory.Count - availableRows) - scroll;

                WrappedHistory.Skip(startIndex).Take(availableRows).ToList().ForEach(x => c.colorPrintln(x, false));

                DrawArrow();
            }
            else if (client != null)
            {
                Console.SetCursorPosition(0, 0);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, 0);

                Console.ResetColor();
                Console.Write(message.Substring(0, Math.Min(message.Length, Console.WindowWidth - 1)));
            }
        }

        private void DrawArrow()
        {
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, Console.WindowHeight - 1);
            c.colorPrint("**yellow**> **gray**", false);
            if (InputBuffer.Length > 0)
                Console.Write(InputBuffer);
        }

        private List<string> Wrap(string input)
        {
            List<string> result = [];
            int width = Console.WindowWidth - 1;

            foreach (string line in input.Split('\n'))
            {
                string s = line.TrimEnd('\r');

                while (s.Length >= width)
                {
                    result.Add(s[..width]);
                    s = s[width..];
                }
                result.Add(s);
            }

            return result;
        }

    }
}
