using GameNetcodeStuff;
using HarmonyLib;
using SimpleCommands.Commands;
using SimpleCommands.Managers;
using JLL.API;
using UnityEngine;

namespace SimpleCommands.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        [HarmonyPatch("AddTextToChatOnServer")]
        [HarmonyPrefix]
        public static bool patchAddTextToChatOnServer(ref string chatMessage, int playerId, HUDManager __instance)
        {
            if (playerId != -1)
            {
                PlayerControllerB sender = __instance.playersManager.allPlayerScripts[playerId];
                string prefix = SimpleCommand.GetPrefix();

                if (chatMessage.StartsWith(prefix))
                {
                    string payload = chatMessage.Substring(prefix.Length);

                    payload = payload.Replace("&//=", "").Replace(prefix, "&//=");

                    Vector3 targetPos = sender.transform.position;

                    if (Physics.Raycast(new Ray(sender.gameplayCamera.transform.position, sender.gameplayCamera.transform.forward), out RaycastHit hit, 200, 1073742656))
                    {
                        targetPos = hit.point;
                    }

                    if (sender.IsHost || sender.IsServer)
                    {
                        SimpleCommandsNetworkManager.Instance.CommandExecutionClientRpc(playerId, payload, SimpleCommandsBase.hideDefault.Value, targetPos);
                    }
                    else
                    {
                        SimpleCommandsBase.LogInfo("Sending Command Request.", JLogLevel.User);
                        SimpleCommandsNetworkManager.Instance.RequestCommandExecutionServerRpc(playerId, payload, targetPos);
                    }
                    chatMessage = string.Empty;
                    return false;
                }
            }
            return true;
        }
    }
}
