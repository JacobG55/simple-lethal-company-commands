using GameNetcodeStuff;
using LethalLib.Modules;
using SimpleCommands;
using SimpleCommands.Commands;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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

            SimpleCommandsBase.Instance.mls.LogInfo("Registering Base Game Prefabs.");

            Terminal terminal = Object.FindObjectOfType<Terminal>();
            RegisterSimplePrefab("Cruiser", new CruiserPrefab { prefab = terminal.buyableVehicles[0].vehiclePrefab, spawnOffset = new Vector3(0, 14, 0) });

            SimpleCommandsBase.Instance.mls.LogInfo($"Vanilla Map Hazards: {terminal.moonsCatalogueList[1].spawnableMapObjects.Length}");

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

            SimpleCommandsBase.Instance.mls.LogInfo($"LethalLib Map Hazards: {MapObjects.mapObjects.Count}");

            foreach (var mapObject in MapObjects.mapObjects)
            {
                RegisterSimplePrefab(mapObject.mapObject.prefabToSpawn.name, new SimplePrefab { prefab = mapObject.mapObject.prefabToSpawn });
            }
        }

        public class SimplePrefab
        {
            public GameObject prefab;
            public Vector3 spawnOffset = Vector3.zero;
            public virtual GameObject SpawnPrefab(Vector3 pos, Quaternion rot)
            {
                GameObject obj = GameObject.Instantiate(prefab);
                obj.transform.position = pos;
                obj.transform.rotation = rot;
                if (obj.TryGetComponent(out NetworkObject networkObject))
                {
                    networkObject.Spawn();
                }
                return obj;
            }
        }

        public class CruiserPrefab : SimplePrefab
        {
            public override GameObject SpawnPrefab(Vector3 pos, Quaternion rot)
            {
                GameObject obj = base.SpawnPrefab(pos, rot);
                if (obj.TryGetComponent(out VehicleController vehicleController))
                {
                    vehicleController.hasBeenSpawned = true;
                    vehicleController.inDropshipAnimation = false;
                }
                return obj;
            }
        }

        internal class PrefabCommand : SimpleCommand
        {
            public PrefabCommand() : base("prefab", "spawn registered prefabs")
            {
                instructions.Add("[/cmd] [name] - Spawns prefab at the player.");
                instructions.Add("[/cmd] [target] [name] - Spawns prefab at the target.");
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
                        PlayerControllerB? player = GetPlayer(name);
                        if (player != null)
                        {
                            position = player.transform.position;
                            name = parameters.GetString();
                        }
                        else
                        {
                            return UnknownPlayerException(name);
                        }
                    }

                    List<KeyValuePair<string, SimplePrefab>> foundMatches = new List<KeyValuePair<string, SimplePrefab>>();
                    foreach (KeyValuePair<string, SimplePrefab> prefab in Prefabs)
                    {
                        if (prefab.Key.ToLower().StartsWith(name.ToLower()))
                        {
                            foundMatches.Add(prefab);
                        }
                    }


                    if (foundMatches.Count > 0)
                    {
                        int smallest = 0;
                        for (int i = 0; i < foundMatches.Count; i++)
                        {
                            if (foundMatches[i].Key.Length < foundMatches[smallest].Key.Length)
                            {
                                smallest = i;
                            }
                        }

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
                int page = 0;
                if (!parameters.IsEmpty())
                {
                    page = parameters.GetNumber();
                }

                success = true;
                ClearChat();
                return PagedList("Spawnable Prefabs:", Prefabs.Keys.ToList(), page, 8);
            }
        }
    }
}
