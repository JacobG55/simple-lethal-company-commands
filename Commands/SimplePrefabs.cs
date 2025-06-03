using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using JLL.API;
using JLL.API.LevelProperties;
using SimpleCommands.Managers;

namespace SimpleCommands.Commands
{
    public class SimplePrefabs
    {
        internal static readonly Dictionary<string, SimplePrefab> Prefabs = new Dictionary<string, SimplePrefab>();
        private static bool registeredBasePrefabs = false;

        public static void RegisterSimplePrefab(string name, SimplePrefab prefab)
        {
            if (!Prefabs.ContainsKey(name))
            {
                Prefabs.Add(name.Replace(' ', '_'), prefab);
            }
        }

        public static void RegisterSimplePrefab(string name, GameObject prefab)
            => RegisterSimplePrefab(name, new SimplePrefab(prefab));

        internal static void RegisterBasePrefabs()
        {
            if (registeredBasePrefabs) return;
            registeredBasePrefabs = true;

            SimpleCommandsBase.LogInfo("Registering Base Game Prefabs.", JLogLevel.Debuging);

            Terminal terminal = JLevelPropertyRegistry.GetTerminal();
            RegisterSimplePrefab("Cruiser", new CruiserPrefab(terminal.buyableVehicles[0].vehiclePrefab) {spawnOffset = new Vector3(0, 14, 0)});

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
                RegisterSimplePrefab(name, new SimplePrefab(mapObject.prefabToSpawn, PrefabSource.Hazard));
            }

            if (JCompatabilityHelper.IsLoaded(JCompatabilityHelper.CachedMods.LethalLib)) 
                LethalLibCompatibility.RegisterLethalLibPrefabs();
        }

        public class SimplePrefab
        {
            public GameObject prefab;
            public Vector3 spawnOffset = Vector3.zero;
            public readonly PrefabSource source;

            public SimplePrefab(GameObject prefab) : this(prefab, PrefabSource.Custom) { }

            internal SimplePrefab(GameObject prefab, PrefabSource source)
            {
                this.prefab = prefab;
                this.source = source;
            }

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

        public class CruiserPrefab(GameObject prefab) : SimplePrefab(prefab, PrefabSource.Vehicle)
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

        public enum PrefabSource
        {
            Custom,
            Vehicle,
            Hazard,
        }
    }
}
