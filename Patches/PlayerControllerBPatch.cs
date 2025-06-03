using GameNetcodeStuff;
using HarmonyLib;
using LethalLib.Modules;
using SimpleCommands.Components;

namespace SimpleCommands.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void patchStart(PlayerControllerB __instance)
        {
            __instance.gameObject.AddComponent<PlayerModification>();
        }

        [HarmonyPrefix]
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPatch("KillPlayer")]
        public static bool patchDamagePlayer(PlayerControllerB __instance)
        {
            if (__instance.GetComponent<PlayerModification>().invulnerable)
            {
                __instance.health = 100;
                return false;
            }
            return true;
        }
    }
}
