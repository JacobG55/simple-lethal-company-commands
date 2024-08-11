using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

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
                SimpleCommandsBase.Instance.mls.LogInfo("Simple Command Manager Initialized.");
            }
        }
    }
}
