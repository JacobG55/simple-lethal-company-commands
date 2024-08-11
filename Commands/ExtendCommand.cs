using GameNetcodeStuff;

namespace SimpleCommands.Commands
{
    public class ExtendCommand : SimpleCommand
    {
        public ExtendCommand() : base("extend", "extends quota") 
        {
            instructions.Add("[/cmd] - Extends Quota 1 Day");
            instructions.Add("[/cmd] [days]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            int days;

            if (parameters.IsEmpty())
            {
                days = 1;
            }
            else
            {
                days = parameters.GetNumber();
            }

            if (days != 0)
            {
                TimeOfDay timeOfDay = TimeOfDay.Instance;

                timeOfDay.timeUntilDeadline += timeOfDay.totalTime * days;
                timeOfDay.UpdateProfitQuotaCurrentTime();

                success = true;
                return "Extended Deadline " + days + " day(s).";
            }

            success = false;
            return "";
        }
    }
}
