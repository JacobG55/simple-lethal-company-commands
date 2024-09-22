using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using JLL.API;

namespace SimpleCommands.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void spawnNetworkManager(StartOfRound __instance)
        {
            if (__instance.IsHost || __instance.IsServer)
            {
                GameObject obj = GameObject.Instantiate(SimpleCommandsBase.Instance.networkObject);
                obj.GetComponent<NetworkObject>().Spawn();

                SimpleCommandsBase.LogInfo("Simple Command Manager Initialized.", JLogLevel.Debuging);
            }
        }
    }
}
