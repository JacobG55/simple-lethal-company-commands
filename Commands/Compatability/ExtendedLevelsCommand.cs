using GameNetcodeStuff;
using JLL.API;
using LethalLevelLoader;
using System.Linq;

namespace SimpleCommands.Commands.Compatability
{
    public class ExtendedLevelsCommand : SimpleCommand
    {
        public ExtendedLevelsCommand() : base("levels", "LethalLevelLoader ExtendedLevels (iAmBatby)")
        {
            overrideShowOutput = true;
            permissionRequired = false;

            instructions.Add("[/cmd] - lists levels registered through LLL");
            instructions.Add("[/cmd] [page]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = true;
            if (!sender.IsLocalPlayer()) return "";
            ClearChat();
            return PagedList("Extended Levels:", PatchedContent.ExtendedLevels.Select((level) => level.NumberlessPlanetName).ToList(), parameters.IsEmpty() ? 0 : parameters.GetNumber(), 7);
        }
    }
}
