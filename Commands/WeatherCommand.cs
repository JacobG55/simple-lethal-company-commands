using GameNetcodeStuff;

namespace SimpleCommands.Commands
{
    public class WeatherCommand : SimpleCommand
    {
        public WeatherCommand() : base("weather", "changes weather")
        {
            instructions.Add("[/cmd] (clear | dusty | foggy | rainy | stormy | eclipsed)");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (parameters.Count() >= 1)
            {
                string type = parameters.GetLowerCase();

                LevelWeatherType weatherType;
                switch (type)
                {
                    case ("clear"):
                        weatherType = LevelWeatherType.None;
                        break;
                    case ("dust"):
                        weatherType = LevelWeatherType.DustClouds;
                        break;
                    case ("fog"):
                        weatherType = LevelWeatherType.Foggy;
                        break;
                    case ("rain"):
                        weatherType = LevelWeatherType.Rainy;
                        break;
                    case ("storm"):
                        weatherType = LevelWeatherType.Stormy;
                        break;
                    case ("eclipse"):
                        weatherType = LevelWeatherType.Eclipsed;
                        break;
                    case ("foggy"):
                        weatherType = LevelWeatherType.Foggy;
                        break;
                    case ("rainy"):
                        weatherType = LevelWeatherType.Rainy;
                        break;
                    case ("stormy"):
                        weatherType = LevelWeatherType.Stormy;
                        break;
                    case ("eclipsed"):
                        weatherType = LevelWeatherType.Eclipsed;
                        break;

                    default:
                        return "";
                }

                SelectableLevel selected = StartOfRound.Instance.currentLevel;

                selected.currentWeather = weatherType;

                success = true;
                return "Set Weather for " + selected.PlanetName + " to " + type + ".";
            }
            return "";
        }
    }
}
