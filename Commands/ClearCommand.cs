using GameNetcodeStuff;
using JLL.API;

namespace SimpleCommands.Commands
{
    public class ClearCommand : SimpleCommand
    {
        public ClearCommand() : base("clear", "clears chat")
        {
            instructions.Add("[/cmd] - clears chat");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = true;
            ClearChat();
            JHudHelper.ClearDisplayTipQueue();
            return "";
        }
    }
}
