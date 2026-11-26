namespace Yep
{
    internal class Menu
    {
        internal Dictionary<int, Menu.Screen> Menues = new();
        internal delegate bool Del();

        public Screen CreateScreen(int Id, string Name)
        {
            Screen screen = new() { Id = Id, Name = Name, menu = this };
            Menues[Id] = screen;
            return screen;
        }

        internal class Screen
        {
            required internal int Id;
            required internal string Name;
            internal Menu menu = null!;
            internal List<(int id, Func<string> text, int targetMenu, Del? action)> Options = [];

            public void AddOption(Func<string> text, int targetMenu, Del? action)
            {
                Options.Add((Options.Count + 1, text, targetMenu, action));
            }

            public void AddOptions(params (Func<string> text, int targetMenu, Del? action)[] options)
            {
                foreach (var option in options)
                {
                    Options.Add((Options.Count + 1, option.text, option.targetMenu, option.action));
                }
            }

            /// <summary>
            /// Do not manually call this function. Use <see cref="Menu.Navigate(int)"/> instead.
            /// </summary>

            internal void Display()
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{Name}\n");
                Console.ForegroundColor = ConsoleColor.White;

                foreach (var option in Options)
                {
                    Console.WriteLine($"  {option.text()}");
                }
            }
        }

        public void Navigate(int menuId)
        {
            Screen menu = Menues[menuId];
            int offset = menu.Name.Split('\n').Count() + 1;
            menu.Display();

            int cursor = 0;

            Console.SetCursorPosition(0, cursor + offset);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write('>');

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey();

                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (cursor > 0)
                        {

                            Console.SetCursorPosition(0, cursor + offset);
                            Console.Write(' ');
                            cursor--;
                            Console.SetCursorPosition(0, cursor + offset);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write('>');
                        }

                        break;
                    case ConsoleKey.DownArrow:
                        if (cursor < menu.Options.Count - 1)
                        {
                            Console.SetCursorPosition(0, cursor + offset);
                            Console.Write(' ');
                            cursor++;
                            Console.SetCursorPosition(0, cursor + offset);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write('>');
                        }
                        break;
                    case ConsoleKey.Enter:
                        bool navAfter = true;
                        if (menu.Options[cursor].action != null)
                            navAfter = menu.Options[cursor].action.Invoke();

                        if (navAfter)
                        {
                            menu = Menues[menu.Options[cursor].targetMenu];
                            offset = menu.Name.Split('\n').Count() + 1;
                            menu.Display();

                            cursor = 0;

                            Console.SetCursorPosition(0, cursor + offset);
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.Write('>');
                        }

                        else return;
                        break;

                }
            }
        }
    }
}
