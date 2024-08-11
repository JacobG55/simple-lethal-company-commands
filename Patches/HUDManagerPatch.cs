using GameNetcodeStuff;
using HarmonyLib;
using SimpleCommands.Commands;
using SimpleCommands.Managers;
using Unity.Netcode;

namespace SimpleCommands.Patches
{
    [HarmonyPatch(typeof(HUDManager))]
    internal class HUDManagerPatch
    {
        [HarmonyPatch("AddTextToChatOnServer")]
        [HarmonyPrefix]
        public static bool patchAddTextToChatOnServer(string chatMessage, int playerId, HUDManager __instance)
        {
            if (playerId != -1)
            {
                PlayerControllerB sender = __instance.playersManager.allPlayerScripts[playerId];
                string prefix = SimpleCommand.GetPrefix();

                if (chatMessage.StartsWith(prefix))
                {
                    string payload = chatMessage.Substring(prefix.Length);

                    payload = payload.Replace("&//=", "").Replace(prefix, "&//=");

                    if (sender.IsHost || sender.IsServer)
                    {
                        SimpleCommandsNetworkManager.Instance.CommandExecutionClientRpc(playerId, payload, SimpleCommandsBase.hideDefault.Value);
                    }
                    else
                    {
                        SimpleCommandsBase.Instance.mls.LogInfo("Sending Command Request.");
                        SimpleCommandsNetworkManager.Instance.RequestCommandExecutionServerRpc(playerId, payload);
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
