namespace Yep
{
    internal class GameState
    {

        internal uint startingSeed = 0;
        internal uint maxLevelSize = 59;
        internal uint startingLevelSize = 5;
        internal uint levelSizeIncrement = 2;
        internal bool enemiesEnabled = true;

        internal Dictionary<int, Maze> mazes = new();
        internal Dictionary<string, string> PlayerNames = new();
        internal Dictionary<string, (int Seed, int[] Position, int[] OldPosition, char Mover)> PlayerInfo = new();

        internal void NewLevel(int seed, uint size)
        {
            Maze level = new Maze
            {
                Seed = seed,
                Size = size,
                EnemiesEnabled = enemiesEnabled
            };

            level.Size = Math.Min(maxLevelSize, level.Size);

            level.Generate(level.Size);

            mazes[seed] = level;
        }

    }
}
