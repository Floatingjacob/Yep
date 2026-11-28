using WebSocketSharp;
using WebSocketSharp.Server;

namespace Yep
{

    internal class MultiplayerSession(GameState game, MultiplayerServer server) : WebSocketBehavior
    {
        bool authenticated = false;
        protected override void OnOpen()
        {
            game.Clients.Add(this);
            Send($"{NetworkCodes.ConnectionReceived}|{ID}");

            if (string.IsNullOrWhiteSpace(server.password))
                Initialize();
            else Send($"{NetworkCodes.Password}");
        }

        private void Initialize()
        {
            authenticated = true;
            int[] startingPos = [game.mazes[(int)game.startingSeed].Player_.PlayerBlip[0], game.mazes[(int)game.startingSeed].Player_.PlayerBlip[1]];
            game.PlayerInfo[ID] = (0, startingPos, startingPos, ID[0]);
            game.PlayerScores[ID] = 0;
            Send($"{NetworkCodes.Name}");
            Send($"{NetworkCodes.Level}|{MultiplayerSerialization.ToString(game.mazes[(int)game.startingSeed].Level)}|{string.Join(',', game.mazes[(int)game.startingSeed].Player_.PlayerBlip)}");
            Send($"{NetworkCodes.Start}");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            game.Clients.Remove(this);
            game.PlayerNames.TryGetValue(ID, out string? name);

            string message = $"Player \"{name ?? "Anonymous"}\" disconnected with reason \"{e.Reason}\"";

            Console.WriteLine(message);
            Sessions.Broadcast($"{NetworkCodes.Message}|{message}");
            try
            {
                foreach (var player in game.PlayerInfo)
                    if (player.Value.Seed == game.PlayerInfo[ID].Seed && player.Key != ID)
                        Sessions.SendTo($"{NetworkCodes.PeerDisconnect}|{game.PlayerInfo[ID].Position[0]},{game.PlayerInfo[ID].Position[1]}|{ID}", player.Key);

                game.PlayerInfo.Remove(ID);
                game.PlayerNames.Remove(ID);
                game.PlayerScores.Remove(ID);
            }
            catch (KeyNotFoundException) { }; // shhh

        }

