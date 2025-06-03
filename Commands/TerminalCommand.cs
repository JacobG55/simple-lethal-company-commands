using GameNetcodeStuff;
using SimpleCommands.Patches;

namespace SimpleCommands.Commands
{
    public class TerminalCommand : SimpleCommand
    {
        public TerminalCommand() : base("terminal", "Send Terminal Codes") 
        {
            instructions.Add("[/cmd] [code] - Used for opening doors / disabling hazards.");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            Terminal terminal = GetTerminal();
            if (terminal == null)
            {
                return MissingTerminal();
            }

            if (!parameters.IsEmpty())
            {
                string code = parameters.GetString();
                TerminalPatch.terminalObjectRequest(terminal, code);
                success = true;
                return "";
            }

            return "";
        }
    }
}
