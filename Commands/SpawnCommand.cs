using GameNetcodeStuff;
using System.Collections.Generic;
using JLL.API.LevelProperties;
using UnityEngine;
using UnityEngine.AI;
using BepInEx;
using JLL.API;
using LethalLib.Modules;

namespace SimpleCommands.Commands
{
    public class SpawnCommand : SimpleCommand
    {
        public SpawnCommand() : base("spawn", "spawns item")
        {
            instructions.Add("[/cmd] [enemy]");
            instructions.Add("[/cmd] [enemy] [target]");
            instructions.Add("[/cmd] [enemy] [x] [y] [z]");

            tagInfo.Add("'Ignore':\nIgnores nav mesh restrictions (Will Throw Errors)");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = false;

            if (sender.IsHost || sender.IsServer)
            {
                string enemyName = "";
                Vector3 spawnPos = sender.transform.position;

                if (!parameters.IsEmpty())
                {
                    enemyName = parameters.GetString();


                    if (parameters.Count() >= 4)
                    {
                        if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos))
                        {
                            spawnPos = pos;
                        }
                        else return UnknownVectorException();
                    }
                    else if (parameters.Count(2))
                    {
                        string playerName = parameters.GetString();
                        PlayerControllerB? player = GetPlayer(playerName);

                        if (player != null)
                        {
                            spawnPos = player.transform.position;
                        }
                        else
                        {
                            return UnknownPlayerException(playerName);
                        }
                    }
                }

                List<EnemyType> foundMatches = new List<EnemyType>();
                int smallest = 0;
                if (!enemyName.IsNullOrWhiteSpace()) foreach (EnemyType enemy in JLevelPropertyRegistry.AllSortedEnemies)
                {
                    if (enemy.enemyName.ToLower().Replace(' ', '_').StartsWith(enemyName.ToLower()))
                    {
                        foundMatches.Add(enemy);
                        if (enemy.enemyName.Length < foundMatches[smallest].enemyName.Length) smallest = foundMatches.Count - 1;
                    }
                }

                if (foundMatches.Count > 0)
                {
                    if (foundMatches[smallest].enemyPrefab != null)
                    {
                        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5, NavMesh.AllAreas))
                        {
                            RoundManager.Instance.SpawnEnemyGameObject(hit.position, 0, 0, foundMatches[smallest]);
                        }
                        else if (parameters.isFlagged("ignore"))
                        {
                            RoundManager.Instance.SpawnEnemyGameObject(spawnPos, 0, 0, foundMatches[smallest]);
                        }
                        else
                        {
                            return "Failed to spawn. (Couldn't Find NavMesh.)";
                        }
                    }

                    success = true;
                    return $"Spawned {foundMatches[smallest].enemyName} at {spawnPos}.";
                }
                return "Unknown Enemy: " + enemyName;
            }
            return "";
        }
    }

    public class EnemiesCommand : SimpleCommand
    {
        public EnemiesCommand() : base("enemies", "lists enemies") 
        { 
            overrideShowOutput = true;
            permissionRequired = false;

            instructions.Add("[/cmd] - lists item ids");
            instructions.Add("[/cmd] [page]");

            tagInfo.Add("'Indoor':\nFilters to show interior enemies");
            tagInfo.Add("'Outdoor':\nFilters to show exterior enemies");
        }

        public override string Execute(PlayerControllerB sender, CommandParameters parameters, out bool success)
        {
            success = true;
            if (!sender.IsLocalPlayer()) return "";

            string title = "";

            bool indoor = parameters.isFlagged("indoor");
            bool outdoor = parameters.isFlagged("outdoor");

            if (indoor || outdoor)
            {
                if (indoor)
                {
                    title = "Indoor Enemies:";
                }
                if (outdoor)
                {
                    title = "Outdoor Enemies:";
                }
                if (indoor && outdoor)
                {
                    title = "Spawnable Enemies:";
                }
            }
            else
            {
                title = "Spawnable Enemies:";
                indoor = true;
                outdoor = true;
            }

            List<string> names = new List<string>();

            foreach (EnemyType enemy in JLevelPropertyRegistry.AllSortedEnemies)
            {
                if ((enemy.isOutsideEnemy || enemy.isDaytimeEnemy) ? outdoor : indoor)
                {
                    names.Add(enemy.enemyName.Replace(' ', '_'));
                }
            }

            ClearChat();
            return PagedList(title, names, parameters.IsEmpty() ? 0 : parameters.GetNumber(), 8);
        }
    }
}
