using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JLL.API;
using static JLL.JLL;
using SimpleCommands.Commands;
using SimpleCommands.Commands.Compatability;
using SimpleCommands.Patches;
using SimpleCommands.Managers;
using System.Reflection;
using JLL.API.LevelProperties;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace SimpleCommands
{
    [BepInPlugin(modGUID, modName, modVersion)]
    [BepInDependency("JacobG5.JLL")]
    [BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mrov.WeatherRegistry", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
    public class SimpleCommandsBase : BaseUnityPlugin
    {
        private const string modGUID = "JacobG5.SimpleCommands";
        private const string modName = "SimpleCommands";
        private const string modVersion = "1.7.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static SimpleCommandsBase SimpleCommandsInstance;

        public static bool LLLPresent { get; private set; } = false;
        public static bool WRPresent { get; private set; } = false;

        private ManualLogSource mls;

        public static ConfigEntry<string> commandPrefix;
        public static ConfigEntry<bool> hostOnly;
        public static ConfigEntry<bool> hideDefault;
        public static ConfigEntry<int> spawnCap;

        void Awake()
        {
            if (SimpleCommandsInstance == null)
            {
                SimpleCommandsInstance = this;
            }

            hostOnly = Config.Bind("Main", "hostOnly", true, "Restricts commands to only being executed by the host.");
            hideDefault = Config.Bind("Main", "hideDefault", false, "Hides command feedback by default. ('hide' flag now shows command feedback.)");
            spawnCap = Config.Bind("Main", "spawnCap", 20, "Cap for spawn commands.");
            commandPrefix = Config.Bind("Main", "commandPrefix", "/", "Prefix for SimpleCommands");

            mls = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            NetcodePatch(mls, Assembly.GetExecutingAssembly().GetTypes());
            Instance.networkObject.AddComponent<SimpleCommandsNetworkManager>();
            HarmonyPatch(harmony, mls, typeof(HUDManagerPatch), typeof(PlayerControllerBPatch), typeof(TerminalPatch));

            LLLPresent = JCompatabilityHelper.IsLoaded("imabatby.lethallevelloader");
            WRPresent = JCompatabilityHelper.IsLoaded("mrov.WeatherRegistry");

            RegisterBaseCommands();
        }

        private void RegisterBaseCommands()
        {
            SimpleCommand.Register(new HelpCommand());
            SimpleCommand.Register(new ClearCommand());
            SimpleCommand.Register(new ListCommand());
            SimpleCommand.Register(new TeleportCommand());
            SimpleCommand.Register(new PayCommand());
            SimpleCommand.Register(new RouteCommand());

            SimpleCommand.Register(new AllCheatsCommand());
            SimpleCommand.Register(new FlyCommand());
            SimpleCommand.Register(new InvulnerabilityCommand());
            SimpleCommand.Register(new InfiniteSprintCommand());
            SimpleCommand.Register(new SpeedCommand());
            SimpleCommand.Register(new TerminalCommand());

            //SimpleCommand.Register(new ItemCommand());
            //SimpleCommand.Register(new ItemsCommand());
            SimpleCommand.Register(new SpawnCommand<Item>("item", "spawns items", "items", "Spawnable Items", [("Store", "Filters to show store items"), ("Scrap", "Filters to show scrap items")],
                (item) => item.itemName,
                (filters) =>
                {
                    bool storeItems = filters.Contains("Store");
                    bool scrapItems = filters.Contains("Scrap");
                    if ((storeItems || scrapItems) && storeItems != scrapItems)
                    {
                        return storeItems ? JLevelPropertyRegistry.GetTerminal().buyableItemsList : StartOfRound.Instance.currentLevel.spawnableScrap.Select((x) => x.spawnableItem);
                    }
                    return RoundManager.Instance.playersManager.allItemsList.itemsList;
                },
                (item, pos, parameters) =>
                {
                    GameObject obj = Instantiate(item.spawnPrefab, pos, Quaternion.identity);
                    GrabbableObject spawned = obj.GetComponent<GrabbableObject>();
                    spawned.fallTime = 0f;
                    if (item.isScrap)
                    {
                        spawned.SetScrapValue(Mathf.RoundToInt(UnityEngine.Random.Range(item.minValue, item.maxValue) * RoundManager.Instance.scrapValueMultiplier));
                    }
                    spawned.GetComponent<NetworkObject>().Spawn();
                }
            ));
            //SimpleCommand.Register(new SimplePrefabs.PrefabCommand());
            //SimpleCommand.Register(new SimplePrefabs.PrefabsCommand());
            SimpleCommand.Register(new SpawnCommand<KeyValuePair<string, SimplePrefabs.SimplePrefab>>("prefab", "spawns prefabs", "prefabs", "Spawnable Prefabs",
                Enum.GetNames(typeof(SimplePrefabs.PrefabSource)).Select((name) => (name, $"Filters to show {name.ToLower()} prefabs")).ToArray(),
                (prefabEntry) => prefabEntry.Key,
                (filters) =>
                {
                    if (filters.Length == 0) return SimplePrefabs.Prefabs;
                    IEnumerable<SimplePrefabs.PrefabSource> whitelist = Enum.GetNames(typeof(SimplePrefabs.PrefabSource)).Where(filters.Contains).Select(Enum.Parse<SimplePrefabs.PrefabSource>);
                    return SimplePrefabs.Prefabs.Where((x) => whitelist.Contains(x.Value.source));
                },
                (prefabEntry, pos, parameters) => prefabEntry.Value.SpawnPrefab(parameters.sender.transform.position == pos ? (pos + prefabEntry.Value.spawnOffset) : pos, parameters.sender.transform.rotation)
            ));
            //SimpleCommand.Register(new SpawnCommand());
            //SimpleCommand.Register(new EnemiesCommand());
            SimpleCommand.Register(new SpawnCommand<EnemyType>("spawn", "spawns enemies", "enemies", "Spawnable Enemies", [("Indoor", "Filters to show interior enemies"), ("Outdoor", "Filters to show exterior enemies")],
                (enemy) => enemy.enemyName,
                (filters) =>
                {
                    bool indoor = filters.Contains("Indoor");
                    bool outdoor = filters.Contains("Outdoor");
                    if ((indoor || outdoor) && indoor != outdoor)
                    {
                        mls.LogInfo($"{indoor} {outdoor}");
                        return JLevelPropertyRegistry.AllSortedEnemies.Where((enemy) => (enemy.isOutsideEnemy || enemy.isDaytimeEnemy) == outdoor);
                    }
                    return JLevelPropertyRegistry.AllSortedEnemies;
                },
                (enemy, pos, parameters) => RoundManager.Instance.SpawnEnemyGameObject(pos, 0, 0, enemy), true
            ));

            SimpleCommand.Register(new HealCommand());
            SimpleCommand.Register(new DamageCommand());
            SimpleCommand.Register(new ExplodeCommand());
            SimpleCommand.Register(new ExtendCommand());
            SimpleCommand.Register(new ChargeCommand());
            SimpleCommand.Register(new PosCommand());

            SimpleCommand.Register(new WeatherCommand());
            if (WRPresent) SimpleCommand.Register(new WeatherRegistryCommand());
            if (LLLPresent) SimpleCommand.Register(new ExtendedLevelsCommand());
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
