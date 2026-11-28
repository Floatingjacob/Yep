namespace Yep.Network.Client
{
    internal class Commands(Client client, Monitor mon)
    {
        CommandHandler cmh = new(mon);
        public void RegisterAll()
        {
            cmh.RegisterCommand("name", "Shows or changes your display name.", (args) => { Name(args); return Task.CompletedTask; }, optionalArguments: ["new name"]);
            cmh.RegisterCommand("close", "Closes the monitor and returns to the pause menu.", (args) => { return Task.CompletedTask; }); // Dummy command
        }

        public async Task<bool> TryRun(string commandString)
        {
            return await cmh.Dispatch(commandString);
        }

        private void Name(List<string> args)
        {
            if (args.Count == 0)
                mon.Push($"\nYour name is **yellow**\"{client.Name}\"**white**");
            else
            {
                if (args[0].Length == 0)
                {
                    mon.Push("\n**red**Your name cannot be empty**white**");
                    return;
                }
                client.Name = args[0];
                client.Send($"{NetworkCodes.Name}|{args[0]}");
            }
        }
    }
}
