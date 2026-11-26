using WebSocketSharp;
using WebSocketSharp.Server;

namespace Yep
{

    internal class MultiplayerSession(GameState game) : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            int[] startingPos = [game.mazes[(int)game.startingSeed].Player_.PlayerBlip[0], game.mazes[(int)game.startingSeed].Player_.PlayerBlip[1]];
            game.PlayerInfo[ID] = (0, startingPos, startingPos, ID[0]);
            Send($"{NetworkCodes.ConnectionReceived}|{ID}");
            Send($"{NetworkCodes.Level}|{MultiplayerSerialization.ToString(game.mazes[(int)game.startingSeed].Level)}|{string.Join(',', game.mazes[(int)game.startingSeed].Player_.PlayerBlip)}");
            Send($"{NetworkCodes.Start}");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Sessions.BroadcastAsync($"{NetworkCodes.PeerDisconnect}|{e.Reason}", null);
            game.PlayerInfo.Remove(ID);
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
                case NetworkCodes.NextLevel:
                    int nextLevel = game.PlayerInfo[ID].Seed + 1;
                    Maze currentLevel = game.mazes[nextLevel - 1];

                    if (currentLevel.Level[game.PlayerInfo[ID].Position[0], game.PlayerInfo[ID].Position[1]] != 3)
                        break;

                    if (!game.mazes.ContainsKey(nextLevel))
                        game.NewLevel(nextLevel, currentLevel.Size + game.levelSizeIncrement);

                    Send($"{NetworkCodes.Level}|{MultiplayerSerialization.ToString(game.mazes[nextLevel].Level)}|{string.Join(',', game.mazes[nextLevel].Player_.PlayerBlip)}");
                    Send($"{NetworkCodes.Start}");

                    int[] startingPos = [game.mazes[nextLevel].Player_.PlayerBlip[0], game.mazes[nextLevel].Player_.PlayerBlip[1]];

                    game.PlayerInfo[ID] = (nextLevel, startingPos, startingPos, game.PlayerNames[ID][0]);

                    SendPlayer();

                    Console.WriteLine($"Player {game.PlayerNames[ID]} advanced to seed {nextLevel}");

                    break;

                case NetworkCodes.Move:

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
                    
                    break;

                case NetworkCodes.Name:
                    string name = splitMessage[1];
                    game.PlayerNames[ID] = name;
                    game.PlayerInfo[ID] = (game.PlayerInfo[ID].Seed, game.PlayerInfo[ID].Position, game.PlayerInfo[ID].OldPosition, name[0]);
                    Console.WriteLine($"Player {ID} renamed to {name}");
                    break;
            }
        }

        private void SendPlayer()
        {
            // sends its own pos
            foreach (var player in game.PlayerInfo)
            {
                if (player.Value.Seed == game.PlayerInfo[ID].Seed)
                {
                    Sessions.SendTo($"{NetworkCodes.PlayerPosition}|{game.PlayerInfo[ID].Position[0]},{game.PlayerInfo[ID].Position[1]}|{game.PlayerInfo[ID].OldPosition[0]},{game.PlayerInfo[ID].OldPosition[1]}|{ID}|{game.PlayerInfo[ID].Mover}", player.Key);
                }
            }
        }

    }
}