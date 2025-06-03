using GameNetcodeStuff;
using System;
using static SimpleCommands.Commands.SimpleCommand;

namespace SimpleCommands.Commands
{
    internal class ActionCommand(string name, string description, Action<CommandParameters> action) : SimpleCommand(name, description)
    {
        public readonly Action<CommandParameters> Action = action;

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            Action.Invoke(parameters);
            success = true;
            return string.Empty;
        }
    }
}
