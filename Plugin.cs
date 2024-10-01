using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JLL.API;
using LethalLib.Modules;
using Simple_Commands.Commands;
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
        private const string modVersion = "1.3.1";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static SimpleCommandsBase Instance;

        private ManualLogSource mls;

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

            hostOnly = Config.Bind("Main", "hostOnly", true, "Restricts commands to only being executed by the host.");
            hideDefault = Config.Bind("Main", "hideDefault", false, "Hides command feedback by default. ('hide' flag now shows command feedback.)");
            commandPrefix = Config.Bind("Main", "commandPrefix", "/", "Prefix for SimpleCommands");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            NetcodeRequired(mls);
            networkObject = NetworkPrefabs.CreateNetworkPrefab("SimpleCommandsNetworkManager");
            networkObject.AddComponent<SimpleCommandsNetworkManager>();

            harmony.PatchAll(typeof(HUDManagerPatch));
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            harmony.PatchAll(typeof(StartOfRoundPatch));
            harmony.PatchAll(typeof(TerminalPatch));

            RegisterBaseCommands();
        }

        private static void NetcodeRequired(ManualLogSource logSource)
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                try
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
                catch
                {
                    logSource.LogInfo("Skipping Netcode Class");
                }
            }
            logSource.LogInfo("Netcode Successfully Patched!");
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

            SimpleCommand.Register(new ChargeCommand());
            SimpleCommand.Register(new ExtendCommand());
            SimpleCommand.Register(new PosCommand());
            SimpleCommand.Register(new ExplodeCommand());
            // Spawn Monster w/ Raycast

            if (JCompatabilityHelper.IsModLoaded.WeatherRegistry)
            {
                SimpleCommand.Register(new WeatherRegistryCommand());
            }
        }

        internal static void LogInfo(string message, JLogLevel level)
        {
            if (JLogHelper.AcceptableLogLevel(level))
            {
                Instance.mls.LogInfo(message);
            }
        }
    }
}
