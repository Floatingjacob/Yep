namespace Yep
{
    internal static class NetworkCodes
    {
        internal const int ConnectionReceived = 0x00; // A "Hello"
        internal const int Heartbeat = 0x01;
        internal const int Message = 0x02;
        internal const int Level = 0x03; // Maze data
        internal const int Start = 0x04;
        internal const int NextLevel = 0x05;
        internal const int Move = 0x06; 
        internal const int PlayerPosition = 0x07;
        internal const int UpdateTile = 0x08;
        internal const int Name = 0x09;
        internal const int Score = 0x0A;
        internal const int GetPlayers = 0x0B;
        internal const int PeerDisconnect = 0x0C;
        internal const int Password = 0x0D;
        internal const int Forbidden = 0x0E;
    }
}
