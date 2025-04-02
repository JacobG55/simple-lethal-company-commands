using GameNetcodeStuff;
using JLL.API;
using JLL.API.Compatability;
using System.Collections.Generic;

namespace SimpleCommands.Commands
{
    public class WeatherCommand : SimpleCommand
    {
        public static Dictionary<string, string> weatherAlias = new Dictionary<string, string>();

        public WeatherCommand() : base("weather", "changes weather")
        {
            instructions.Add("[/cmd] (clear | dusty | foggy | rainy | stormy | flooded | eclipsed)");
            if (JCompatabilityHelper.IsLoaded(JCompatabilityHelper.CachedMods.WeatherRegistry))
            {
                instructions.Add("{Check /weatherregistry for weather registry weather ids}");
            }

            weatherAlias.Add("clear", "none");
            weatherAlias.Add("dust", "dustclouds");
            weatherAlias.Add("dusty", "dustclouds");
            weatherAlias.Add("fog", "foggy");
            weatherAlias.Add("rain", "rainy");
            weatherAlias.Add("storm", "stormy");
            weatherAlias.Add("flood", "flooded");
            weatherAlias.Add("eclipse", "eclipsed");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (!StartOfRound.Instance.inShipPhase)
            {
                return "Can not change weather mid round!";
            }

            if (parameters.Count() >= 1)
            {
                string type = parameters.GetLowerCase();
                string moddedWeather = string.Empty;

                if (weatherAlias.TryGetValue(type, out string other)) type = other;

                if (SimpleCommandsBase.WRPresent)
                {
                    foreach (string name in JWeatherRegistryHelper.GetCustomWeatherNames())
                    {
                        if (name.ToLower().Replace(" ", "_").Equals(type))
                        {
                            moddedWeather = name;
                            break;
                        }
                    }
                }

                SelectableLevel selected = StartOfRound.Instance.currentLevel;

                if (moddedWeather.Equals(string.Empty))
                {
                    LevelWeatherType weatherType;
                    switch (type)
                    {
                        case "none":
                            weatherType = LevelWeatherType.None;
                            break;
                        case "dustclouds":
                            weatherType = LevelWeatherType.DustClouds;
                            break;
                        case "foggy":
                            weatherType = LevelWeatherType.Foggy;
                            break;
                        case "rainy":
                            weatherType = LevelWeatherType.Rainy;
                            break;
                        case "stormy":
                            weatherType = LevelWeatherType.Stormy;
                            break;
                        case "flooded":
                            weatherType = LevelWeatherType.Flooded;
                            break;
                        case "eclipsed":
                            weatherType = LevelWeatherType.Eclipsed;
                            break;

                        default: return $"Unknown Weather: {type}";
                    }

                    selected.currentWeather = weatherType;
                }
                else
                {
                    JWeatherRegistryHelper.ChangeWeather(moddedWeather, selected);
                }

                success = true;
                return "Set Weather for " + selected.PlanetName + " to " + type + ".";
            }
            return "";
        }
    }
}
