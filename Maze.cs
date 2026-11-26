namespace Yep
{
    internal class Maze
    {
        internal int Seed = 0;
        internal Random Rand = null!;
        internal bool EnemiesEnabled = true;
        internal uint Size;
        internal int[,] Level = new int[,] { };
        private List<Enemy> Enemies = [];
        internal Player Player_ = new();

        internal void Generate(uint size = 31)
        {
            Rand = new(Seed);

            int[] builder = [0, 0];

            Size = size;

            Initialize(size);

            builder[0] = Rand.Next(0, (int)size) | 1; // Random odd-numbered starting position for the builder
            if (builder[0] > size - 1) builder[0] -= 2;
            else if (builder[0] < 0) builder[0] += 2;
            builder[1] = Rand.Next(0, (int)size) | 1;
            if (builder[1] > size - 1) builder[1] -= 2;
            else if (builder[1] < 0) builder[1] += 2;

            Level[builder[0], builder[1]] = 2; // 2 is entrance
            Player_.Initialize(this);
            Player_.PlayerBlip = [builder[0], builder[1]];

            Stack<int[]> path = new();

            path.Push([builder[0], builder[1]]);

            while (path.Count > 0)
            {

                List<int> possibleMoves = [];

                if (builder[0] + 2 < size && Level[builder[0] + 2, builder[1]] == -1)
                    possibleMoves.Add(1);

                if (builder[0] - 2 >= 0 && Level[builder[0] - 2, builder[1]] == -1)
                    possibleMoves.Add(2);

                if (builder[1] + 2 < size && Level[builder[0], builder[1] + 2] == -1)
                    possibleMoves.Add(3);

                if (builder[1] - 2 >= 0 && Level[builder[0], builder[1] - 2] == -1)
                    possibleMoves.Add(4);

                if (possibleMoves.Count < 1)
                {
                    int[]? value;

                    if (path.TryPop(out value))
                        builder = value;

                    continue;
                }

                int next = Rand.Next(0, possibleMoves.Count);
                int nextPossibleMove = possibleMoves[next];

                int wallX = builder[0];
                int wallY = builder[1];

                switch (nextPossibleMove)
                {
                    case 1:
                        wallX += 1;
                        builder[0] += 2; // Move Right
                        break;
                    case 2:
                        wallX -= 1;
                        builder[0] -= 2; // Move Left
                        break;
                    case 3:
                        wallY += 1;
                        builder[1] += 2; // Move Up
                        break;
                    case 4:
                        wallY -= 1;
                        builder[1] -= 2; // Move Down
                        break;
                }

                Level[wallX, wallY] = 5;

                Level[builder[0], builder[1]] = 5;

                path.Push([builder[0], builder[1]]);
            }

            Season(size);

            PlaceExit([Player_.PlayerBlip[0], Player_.PlayerBlip[1]]);
        }

        internal void Season(uint size)
        {
            for (int y = 0; y < size; y++) // y
            {
                for (int x = 0; x < size; x++) // x
                {
                    if (x == 0 || x == size - 1 || y == 0 || y == size - 1) continue; // Don't break the border

                    int randN = Rand.Next(0, 1001);

                    if (Level[x, y] == -1 && 900 <= randN) // Breaks a wall
                    {                                      // Need to check if there is a path in between cells
                        Level[x, y] = 5;
                    }
                    if (EnemiesEnabled && Level[x, y] == 5 && 990 <= randN && Enemies.Count < Math.Floor(size / 3.0)) // Spawns an enemy
                    {
                        Enemy enemy = new();
                        enemy.Initalize(this, [x, y]);
                        Enemies.Add(enemy);
                    }

                }
            }
        }

        internal void PlaceExit(int[] start)
        {
            List<int[]> queue = new();
            HashSet<(int, int)> visited = new();
            int[] farthestTile = [start[0], start[1], 0];

            queue.Add([..start, 1]);
            visited.Add((start[0], start[1]));

            while (queue.Count > 0)
            {
                if (queue[0][0] + 1 < Size && CanExplore(Level[queue[0][0] + 1, queue[0][1]]) && !visited.Contains((queue[0][0] + 1, queue[0][1])))
                {
                    queue.Add([queue[0][0] + 1, queue[0][1], queue[0][2] + 1]);
                    visited.Add((queue[0][0] + 1, queue[0][1]));
                }

                if (queue[0][0] - 1 >= 0 && CanExplore(Level[queue[0][0] - 1, queue[0][1]]) && !visited.Contains((queue[0][0] - 1, queue[0][1])))
                {
                    queue.Add([queue[0][0] - 1, queue[0][1], queue[0][2] + 1]);
                    visited.Add((queue[0][0] - 1, queue[0][1]));
                }

                if (queue[0][1] + 1 < Size && CanExplore(Level[queue[0][0], queue[0][1] + 1]) && !visited.Contains((queue[0][0], queue[0][1] + 1)))
                {
                    queue.Add([queue[0][0], queue[0][1] + 1, queue[0][2] + 1]);
                    visited.Add((queue[0][0], queue[0][1] + 1));
                }

                if (queue[0][1] - 1 >= 0 && CanExplore(Level[queue[0][0], queue[0][1] - 1]) && !visited.Contains((queue[0][0], queue[0][1] - 1)))
                {
                    queue.Add([queue[0][0], queue[0][1] - 1, queue[0][2] + 1]);
                    visited.Add((queue[0][0], queue[0][1] - 1));
                }

                if (queue[0][2] > farthestTile[2])
                {
                    farthestTile[0] = queue[0][0];
                    farthestTile[1] = queue[0][1];
                    farthestTile[2] = queue[0][2];
                }

                //Console.WriteLine(queue[0][0] + ", " + queue[0][1] + ", " + queue[0][2]);
                queue.RemoveAt(0);
            }

            Level[farthestTile[0], farthestTile[1]] = 3;

        }

        internal static bool CanExplore(int type)
        {
            switch (type)
            {
                case -1: return false; // Wall
                case 0: return true;   // Empty space
                case 2: return false;  // Entrance
                case 3: return false;  // Exit
                case 4: return true;   // Enemy
                case 5: return true;   // Point
                default: return false;
            }
        }

        internal static Tuple<char, int> getChar(int type)
        {
            switch (type)
            {
                case -1: // Wall
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    return new('#', 2);
                case 0: // Pathway
                    Console.ForegroundColor = ConsoleColor.White;
                    return new(' ', 2);
                case 1: // Player
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    return new('@', 1);
                case 2: // Entrance
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    return new('E', 2);
                case 3: // Exit
                    Console.ForegroundColor = ConsoleColor.Green;
                    return new('X', 2);
                case 4: // Enemy
                    Console.ForegroundColor = ConsoleColor.Red;
                    return new('&', 1);
                case 5: // Points
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    return new('.', 1);
                default: // Unknown
                    Console.ForegroundColor = ConsoleColor.Red;
                    return new('?', 1);
            }
        }

        internal void Draw()
        {
            Console.Clear();

            if (Seed % 5 == 0) // Incentive, idk bro
                Sound.Beep((500, 0.4), (750, 0.2), (0, 0.4), (750, 0.2), (0, 0.4), (500, 0.4), (700, 0.2), (0, 0.4), (700, 0.2), (0, 0.4), (500, 0.4), (680, 0.2), (0, 0.4), (680, 0.2), (0, 0.4), (500, 0.4), (650, 1.0));

            for (int y = 0; y < Size; y++)
            {

                for (int x = 0; x < Size; x++)
                {
                    var char_ = getChar(Level[x, y]);

                    Console.SetCursorPosition(x * 2, y);
                    Console.Write(new string(char_.Item1, char_.Item2));

                }
            }
        }

        internal void Initialize(uint size)
        {
            Level = new int[size, size];
            Enemies.Clear();
            Player_ = new();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Level[x, y] = -1; // -1 = wall
                }
            }
        }

        internal void UpdateEnemies()
        {
            foreach (Enemy enemy in Enemies)
            {
                enemy.Move();
            }
        }

    }
}
