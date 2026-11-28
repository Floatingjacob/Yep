using System.Diagnostics;

namespace Yep.Network.Server
{
    internal class Commands(Server server, GameState game)
    {
        CommandHandler cmh = new(server.mon);
        public void RegisterAll()
        {
            cmh.RegisterCommand("stop", "Shuts down the server.", (args) => { Stop(); return Task.CompletedTask; });
            cmh.RegisterCommand("join", "Joins the server in a new instance.", async (args) => Join());
            cmh.RegisterCommand("suck", "Teleports all players to the specified seed.", (args) => { Suck(args); return Task.CompletedTask; }, ["destination seed"], ["destination x", "destination y"]);
            cmh.RegisterCommand("password", "Returns the password if ran without an argument, otherwise changes the password.", (args) => { Password(args); return Task.CompletedTask; }, optionalArguments: ["\"password\""]);
        }

        public async Task<bool> TryRun(string commandString)
        {
            return await cmh.Dispatch(commandString);
        }

        private void Password(List<string> args)
        {
            if (args.Count > 0)
            {
                server.password = args[0];
                c.colorPrintln($"\n**green**Server password is now \"{server.password ?? ""}\"");
            }
            else c.colorPrintln($"\nThe server password is **red**\"{server.password ?? ""}\"");
        }

        private void Stop()
        {
            server.mon.Push("\nDisconnecting all clients and shutting down server");
            foreach (Session client in game.Clients.ToArray())
                client.Disconnect("Server shutting down");

            server.wssv.Stop();
            server.mon.Push("Server stopped. Press any key to return to the main menu...");
            Console.ReadKey();
        }

        private void Suck(List<string> args)
        {
            if (args.Count == 1)
            {
                if (int.TryParse(args[0], out int seed))
                {
                    string message = $"\nAll players were sent to seed {seed}";

                    foreach (Session client in game.Clients)
                    {

                        client.send($"{NetworkCodes.Message}|{message}");
                        client.ChangeLevel(seed);
                    }
                    server.mon.Push(message);
                }
                else
                    server.mon.Push("\nAll arguments must be valid whole numbers");
                return;
            }

            if (args.Count == 2)
            {
                server.mon.Push("\nYou must supply both an X and a Y value if you wish to set a destination tile");
                return;
            }

            if (args.Count == 3)
            {
                if (int.TryParse(args[0], out int seed) && int.TryParse(args[1], out int x) && int.TryParse(args[2], out int y))
                {
                    string message = $"\nAll players were sent to position ({x}, {y}) on seed {seed}";
                    foreach (Session client in game.Clients)
                    {

                        client.send($"{NetworkCodes.Message}|{message}");
                        client.ChangeLevel(seed, x, y);
                    }
                    
                    server.mon.Push(message);
                }
                else
                    server.mon.Push("\nAll arguments must be valid whole numbers");
                return;
            }
        }
        private void Join()
        {
            if (!OperatingSystem.IsWindows())
            {
                server.mon.Push("Your platform does not currently support using the JOIN command.\nPlease manually launch a separate instance and enter \"localhost\" as the ip.");
                return;
            }
            string path = Path.GetFullPath(Environment.GetCommandLineArgs()[0]);

#if DEBUG
            if (path.Contains(".dll")) // Only for development
                path = path.Replace(".dll", ".exe");
#endif

            Process p = new();
            ProcessStartInfo psi = new() { FileName = path, UseShellExecute = true };

            psi.ArgumentList.Add("localhost");
            psi.ArgumentList.Add(server.password);
            psi.ArgumentList.Add("Host");

            p.StartInfo = psi;
            p.Start();
        }

    }
}