        protected override void OnMessage(MessageEventArgs e)
        {
            string[] splitMessage = e.Data.Split('|');
            int type = int.Parse(splitMessage[0]);

            switch (type)
            {
                case NetworkCodes.Heartbeat:
                    Send($"{NetworkCodes.Heartbeat}");
                    Console.WriteLine("Received a heartbeat from a client");
                    break;
                case NetworkCodes.Password:
                    if (splitMessage[1] == server.password)
                        Initialize();
                    else Send($"{NetworkCodes.Password}");
                    break;
                case NetworkCodes.NextLevel:
                    if (!authenticated)
                    {
                        Send($"{NetworkCodes.Forbidden}|You cannot request levels right now");
                        break;
                    }
                    int nextLevel = game.PlayerInfo[ID].Seed + 1;

                    Maze currentLevel = game.mazes[nextLevel - 1];

                    if (currentLevel.Level[game.PlayerInfo[ID].Position[0], game.PlayerInfo[ID].Position[1]] != 3)
                        break;

                    ChangeLevel(nextLevel);

                    string message = $"Player {game.PlayerNames[ID]} advanced to seed {nextLevel}";

                    Console.WriteLine(message);
                    Sessions.Broadcast($"{NetworkCodes.Message}|{message}");

                    break;

                case NetworkCodes.Move:
                    if (!authenticated)
                    {
                        Send($"{NetworkCodes.Forbidden}|You cannot move right now.");
                        break;
                    }
                    (int Seed, int[] Position, int[] OldPosition, char Mover) playerInfo = game.PlayerInfo[ID];

                    switch ((Player.Direction)int.Parse(splitMessage[1]))
                    {
                        case Player.Direction.Left:
                            if (playerInfo.Position[0] > 0 && Player.CanMoveTo(game.mazes[playerInfo.Seed].Level[playerInfo.Position[0] - 1, playerInfo.Position[1]]))
                                game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0] - 1, playerInfo.Position[1]], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            else game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0], playerInfo.Position[1]], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            break;
                        case Player.Direction.Right:
                            if (playerInfo.Position[0] < game.mazes[playerInfo.Seed].Size && Player.CanMoveTo(game.mazes[playerInfo.Seed].Level[playerInfo.Position[0] + 1, playerInfo.Position[1]]))
                                game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0] + 1, playerInfo.Position[1]], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            else game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0], playerInfo.Position[1]], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            break;
                        case Player.Direction.Up:
                            if (playerInfo.Position[1] > 0 && Player.CanMoveTo(game.mazes[playerInfo.Seed].Level[playerInfo.Position[0], playerInfo.Position[1] - 1]))
                                game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0], playerInfo.Position[1] - 1], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            else game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0], playerInfo.Position[1]], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            break;
                        case Player.Direction.Down:
                            if (playerInfo.Position[1] < game.mazes[playerInfo.Seed].Size && Player.CanMoveTo(game.mazes[playerInfo.Seed].Level[playerInfo.Position[0], playerInfo.Position[1] + 1]))
                                game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0], playerInfo.Position[1] + 1], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            else game.PlayerInfo[ID] = (playerInfo.Seed, [playerInfo.Position[0], playerInfo.Position[1]], [playerInfo.Position[0], playerInfo.Position[1]], playerInfo.Mover);
                            break;
                    }

                    SendPlayer();

                    switch(game.mazes[playerInfo.Seed].Level[game.PlayerInfo[ID].Position[0], game.PlayerInfo[ID].Position[1]])
                    {
                        case 5:
                            game.PlayerScores[ID]++;
                            UpdateTile(playerInfo.Seed, game.PlayerInfo[ID].Position[0], game.PlayerInfo[ID].Position[1], 0);
                            Send($"{NetworkCodes.Score}|{game.PlayerScores[ID]}");
                            break;
                    }

                    game.mazes[playerInfo.Seed].UpdateEnemies(this);

                    break;
                case NetworkCodes.Message:
                    Sessions.Broadcast($"{NetworkCodes.Message}|{game.PlayerNames[ID]}: {splitMessage[1]}");
                    break;
                case NetworkCodes.Name:
                    if (!authenticated)
                    {
                        Send($"{NetworkCodes.Forbidden}|You cannot change your name right now.");
                        break;
                    }
                    string name = splitMessage[1];
                    game.PlayerNames[ID] = name;
                    game.PlayerInfo[ID] = (game.PlayerInfo[ID].Seed, game.PlayerInfo[ID].Position, game.PlayerInfo[ID].OldPosition, name[0]);

                    string message_ = $"Player {ID} renamed to {name}";

                    Console.WriteLine(message_);
                    Sessions.Broadcast($"{NetworkCodes.Message}|{message_}");

                    break;
                case NetworkCodes.GetPlayers:
                    if (!authenticated)
                    {
                        Send($"{NetworkCodes.Forbidden}|You cannot request players right now.");
                        break;
                    }
                    foreach (var player in game.PlayerInfo)
                    {
                        if (player.Value.Seed == game.PlayerInfo[ID].Seed)
                        {
                            Send($"{NetworkCodes.PlayerPosition}|{player.Value.Position[0]},{player.Value.Position[1]}|{player.Value.OldPosition[0]},{player.Value.OldPosition[1]}|{player.Key}|{player.Value.Mover}");
                        }
                    }
                    break;
            }
        }

        internal void UpdateTile(int seed, int x, int y, int newTile)
        {
            //Console.WriteLine($"Changing tile ({x}, {y}) in seed {seed} to {newTile}");

            game.mazes[seed].Level[x, y] = newTile;

            foreach (var player in game.PlayerInfo)
            {
                if (player.Value.Seed == game.PlayerInfo[ID].Seed)
                {
                    Sessions.SendTo($"{NetworkCodes.UpdateTile}|{x},{y}|{newTile}", player.Key);
                }
            }
        }

        internal void ChangeLevel(int targetSeed, int? x = null, int? y = null)
        {
            
            if (!game.mazes.ContainsKey(targetSeed))
                game.NewLevel(targetSeed, game.startingLevelSize + (uint)Math.Abs(targetSeed - game.startingSeed) * game.levelSizeIncrement);

            Send($"{NetworkCodes.Level}|{MultiplayerSerialization.ToString(game.mazes[targetSeed].Level)}|{string.Join(',', game.mazes[targetSeed].Player_.PlayerBlip)}");
            Send($"{NetworkCodes.Start}");

            int[] startingPos = [game.mazes[targetSeed].Player_.PlayerBlip[0], game.mazes[targetSeed].Player_.PlayerBlip[1]];

            game.PlayerInfo[ID] = (targetSeed, [x ?? startingPos[0], y ?? startingPos[1]], [x ?? startingPos[0], y ?? startingPos[1]], game.PlayerNames[ID][0]);

            SendPlayer();
        }

        internal void send(string data)
        {
            Send(data);
        }

        private void SendPlayer()
        {
            // Broadcast the current connection's player to all other players on the same seed
            foreach (var player in game.PlayerInfo)
            {
                if (player.Value.Seed == game.PlayerInfo[ID].Seed)
                {
                    Sessions.SendTo($"{NetworkCodes.PlayerPosition}|{game.PlayerInfo[ID].Position[0]},{game.PlayerInfo[ID].Position[1]}|{game.PlayerInfo[ID].OldPosition[0]},{game.PlayerInfo[ID].OldPosition[1]}|{ID}|{game.PlayerInfo[ID].Mover}", player.Key);
                }
            }
        }

        internal void Disconnect(string reason)
        {
            Close(0u, reason);
        }

    }
}