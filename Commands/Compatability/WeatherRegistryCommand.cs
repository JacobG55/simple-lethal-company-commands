using GameNetcodeStuff;
using SimpleCommands.Commands;
using System.Collections.Generic;
using WeatherRegistry;

namespace Simple_Commands.Commands.Compatability
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
            List<string> weatherIds = new List<string>();
            
            foreach (Weather weather in WeatherManager.Weathers)
            {
                weatherIds.Add(weather.name.Replace(" ", "_"));
            }

            int page = 0;
            if (!parameters.IsEmpty())
            {
                page = parameters.GetNumber();
            }

            success = true;
            ClearChat();
            return PagedList("WeatherRegistry:", weatherIds, page, 7);
        }
    }
}
