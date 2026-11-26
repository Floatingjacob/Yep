namespace Yep
{
    internal class Enemy
    {
        int[] Position = [0, 0];
        int Tile = 0;
        Maze maze = null!;

        internal void Move(MultiplayerSession? multiplayerSession = null)
        {
            Tuple<char, int> char_;

            maze.Level[Position[0], Position[1]] = Tile;

            if (multiplayerSession != null)
                multiplayerSession.UpdateTile(maze.Seed, Position[0], Position[1], Tile);
            else
            {
                Console.SetCursorPosition(Position[0] * 2, Position[1]);

                char_ = Maze.getChar(maze.Level[Position[0], Position[1]]);

                Console.Write(new string(char_.Item1, char_.Item2));
            }

            int direction = maze.Rand.Next(0, 4);

            switch (direction)
            {
                case 0:
                    if (Position[0] > 0 && CanMoveTo(maze.Level[Position[0] - 1, Position[1]]))
                        Position[0] -= 1;
                    break;
                case 1:
                    if (Position[0] < maze.Size - 1 && CanMoveTo(maze.Level[Position[0] + 1, Position[1]]))
                        Position[0] += 1;
                    break;
                case 2:
                    if (Position[1] > 0 && CanMoveTo(maze.Level[Position[0], Position[1] - 1]))
                        Position[1] -= 1;
                    break;
                case 3:
                    if (Position[1] < maze.Size - 1 && CanMoveTo(maze.Level[Position[0], Position[1] + 1]))
                        Position[1] += 1;
                    break;
            }

            Tile = maze.Level[Position[0], Position[1]]; // Store old tile
            maze.Level[Position[0], Position[1]] = 4;    // Set tile as enemy

            if (multiplayerSession != null) 
                multiplayerSession.UpdateTile(maze.Seed, Position[0], Position[1], 4);
            else
            {
                Console.SetCursorPosition(Position[0] * 2, Position[1]);

                char_ = Maze.getChar(4);

                Console.Write(char_.Item1);
            }
            
        }

        private static bool CanMoveTo(int type)
        {
            switch (type)
            {
                case -1: return false; // Wall
                case 0: return true;   // Empty space
                case 2: return false;  // Entrance
                case 3: return false;  // Exit
                case 4: return false;  // Enemy
                case 5: return true;   // Point
                default: return false;
            }
        }

        internal void Initalize(Maze _maze, int[] position)
        {
            maze = _maze;
            Position = position;
            maze.Level[Position[0], Position[1]] = 4;
        }

    }
}
