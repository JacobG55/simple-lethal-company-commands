using GameNetcodeStuff;

namespace SimpleCommands.Commands
{
    public class ChargeCommand : SimpleCommand
    {
        public ChargeCommand() : base("charge", "charges held item")
        {
            instructions.Add("[/cmd]");
            instructions.Add("[/cmd] [target]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            PlayerControllerB effected = sender;

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
            }

            GrabbableObject held = effected.currentlyHeldObjectServer;

            if (held != null)
            {
                if (held.itemProperties.requiresBattery)
                {
                    held.insertedBattery = new Battery(isEmpty: false, 1f);
                }
            }

            success = true;
            return "Charged " + effected.playerUsername + "'s Item.";
        }
    }
}
