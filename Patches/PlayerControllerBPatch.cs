using GameNetcodeStuff;
using HarmonyLib;
using SimpleCommands.Components;
using UnityEngine;

namespace SimpleCommands.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        public static void patchStart(PlayerControllerB __instance)
        {
            __instance.gameObject.AddComponent<PlayerModification>();
        }

        [HarmonyPatch("KillPlayer")]
        [HarmonyPrefix]
        public static bool patchKillPlayer(PlayerControllerB __instance)
        {
            if (__instance.TryGetComponent<PlayerModification>(out PlayerModification playerMod)) 
            {
                if (playerMod.invulnerable)
                {
                    __instance.health += 100;
                    return false;
                }
            }
            return true;
        }
    }
}
