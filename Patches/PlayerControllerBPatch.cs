using GameNetcodeStuff;
using HarmonyLib;
using SimpleCommands.Components;

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
            return CheckInvulnerability(__instance);
        }

        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static bool patchDamagePlayer(PlayerControllerB __instance)
        {
            return CheckInvulnerability(__instance);
        }

        public static bool CheckInvulnerability(PlayerControllerB player)
        {
            if (player.GetComponent<PlayerModification>().invulnerable)
            {
                player.health = 100;
                return false;
            }
            return true;
        }
    }
}
