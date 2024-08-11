using GameNetcodeStuff;

namespace SimpleCommands.Commands
{
    public class HealCommand : SimpleCommand
    {
        public HealCommand() : base("heal", "heals players")
        {
            instructions.Add("[/cmd]");
            instructions.Add("[/cmd] [target]");
            instructions.Add("[/cmd] [target] [value]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            PlayerControllerB effected = sender;
            int amount = 100;

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
                    success = false;
                    return UnknownPlayerException(name);
                }

                if (parameters.Count() > 1)
                {
                    int num = parameters.GetNumberAt(1, out bool isNumber);
                    if (isNumber)
                    {
                        amount = num;
                    }
                }
            }

            effected.DamagePlayer(-amount);

            success = true;
            return "Healed " + effected.playerUsername + ".";
        }
    }
}
