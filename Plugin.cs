using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JLL.API;
using LethalLib.Modules;
using Simple_Commands.Commands.Compatability;
using Simple_Commands.Patches;
using SimpleCommands.Commands;
using SimpleCommands.Managers;
using SimpleCommands.Patches;
using System.Reflection;
using UnityEngine;

namespace SimpleCommands
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("JacobG5.JLL")]
    [BepInDependency("evaisa.lethallib")]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    public class SimpleCommandsBase : BaseUnityPlugin
    {
        private const string modGUID = "JacobG5.SimpleCommands";
        private const string modName = "SimpleCommands";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static SimpleCommandsBase Instance;

        public ManualLogSource mls;

        public GameObject networkObject;

        public static ConfigEntry<string> commandPrefix;
        public static ConfigEntry<bool> hostOnly;
        public static ConfigEntry<bool> hideDefault;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            NetcodeRequired();

            hostOnly = Config.Bind("Main", "hostOnly", true, "Restricts commands to only being executed by the host.");
            hideDefault = Config.Bind("Main", "hideDefault", false, "Hides command feedback by default. ('hide' flag now shows command feedback.)");
            commandPrefix = Config.Bind("Main", "commandPrefix", "/", "Prefix for SimpleCommands");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            networkObject = NetworkPrefabs.CreateNetworkPrefab("SimpleCommandsNetworkManager");
            networkObject.AddComponent<SimpleCommandsNetworkManager>();

            harmony.PatchAll(typeof(HUDManagerPatch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(TerminalPatch));

            RegisterBaseCommands();
        }

        private void NetcodeRequired()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private void RegisterBaseCommands()
        {
            SimpleCommand.register(new HelpCommand());
            SimpleCommand.register(new ClearCommand());
            SimpleCommand.register(new ListCommand());
            SimpleCommand.register(new PayCommand());
            SimpleCommand.register(new WeatherCommand());

            SimpleCommand.register(new HealCommand());
            SimpleCommand.register(new InvulnerabilityCommand());
            SimpleCommand.register(new InfiniteSprintCommand());
            SimpleCommand.register(new ChargeCommand());
            SimpleCommand.register(new ExtendCommand());

            SimpleCommand.register(new ItemCommand());
            SimpleCommand.register(new ItemsCommand());
            SimpleCommand.register(new TeleportCommand());
            SimpleCommand.register(new TerminalCommand());
            // Spawn Monster w/ Raycast

            if (JCompatabilityHelper.IsModLoaded.WeatherRegistry)
            {
                SimpleCommand.register(new WeatherRegistryCommand());
            }
        }
    }
}
