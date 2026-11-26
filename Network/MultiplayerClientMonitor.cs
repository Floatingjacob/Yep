namespace Yep
{
    internal class MultiplayerClientMonitor(MultiplayerClient client)
    {
        private List<string> History = [];
        private bool Visible = false;
        private readonly Lock Lock_ = new();
        private string InputBuffer = "";
        private int Offset = 0;

        internal async Task Open()
        {
            Visible = true;
            Console.Clear();
            Console.ResetColor();
            foreach (string s in History)
                Console.Write(s);

            Console.CursorVisible = true;

            while (Entry.playing)
            {
                lock(Lock_)
                    DrawArrow(Offset + 1);

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
                        default:
                            InputBuffer += key.KeyChar;
                            Console.Write(key.KeyChar);
                            break;
                    }
                }

                if (input.ToUpper() == "/EXIT") break;

                client.Send($"{NetworkCodes.Message}|{input}");

                input = null;
            }

            Console.CursorVisible = false;
            Visible = false;
        }

        internal void Push(string message)
        {
            lock (Lock_)
            {
                push(message);
            }
        }

        private void push(string message)
        {
            History.Add(Chop(message));

            int offset = 0;

            foreach (string s in History)
                offset += s.Split('\n').Length - 1;

            Offset = offset;

            if (Visible)
            {
                Console.SetCursorPosition(0, offset);
                Console.Write(History[History.Count - 1]);
                DrawArrow(offset + 1);
                return;
            }

            // Prints message at the top of the screen
            Console.SetCursorPosition(0, 0);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 0);

            Console.ResetColor();
            Console.Write(message.Substring(0, Math.Min(message.Length, Console.WindowWidth - 1)));
        }

        private void DrawArrow(int position)
        {
            Console.SetCursorPosition(0, position);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, position);
            c.colorPrint("**yellow**> **gray**", false);
            if (InputBuffer.Length > 0)
                Console.Write(InputBuffer);
        }
        private string Chop(string input)
        {
            string s = input;
            string result = "";

            while (s.Length >= Console.WindowWidth - 1)
            {
                result += $"{s.Substring(0, Console.WindowWidth - 1)}\n";
                s = s.Substring(Console.WindowWidth - 1);
            }

            if (s.Length > 0)
                result += $"{s}\n";

            return result;
        }

    }
}
