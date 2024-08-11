using GameNetcodeStuff;

namespace SimpleCommands.Commands
{
    public class PayCommand : SimpleCommand
    {
        public PayCommand() : base("pay", "free credits") 
        {
            instructions.Add("[/cmd] - Grants 100 credits");
            instructions.Add("[/cmd] [value]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            int value;
            success = false;

            Terminal terminal = GetTerminal();
            if (terminal == null)
            {
                return MissingTerminal();
            }

            if (parameters.IsEmpty())
            {
                value = 100;
            } else
            {
                value = parameters.GetNumber();
            }

            if (value != 0)
            {
                terminal.groupCredits += value;

                success = true;
                return "Received " + value + " Credits.";
            }

            return "";
        }
    }
}
