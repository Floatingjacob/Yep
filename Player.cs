namespace Yep
{
    internal class Player
    {
        Maze Maze_ = null!;
        internal int[] PlayerBlip = [0, 0];
        private int Direction_ = 1;
        private int Score = 0;

        internal enum Direction
        {
            Left,
            Right,
            Up,
            Down
        }

        internal bool Move(Direction direction)
        {
            int oldX = PlayerBlip[0];
            var char_ = Maze.getChar(Maze_.Level[PlayerBlip[0], PlayerBlip[1]]);
            bool canMove = true;

            Console.SetCursorPosition(PlayerBlip[0] * 2, PlayerBlip[1]);
            Console.Write(new string(char_.Item1, char_.Item2));

            switch (direction)
            {
                case Direction.Left:
                    if (PlayerBlip[0] > 0 && CanMoveTo(Maze_.Level[PlayerBlip[0] - 1, PlayerBlip[1]]))
                        PlayerBlip[0] -= 1;
                    else canMove = false;
                    break;
                case Direction.Right:
                    if (PlayerBlip[0] < Maze_.Size && CanMoveTo(Maze_.Level[PlayerBlip[0] + 1, PlayerBlip[1]]))
                        PlayerBlip[0] += 1;
                    else canMove = false;
                    break;
                case Direction.Up:
                    if (PlayerBlip[1] > 0 && CanMoveTo(Maze_.Level[PlayerBlip[0], PlayerBlip[1] - 1]))
                        PlayerBlip[1] -= 1;
                    else canMove = false;
                    break;
                case Direction.Down:
                    if (PlayerBlip[1] < Maze_.Size && CanMoveTo(Maze_.Level[PlayerBlip[0], PlayerBlip[1] + 1]))
                        PlayerBlip[1] += 1;
                    else canMove = false;
                    break;
            }
            Console.SetCursorPosition(PlayerBlip[0] * 2, PlayerBlip[1]);

            if (PlayerBlip[0] - oldX != 0)
                Direction_ = PlayerBlip[0] - oldX;

            char_ = Maze.getChar(1);

            if (Direction_ == 1)
                Console.Write($" {char_.Item1}");
            if (Direction_ == -1) 
                Console.Write($"{char_.Item1} ");

            if (!canMove)
                Sound.Beep((200, 0.05));

            PostMove(canMove);

            return canMove;
        }

        internal static bool CanMoveTo(int type)
        {
            switch (type)
            {
                case -1: return false;
                case 0: return true;
                case 2: return true;
                case 3: return true;
                case 5: return true;
                default: return false;
            }
        }
        private void CheckTile(int x, int y)
        {
            switch (Maze_.Level[x, y])
            {
                case 0:
                    Sound.Beep((500, 0.05));
                    break;
                case 3:
                    
                    Sound.Beep((700, 0.1), (800, 0.1), (1000, 0.07));

                    Maze_.Seed++;
                    Maze_.Size += 4;
                    Maze_.Initialize(Maze_.Size);
                    Maze_.Generate(Maze_.Size);
                    Maze_.Draw();
                    break;
                case 5:
                    Sound.Beep((600, 0.05));
                    Score++;
                    Maze_.Level[x, y] = 0;
                    break;
            }
        }

        private void PostMove(bool moved)
        {
            if (moved)
                CheckTile(PlayerBlip[0], PlayerBlip[1]);
            Maze_.UpdateEnemies();
        }

        internal void Initialize(Maze maze)
        {
            Maze_ = maze;
        }

    }

}