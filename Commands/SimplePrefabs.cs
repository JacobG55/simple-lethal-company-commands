using GameNetcodeStuff;
using SimpleCommands;
using SimpleCommands.Commands;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using JLL.API;
using JLL.API.LevelProperties;
using Simple_Commands.Managers;
using LethalLib.Modules;

namespace Simple_Commands.Commands
{
    public class SimplePrefabs
    {
        private static readonly Dictionary<string, SimplePrefab> Prefabs = new Dictionary<string, SimplePrefab>();
        private static bool registeredBasePrefabs = false;

        public static void RegisterSimplePrefab(string name, SimplePrefab prefab)
        {
            if (!Prefabs.ContainsKey(name))
            {
                Prefabs.Add(name.Replace(' ', '_'), prefab);
            }
        }

        public static void RegisterSimplePrefab(string name, GameObject prefab)
        {
            RegisterSimplePrefab(name, new SimplePrefab() { prefab = prefab });
        }

        internal static void RegisterBasePrefabs()
        {
            if (registeredBasePrefabs) return;
            registeredBasePrefabs = true;

            SimpleCommandsBase.LogInfo("Registering Base Game Prefabs.", JLogLevel.Debuging);

            Terminal terminal = JLevelPropertyRegistry.GetTerminal();
            RegisterSimplePrefab("Cruiser", new CruiserPrefab { prefab = terminal.buyableVehicles[0].vehiclePrefab, spawnOffset = new Vector3(0, 14, 0) });

            SimpleCommandsBase.LogInfo($"Vanilla Map Hazards: {terminal.moonsCatalogueList[1].spawnableMapObjects.Length}", JLogLevel.Debuging);

            foreach (var mapObject in terminal.moonsCatalogueList[1].spawnableMapObjects)
            {
                string name = mapObject.prefabToSpawn.name;
                name = name switch
                {
                    "SpikeRoofTrapHazard" => "SpikeRoofTrap",
                    "TurretContainer" => "Turret",
                    _ => name,
                };
                RegisterSimplePrefab(name, new SimplePrefab { prefab = mapObject.prefabToSpawn });
            }

            if (JCompatabilityHelper.IsLoaded(JCompatabilityHelper.CachedMods.LethalLib)) 
                LethalLibCompatibility.RegisterLethalLibPrefabs();
        }

        public class SimplePrefab
        {
            public GameObject prefab;
            public Vector3 spawnOffset = Vector3.zero;

            public void SpawnPrefab(Vector3 pos, Quaternion rot)
            {
                if (prefab.GetComponent<NetworkObject>())
                {
                    if (RoundManager.Instance.IsServer || RoundManager.Instance.IsClient)
                    {
                        GameObject obj = GameObject.Instantiate(prefab);
                        SetPrefabProperties(ref obj, pos, rot);
                        obj.GetComponent<NetworkObject>().Spawn();
                        return;
                    }
                }
                else
                {
                    GameObject obj = GameObject.Instantiate(prefab);
                    SetPrefabProperties(ref obj, pos, rot);
                }
            }

            public virtual void SetPrefabProperties(ref GameObject prefab, Vector3 pos, Quaternion rot)
            {
                prefab.transform.position = pos;
                prefab.transform.rotation = rot;
            }
        }

        public class CruiserPrefab : SimplePrefab
        {
            public override void SetPrefabProperties(ref GameObject prefab, Vector3 pos, Quaternion rot)
            {
                base.SetPrefabProperties(ref prefab, pos, rot);
                if (prefab.TryGetComponent(out VehicleController vehicleController))
                {
                    vehicleController.hasBeenSpawned = true;
                    vehicleController.inDropshipAnimation = false;
                }
            }
        }

        internal class PrefabCommand : SimpleCommand
        {
            public PrefabCommand() : base("prefab", "spawn registered prefabs")
            {
                instructions.Add("[/cmd] [name] - Spawns prefab at the player.");
                instructions.Add("[/cmd] [name] [target] - Spawns prefab at the target.");
                instructions.Add("[/cmd] [name] [x] [y] [z] - Spawns prefab at specified coordinates.");
            }

            public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
            {
                success = false;

                if (!parameters.IsEmpty())
                {
                    string name = parameters.GetString();

                    Vector3 position = sender.transform.position;
                    bool customPos = false;

                    if (parameters.Count() >= 4)
                    {
                        if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos))
                        {
                            position = pos;
                            customPos = true;
                        }
                        else return UnknownVectorException();
                    }
                    else if (parameters.Count() >= 2)
                    {
                        string playerName = parameters.GetString();
                        PlayerControllerB? player = GetPlayer(playerName);
                        if (player != null)
                        {
                            position = player.transform.position;
                        }
                        else
                        {
                            return UnknownPlayerException(playerName);
                        }
                    }

                    List<KeyValuePair<string, SimplePrefab>> foundMatches = new List<KeyValuePair<string, SimplePrefab>>();
                    int smallest = 0;

                    foreach (KeyValuePair<string, SimplePrefab> prefab in Prefabs)
                    {
                        if (prefab.Key.ToLower().StartsWith(name.ToLower()))
                        {
                            foundMatches.Add(prefab);
                            if (prefab.Key.Length < foundMatches[smallest].Key.Length) smallest = foundMatches.Count - 1;
                        }
                    }

                    if (foundMatches.Count > 0)
                    {
                        if (Prefabs.ContainsKey(foundMatches[smallest].Key))
                        {
                            SimplePrefab prefab = Prefabs[foundMatches[smallest].Key];
                            prefab.SpawnPrefab(position + (customPos ? Vector3.zero : prefab.spawnOffset), sender.transform.rotation);

                            success = true;
                            return $"Spawned {foundMatches[smallest].Key}!";
                        }
                    }
                    return $"Unknown Prefab: {name}";
                }
                return "";
            }
        }

        internal class PrefabsCommand : SimpleCommand
        {
            public PrefabsCommand() : base("prefabs", "list spawnable prefabs")
            {
                instructions.Add("[/cmd] - Lists spawnable prefabs");
                instructions.Add("[/cmd] [page]");
            }

            public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
            {
                success = true;
                if (!sender.IsLocalPlayer()) return "";
                ClearChat();
                return PagedList("Spawnable Prefabs:", Prefabs.Keys.ToList(), parameters.IsEmpty() ? 0 : parameters.GetNumber(), 8);
            }
        }
    }
}
