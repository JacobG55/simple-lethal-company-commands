using GameNetcodeStuff;
using JLL.API;
using JLL.API.LevelProperties;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleCommands.Commands
{
    public class TeleportCommand : SimpleCommand
    {
        public TeleportCommand() : base("tp", "teleports players") 
        {
            instructions.Add("[/cmd] [destination] - Teleports Self");
            instructions.Add("[/cmd] (ship | main | exit{#}) - Teleports Self");
            instructions.Add("[/cmd] [target] [destination]");
            instructions.Add("[/cmd] [target] (ship | main | exit{#})");
            instructions.Add("[/cmd] [x] [y] [z] - Teleports self");
            instructions.Add("[/cmd] [target] [x] [y] [z]");

            tagInfo.Add("'Animate':\nTeleport instantly skipping teleport animation.");
            tagInfo.Add("'Inside':\nSets the teleported player inside the facility.");
            tagInfo.Add("'Outside':\nSets the teleported player outside the facility.");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            string teleportedUser = "";
            string destinationName = "";

            string name1;
            PlayerControllerB? player1;
            string name2;
            PlayerControllerB? player2;

            bool instant = !parameters.isFlagged("animate");
            success = false;

            bool isInside = sender.isInsideFactory;
            if (parameters.isFlagged("inside"))
            {
                isInside = true;
            }
            else if (parameters.isFlagged("outside"))
            {
                isInside = false;
            }

            switch (parameters.Count()) {
                case 1:
                    name1 = parameters.GetString();
                    player1 = GetPlayer(name1);
                    if (player1 != null)
                    {
                        teleportedUser = sender.playerUsername;
                        destinationName = player1.playerUsername;
                        TeleportPlayer(sender, player1.transform.position, instant, isInside);
                        break;
                    }
                    else
                    {
                        if (GetSpecialLocation(name1, out Vector3 pos, out destinationName, ref isInside))
                        {
                            teleportedUser = sender.playerUsername;
                            TeleportPlayer(sender, pos, instant, isInside);
                            break;
                        }
                        else if (destinationName != "")
                        {
                            return $"{destinationName} could not be found.";
                        }
                    }
                    return UnknownPlayerException(name1);

                case 2:
                    name1 = parameters.GetString();
                    player1 = GetPlayer(name1);

                    if (player1 == null)
                    {
                        return UnknownPlayerException(name1);
                    }

                    name2 = parameters.GetString();
                    player2 = GetPlayer(name2);

                    if (player2 == null)
                    {
                        if (GetSpecialLocation(name2, out Vector3 pos, out destinationName, ref isInside))
                        {
                            teleportedUser = player1.playerUsername;
                            TeleportPlayer(player1, pos, instant, isInside);
                            break;
                        }
                        else if (destinationName != "")
                        {
                            return $"{destinationName} not found.";
                        }

                        return UnknownPlayerException(name2);
                    }

                    teleportedUser = sender.playerUsername;
                    destinationName = player1.playerUsername;
                    TeleportPlayer(player1, player2.transform.position, instant, isInside);

                    break;

                case 3:
                    if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos1))
                    {
                        teleportedUser = sender.playerUsername;
                        destinationName = pos1.x + ", " + pos1.y + ", " + pos1.z;
                        TeleportPlayer(sender, pos1, instant, isInside);
                        break;
                    }
                    return UnknownVectorException();

                case 4:
                    name1 = parameters.GetString();
                    player1 = GetPlayer(name1);
                    if (player1 != null)
                    {
                        if (parameters.GetRelativeVector(player1.transform.position, out Vector3 pos2))
                        {
                            teleportedUser = player1.playerUsername;
                            destinationName = pos2.x + ", " + pos2.y + ", " + pos2.z;
                            TeleportPlayer(player1, pos2, instant, isInside);
                            break;
                        }
                        return UnknownVectorException();
                    }
                    return UnknownPlayerException(name1);

                default: break;
            }

            if (teleportedUser != "")
            {
                success = true;
                return "Teleporting " + teleportedUser + " to " + destinationName;
            }

            return "";
        }

        private bool GetSpecialLocation(string name, out Vector3 pos, out string formalName, ref bool isInside)
        {
            if (name == "ship")
            {
                isInside = false;
                pos = new Vector3(0, 0, -14);
                formalName = "Autopilot Ship";
                return true;
            }

            if (name == "breaker")
            {
                BreakerBox breakerBox = Object.FindObjectOfType<BreakerBox>();
                formalName = "Breaker Box";
                isInside = true;
                if (breakerBox != null)
                {
                    pos = NavMeshSnap(breakerBox.transform.position);
                    return true;
                }
                pos = Vector3.zero;
                return false;
            }

            if (name == "apparatus")
            {
                LungProp[] lungs = Object.FindObjectsOfType<LungProp>();
                isInside = true;
                formalName = "Apparatus";
                foreach (LungProp lung in lungs)
                {
                    if (lung.isLungDocked)
                    {
                        pos = NavMeshSnap(lung.transform.position);
                        return true;
                    }
                }
                pos = Vector3.zero;
                return false;
            }

            int entrance = -1;

            if (name == "main")
            {
                entrance = 0;
            }
            else if (name.StartsWith("exit"))
            {
                if (int.TryParse(name[4..], out int num)) 
                {
                    entrance = num;
                }
            }

            if (entrance >= 0)
            {
                Vector3? entrancePosition = JLevelPropertyRegistry.GetEntranceTeleportLocation(entrance, !isInside, true);
                formalName = entrance == 0 ? "Main Entrance" : $"Fire Exit #{entrance}";
                if (entrancePosition != null)
                {
                    pos = entrancePosition.Value;
                    return true;
                }
                pos = Vector3.zero;
                return false;
            }

            string numString = string.Empty;
            for (int i = name.Length-1; i > 0; i--)
            {
                if (char.IsNumber(name[i]))
                {
                    numString = name[i] + numString;
                }
                else
                {
                    break;
                }
            }

            int index = 0;
            if (int.TryParse(numString, out int result))
            {
                index = result;
            }

            name = name.Remove(name.Length - numString.Length, numString.Length);
            SimpleCommandsBase.LogInfo($"POI Parse: '{name}' '{numString}' {index}", JLogLevel.Debuging);
            if (JLevelPropertyRegistry.HasPOI(name))
            {
                formalName = name;
                if (JLevelPropertyRegistry.GetPOI(name, index, out Vector3 found))
                {
                    pos = NavMeshSnap(found);
                    return true;
                }
                pos = Vector3.zero;
                return false;
            }

            formalName = "";
            pos = Vector3.zero;
            return false;
        }

        private Vector3 NavMeshSnap(Vector3 pos)
        {
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 3, NavMesh.AllAreas))
            {
                return hit.position;
            }
            return pos;
        }

        private void TeleportPlayer(PlayerControllerB player, Vector3 pos, bool instant, bool isInside)
        {
            if (instant)
            {
                player.isInsideFactory = isInside;
                player.TeleportPlayer(pos);
            }
            else
            {
                player.StartCoroutine(beamUpPlayer(player, pos, isInside));
            }
        }

        private IEnumerator beamUpPlayer(PlayerControllerB player, Vector3 pos, bool isInside)
        {
            if (player == null)
            {
                Debug.Log("Targeted player is null");
                yield break;
            }

            player.shipTeleporterId = 1;

            player.beamUpParticle.Play();

            /*
            SimpleCommandsBase.Instance.mls.LogInfo("Loading Teleport Sound...");
            AudioClip teleportSound = Resources.Load<AudioClip>("Audio/AudioClips/ShipTeleporterBeamPlayerBody");

            if (teleportSound != null)
            {
                SimpleCommandsBase.Instance.mls.LogInfo(teleportSound.name + " found!");

                player.movementAudio.PlayOneShot(teleportSound);
            }
            */

            yield return new WaitForSeconds(3f);

            /*
                if ((bool)Object.FindObjectOfType<AudioReverbPresets>())
                {
                    Object.FindObjectOfType<AudioReverbPresets>().audioPresets[3].ChangeAudioReverbForPlayer(player);
                }

                player.isInElevator = true;
                player.isInHangarShipRoom = true;
                player.isInsideFactory = false;
            */

            player.isInsideFactory = isInside;

            player.averageVelocity = 0f;
            player.velocityLastFrame = Vector3.zero;
            player.TeleportPlayer(pos);

            player.shipTeleporterId = -1;
        }
    }
    public class PosCommand : SimpleCommand
    {
        public PosCommand() : base("pos", "shows player position")
        {
            permissionRequired = false;
            instructions.Add("[/cmd] - Shows Player Position");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = true;

            if (sender.IsLocalPlayer())
            {
                JHudHelper.QueueDisplayTip($"Pos: {sender.transform.position}", $"Rot: {sender.transform.rotation}");
                SimpleCommandsBase.LogInfo($"Pos: {sender.transform.position} | Rot: {sender.transform.rotation}", JLogLevel.Debuging);
            }

            return "";
        }
    }
}
