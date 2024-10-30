using GameNetcodeStuff;
using JLL.API;
using Simple_Commands.Commands;
using SimpleCommands.Commands;
using Unity.Netcode;
using UnityEngine;

namespace SimpleCommands.Managers
{
    public class SimpleCommandsNetworkManager : NetworkBehaviour
    {
        public static SimpleCommandsNetworkManager Instance;

        public void Awake()
        {
            Instance = this;

            SimplePrefabs.RegisterBasePrefabs();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestCommandExecutionServerRpc(int playerId, string commandMessage, Vector3 targetPos)
        {
            SimpleCommandsBase.LogInfo($"[Server] Processing Command Request from {StartOfRound.Instance.allPlayerScripts[playerId].playerUsername}.", JLogLevel.User);

            if (SimpleCommand.tryParseCommand(commandMessage, out SimpleCommand? command, out SimpleCommand.CommandParameters? parameters, targetPos))
            {
                if (command == null || parameters == null)
                {
                    return;
                }

                if (command.permissionRequired && SimpleCommandsBase.hostOnly.Value)
                {
                    SendErrorTipClientRpc(playerId, "Permission Denied", "Only the host is allowed to execute commands due to the server's config.", true);
                    return;
                }
            }

            CommandExecutionClientRpc(playerId, commandMessage, SimpleCommandsBase.hideDefault.Value, targetPos);
        }

        [ClientRpc]
        public void CommandExecutionClientRpc(int playerId, string commandMessage, bool hideDefault, Vector3 targetPos)
        {
            CommandExecution(StartOfRound.Instance.allPlayerScripts[playerId], commandMessage, hideDefault, targetPos);
        }

        private void CommandExecution(PlayerControllerB sender, string commandMessage, bool hideDefault, Vector3 targetPos)
        {
            HUDManager hudManager = HUDManager.Instance;
            SimpleCommandsBase.LogInfo($"Parsing: {commandMessage}", JLogLevel.Debuging);

            bool isSender = SimpleCommand.IsClient(sender);

            string[] cmds = commandMessage.Split("&//=");

            foreach(string cmd in cmds)
            {
                if (cmd == "") continue;
                if (SimpleCommand.tryParseCommand(cmd, out SimpleCommand? command, out SimpleCommand.CommandParameters? parameters, targetPos))
                {
                    if (command == null || parameters == null)
                    {
                        return;
                    }

                    string append = parameters.IsEmpty() ? "" : $"\n{parameters.asString()}";
                    SimpleCommandsBase.LogInfo($"Player: {sender.playerUsername} | Executing: {SimpleCommand.GetPrefix() + command.name}{append}", JLogLevel.Debuging);

                    string result = command.Execute(sender, parameters, out bool success);

                    if (result != null && result != "")
                    {
                        SimpleCommandsBase.LogInfo($"\nSuccess: {success}\nResult: {result}", JLogLevel.Debuging);
                        if (success)
                        {
                            if (hudManager.IsHost || hudManager.IsServer)
                            {
                                if ((parameters.isFlagged("hide") == hideDefault) || command.overrideShowOutput)
                                {
                                    hudManager.AddTextToChatOnServer(result);
                                }
                            }
                        }
                        else if (isSender)
                        {
                            JHudHelper.QueueDisplayTip("Command Failure:", result);
                        }
                    }
                }
                else if (isSender)
                {
                    JHudHelper.QueueDisplayTip("Unknown Command:", $"{SimpleCommand.GetPrefix()}{cmd.Split(' ')[0]}", true);
                }
            }
        }

        [ClientRpc]
        public void SendErrorTipClientRpc(int playerId, string title, string description, bool isError)
        {
            HUDManager hud = HUDManager.Instance;
            if (hud.localPlayer.actualClientId == hud.playersManager.allPlayerScripts[playerId].actualClientId)
            {
                hud.DisplayTip(title, description, isError);
            }
        }
    }
}
