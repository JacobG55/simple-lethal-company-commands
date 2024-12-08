using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JLL.API;
using static JLL.JLL;
using Simple_Commands.Commands;
using Simple_Commands.Commands.Compatability;
using Simple_Commands.Patches;
using SimpleCommands.Commands;
using SimpleCommands.Managers;
using SimpleCommands.Patches;
using System.Reflection;

namespace SimpleCommands
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("JacobG5.JLL")]
    [BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    public class SimpleCommandsBase : BaseUnityPlugin
    {
        private const string modGUID = "JacobG5.SimpleCommands";
        private const string modName = "SimpleCommands";
        private const string modVersion = "1.5.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static SimpleCommandsBase SimpleCommandsInstance;

        private ManualLogSource mls;

        public static ConfigEntry<string> commandPrefix;
        public static ConfigEntry<bool> hostOnly;
        public static ConfigEntry<bool> hideDefault;

        void Awake()
        {
            if (SimpleCommandsInstance == null)
            {
                SimpleCommandsInstance = this;
            }

            hostOnly = Config.Bind("Main", "hostOnly", true, "Restricts commands to only being executed by the host.");
            hideDefault = Config.Bind("Main", "hideDefault", false, "Hides command feedback by default. ('hide' flag now shows command feedback.)");
            commandPrefix = Config.Bind("Main", "commandPrefix", "/", "Prefix for SimpleCommands");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            NetcodePatch(mls, Assembly.GetExecutingAssembly().GetTypes());
            Instance.networkObject.AddComponent<SimpleCommandsNetworkManager>();
            HarmonyPatch(harmony, mls, typeof(HUDManagerPatch), typeof(PlayerControllerBPatch), typeof(TerminalPatch));

            RegisterBaseCommands();
        }

        private void RegisterBaseCommands()
        {
            SimpleCommand.Register(new HelpCommand());
            SimpleCommand.Register(new ClearCommand());
            SimpleCommand.Register(new ListCommand());
            SimpleCommand.Register(new PayCommand());
            SimpleCommand.Register(new WeatherCommand());

            SimpleCommand.Register(new HealCommand());
            SimpleCommand.Register(new DamageCommand());
            SimpleCommand.Register(new InvulnerabilityCommand());
            SimpleCommand.Register(new InfiniteSprintCommand());
            SimpleCommand.Register(new TeleportCommand());

            SimpleCommand.Register(new ItemCommand());
            SimpleCommand.Register(new ItemsCommand());
            SimpleCommand.Register(new TerminalCommand());
            SimpleCommand.Register(new SimplePrefabs.PrefabCommand());
            SimpleCommand.Register(new SimplePrefabs.PrefabsCommand());

            SimpleCommand.Register(new SpawnCommand());
            SimpleCommand.Register(new EnemiesCommand());
            SimpleCommand.Register(new ChargeCommand());
            SimpleCommand.Register(new ExtendCommand());
            SimpleCommand.Register(new ExplodeCommand());

            SimpleCommand.Register(new PosCommand());

            if (JCompatabilityHelper.IsModLoaded.WeatherRegistry)
            {
                SimpleCommand.Register(new WeatherRegistryCommand());
            }
        }

        internal static void LogInfo(string message, JLogLevel level)
        {
            if (JLogHelper.AcceptableLogLevel(level))
            {
                SimpleCommandsInstance.mls.LogInfo(message);
            }
        }
    }
}
