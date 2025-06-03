using GameNetcodeStuff;
using JLL.API;
using System.Linq;
using WeatherRegistry;

namespace SimpleCommands.Commands.Compatability
{
    public class WeatherRegistryCommand : SimpleCommand
    {
        public WeatherRegistryCommand() : base("weatherregistry", "WeatherRegistry (mrov)")
        {
            overrideShowOutput = true;
            permissionRequired = false;

            instructions.Add("[/cmd] - lists weathers registered through WeatherRegistry");
            instructions.Add("[/cmd] [page]");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = true;
            if (!sender.IsLocalPlayer()) return "";
            ClearChat();
            return PagedList("WeatherRegistry:", WeatherManager.Weathers.Select((weather) => weather.name.Replace(" ", "_")).ToList(), parameters.IsEmpty() ? 0 : parameters.GetNumber(), 7);
        }
    }
}
