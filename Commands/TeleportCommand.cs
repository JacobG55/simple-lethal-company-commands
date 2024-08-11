using GameNetcodeStuff;
using LethalLib.Modules;
using System.Collections;
using UnityEngine;

namespace SimpleCommands.Commands
{
    public class TeleportCommand : SimpleCommand
    {
        public TeleportCommand() : base("tp", "teleports players") 
        {
            instructions.Add("[/cmd] [destination] - Teleports Self");
            instructions.Add("[/cmd] (ship | main) - Teleports Self");
            instructions.Add("[/cmd] [target] [destination]");
            instructions.Add("[/cmd] [target] (ship | main)");
            instructions.Add("[/cmd] [x] [y] [z] - Teleports self");
            instructions.Add("[/cmd] [target] [x] [y] [z]");

            tagInfo.Add("'Instant':\nTeleport instantly skipping teleport animation.");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            string teleportedUser = "";
            string destinationName = "";

            string name1;
            PlayerControllerB? player1;
            string name2;
            PlayerControllerB? player2;

            bool instant = parameters.isFlagged("instant");
            success = false;

            switch (parameters.Count()) {
                case 1:
                    name1 = parameters.GetString();
                    player1 = GetPlayer(name1);
                    if (player1 != null)
                    {
                        teleportedUser = sender.playerUsername;
                        destinationName = player1.playerUsername;
                        TeleportPlayer(sender, player1.transform.position, instant);
                        break;
                    }
                    else
                    {
                        if (GetSpecialLocation(name1, out Vector3 pos, out destinationName, sender))
                        {
                            teleportedUser = sender.playerUsername;
                            TeleportPlayer(sender, pos, instant);
                            break;
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
                        if (GetSpecialLocation(name1, out Vector3 pos, out destinationName, player1))
                        {
                            teleportedUser = player1.playerUsername;
                            TeleportPlayer(player1, pos, instant);
                            break;
                        }

                        return UnknownPlayerException(name2);
                    }

                    teleportedUser = sender.playerUsername;
                    destinationName = player1.playerUsername;
                    TeleportPlayer(player1, player2.transform.position, instant);

                    break;

                case 3:
                    if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos1))
                    {
                        teleportedUser = sender.playerUsername;
                        destinationName = pos1.x + ", " + pos1.y + ", " + pos1.z;
                        TeleportPlayer(sender, pos1, instant);
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
                            TeleportPlayer(player1, pos2, instant);
                            break;
                        }
                        return UnknownVectorException();
                    }
                    return UnknownPlayerException(name1);

                default: 
                    break;
            }

            if (teleportedUser != "")
            {
                success = true;
                return "Teleporting " + teleportedUser + " to " + destinationName;
            }

            return "";
        }

        private bool GetSpecialLocation(string name, out Vector3 pos, out string formalName, PlayerControllerB target)
        {
            if (name == "ship")
            {
                pos = new Vector3(0, 0, -14);
                formalName = "your Autopilot Ship";
                return true;
            }
            if (name == "main")
            {
                pos = RoundManager.FindMainEntrancePosition(true, !target.isInsideFactory);
                if (pos != Vector3.zero)
                {
                    formalName = "the Main Entrance";
                    return true;
                }
            }

            formalName = "";
            pos = Vector3.zero;
            return false;
        }

        private void TeleportPlayer(PlayerControllerB player,  Vector3 pos, bool instant)
        {
            if (instant)
            {
                player.TeleportPlayer(pos);
            }
            else
            {
                player.StartCoroutine(beamUpPlayer(player, pos));
            }
        }

        private IEnumerator beamUpPlayer(PlayerControllerB player, Vector3 pos)
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

            player.DropAllHeldItems();

            /*
                if ((bool)Object.FindObjectOfType<AudioReverbPresets>())
                {
                    Object.FindObjectOfType<AudioReverbPresets>().audioPresets[3].ChangeAudioReverbForPlayer(player);
                }

                player.isInElevator = true;
                player.isInHangarShipRoom = true;
                player.isInsideFactory = false;
            */

            player.averageVelocity = 0f;
            player.velocityLastFrame = Vector3.zero;
            player.TeleportPlayer(pos);

            player.shipTeleporterId = -1;
        }
    }
}
