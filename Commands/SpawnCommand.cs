using GameNetcodeStuff;
using System.Collections.Generic;
using JLL.API.LevelProperties;
using UnityEngine;
using UnityEngine.AI;

namespace SimpleCommands.Commands
{
    public class SpawnCommand : SimpleCommand
    {
        public SpawnCommand() : base("spawn", "spawns item")
        {
            instructions.Add("[/cmd] [enemy]");
            instructions.Add("[/cmd] [target] [enemy]");
            instructions.Add("[/cmd] [enemy] [x] [y] [z]");
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
                    string first = parameters.GetString();

                    if (parameters.Count() >= 4)
                    {
                        if (parameters.GetRelativeVector(sender.transform.position, out Vector3 pos))
                        {
                            enemyName = first;
                            spawnPos = pos;
                        }
                        else return UnknownVectorException();
                    }
                    else if (parameters.Count(2))
                    {
                        PlayerControllerB? player = GetPlayer(first);
                        enemyName = parameters.GetString();

                        if (player != null)
                        {
                            spawnPos = player.transform.position;
                        }
                        else
                        {
                            return UnknownPlayerException(name);
                        }
                    }
                    else
                    {
                        enemyName = first;
                    }
                }

                List<EnemyType> foundMatches = new List<EnemyType>();
                foreach (EnemyType enemy in JLevelPropertyRegistry.AllSortedEnemies)
                {
                    if (enemy.enemyName.ToLower().Replace(' ', '_').StartsWith(enemyName.ToLower()))
                    {
                        foundMatches.Add(enemy);
                    }
                }

                if (foundMatches.Count > 0)
                {
                    int smallest = 0;
                    for (int i = 0; i < foundMatches.Count; i++)
                    {
                        if (foundMatches[i].enemyName.Length < foundMatches[smallest].enemyName.Length)
                        {
                            smallest = i;
                        }
                    }

                    if (foundMatches[smallest].enemyPrefab != null)
                    {
                        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5, NavMesh.AllAreas))
                        {
                            GameObject obj = RoundManager.Instance.SpawnEnemyGameObject(hit.position, 0, 0, foundMatches[smallest]);
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

            int page = 0;
            if (!parameters.IsEmpty())
            {
                page = parameters.GetNumber();
            }

            success = true;
            ClearChat();
            return PagedList(title, names, page, 8);
        }
    }
}
