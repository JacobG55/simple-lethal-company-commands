using GameNetcodeStuff;
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
            Prefabs.Add(name.Replace(' ', '_'), prefab);
        }

        public static void RegisterSimplePrefab(string name, GameObject prefab)
        {
            RegisterSimplePrefab(name, new SimplePrefab() { prefab = prefab });
        }

        internal static void RegisterBasePrefabs()
        {
            if (!registeredBasePrefabs)
            {
                SimpleCommandsBase.Instance.mls.LogInfo("Registering Base Game Prefabs.");

                Terminal terminal = Object.FindObjectOfType<Terminal>();
                RegisterSimplePrefab("cruiser", new CruiserPrefab { prefab = terminal.buyableVehicles[0].vehiclePrefab });
                
                registeredBasePrefabs = true;
            }
        }

        public class SimplePrefab
        {
            public GameObject prefab;
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
                instructions.Add("[/cmd] [name] - Spawns prefab above the player.");
                instructions.Add("[/cmd] [name] [x] [y] [z] - Spawns prefab at specified coordinates.");
            }

            public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
            {
                success = false;

                if (!parameters.IsEmpty())
                {
                    string name = parameters.GetString();

                    Vector3 position = sender.transform.position + new Vector3(0, 12, 0);

                    if (parameters.Count() >= 4)
                    {
                        if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos))
                        {
                            position = pos;
                        }
                        else return UnknownVectorException();
                    }

                    if (Prefabs.ContainsKey(name))
                    {
                        Prefabs[name].SpawnPrefab(position, sender.transform.rotation);

                        success = true;
                        return $"Spawned {name}!";
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
