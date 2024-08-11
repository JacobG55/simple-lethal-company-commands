using GameNetcodeStuff;
using SimpleCommands.Components;

namespace SimpleCommands.Commands
{
    public class InfiniteSprintCommand : SimpleCommand
    {
        public InfiniteSprintCommand() : base("stamina", "toggle infinate sprint") 
        {
            instructions.Add("[/cmd]");
            instructions.Add("[/cmd] [target]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            PlayerControllerB effected = sender;
            success = false;

            if (!parameters.IsEmpty())
            {
                string name = parameters.GetStringAt(0);
                PlayerControllerB? player = GetPlayer(name);

                if (player != null)
                {
                    effected = player;
                }
                else
                {
                    return UnknownPlayerException(name);
                }
            }

            if (toggleInfiniteStamina(effected, out bool newValue))
            {
                success = true;
                return "Infinite Stamina for " + effected.playerUsername + " set to " + newValue;
            }

            return "";
        }

        private bool toggleInfiniteStamina(PlayerControllerB player, out bool newValue)
        {
            if (player.TryGetComponent<PlayerModification>(out PlayerModification mod))
            {
                newValue = !mod.infinateSprint;
                mod.infinateSprint = newValue;
                return true;
            }
            newValue = false;
            return false;
        }
    }
}
