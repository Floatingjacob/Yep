namespace Yep.Network
{
    public class CommandHandler
    {
        public record CommandInfo(
            string Name,
            string Description,
            string[] RequiredArguments,
            string[] OptionalArguments,
            Func<List<string>, Task> Action
            );

        private Monitor? Mon;

        internal Dictionary<string, Command> CommandRegistry = new(StringComparer.OrdinalIgnoreCase);

        internal CommandHandler(Monitor? mon = null)
        {
            Mon = mon;
            RegisterCommand("help", "Returns a list of commands with their usage and description.", (args) => { if (mon != null) mon.Push(GetHelp()); else c.colorPrintln(GetHelp()); return Task.CompletedTask; });
        }

        public void RegisterCommand(string name, string description, Func<List<string>, Task> action, string[]? requiredArguments = null, string[]? optionalArguments = null)
        {
            Command command = new() { Action = action, Name = name, Description = description, RequiredArguments = requiredArguments ?? [], OptionalArguments = optionalArguments ?? [] };
            CommandRegistry[name] = command;
        }

        public void RegisterCommands(params CommandInfo[] commands)
        {
            foreach (CommandInfo command in commands)
            {
                RegisterCommand(command.Name, command.Description, command.Action, command.RequiredArguments, command.OptionalArguments);
            }
        }

        public string GetHelp()
        {
            string result = "";

            foreach (var command in CommandRegistry)
                if (command.Value.Description != null)
                    result += $"\n**yellow**{command.Key}: **gray**{command.Value.Description}\n    Usage: **yellow**{command.Key} **dyellow**{string.Join(' ', command.Value.RequiredArguments.Select(x => $"<{x}>"))} **dgray**{string.Join(' ', command.Value.OptionalArguments.Select(x => $"[{x}]"))}";

            return result;
        }

        public async Task<bool> Dispatch(string commandString)
        {
            List<string>? processedCommand = Parse(commandString.Trim());

            if (processedCommand == null || processedCommand.Count == 0)
                return false;

            string command = processedCommand[0];

            Command? cmd;

            if (!CommandRegistry.TryGetValue(command, out cmd))
                return false;

            processedCommand.RemoveAt(0);

            if (cmd.RequiredArguments.Length > processedCommand.Count)
            {
                if (Mon != null) Mon.Push("\nMissing required arguments");
                else Console.WriteLine("\nMissing required arguments");
                return false;
            }

            if (processedCommand.Count > cmd.RequiredArguments.Length + cmd.OptionalArguments.Length)
            {
                if (Mon != null) Mon.Push("\nToo many arguments");
                else Console.WriteLine("\nToo many arguments");
                return false;
            }

            await cmd.Execute(processedCommand);
            return true;

        }


        private List<string>? Parse(string commandString)
        {

            List<string> result = [];
            string buffer = "";

            bool inString = false;

            int p = 0;

            while (p < commandString.Length)
            {

                switch (commandString[p])
                {
                    case '\\':
                        if (commandString.Length - 1 > p && commandString[p + 1] == '\"')
                        {
                            buffer += '\"';
                            p++;
                            break;
                        }
                        buffer += '\\';
                        break;

                    case '\"':
                        if (commandString.Length - 1 > p && commandString[p + 1] == '\"')
                            result.Add("");

                        inString = !inString;
                        break;

                    default:
                        if (char.IsWhiteSpace(commandString[p]))
                        {
                            if (inString)
                                buffer += commandString[p];
                            else if (buffer.Length > 0)
                            {
                                result.Add(buffer);
                                buffer = "";
                            }
                            break;
                        }
                        buffer += commandString[p];
                        break;
                }
                p++;
            }

            if (buffer.Length > 0)
            {
                result.Add(buffer);
                buffer = "";
            }

            if (inString)
            {
                if (Mon != null) Mon.Push("\nInvalid command, unterminated quote.");
                else Console.WriteLine("\nInvalid command, unterminated quote.");
                return null;
            }

            return result;

        }

        public class Command
        {
            required public string Name;
            public required Func<List<string>, Task> Action;
            public string? Description;
            public string[] RequiredArguments = [];
            public string[] OptionalArguments = [];

            internal async Task Execute(List<string> parameters)
            {
                await Action.Invoke(parameters);
            }
        }
    }
}
